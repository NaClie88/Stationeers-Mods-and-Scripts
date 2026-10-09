using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using SaltysDroidStandby.Patches;
using UnityEngine;

namespace SaltysDroidStandby.Game
{
    // Everything that belongs to the local player, run once per frame by the plugin's
    // Update: the key (tap / hold, spec Revision 3), the hidden Standby menu, the toggle
    // cooldown, wake checks, the safety net, and resetting the local mirror when the
    // server-side rules would have (death, bed/sleeper). Server effects live in the patches.
    public static class LocalController
    {
        private static readonly PressDetector Press = new PressDetector(3f);
        private static WakeEvaluator _evaluator;
        private static float _nextCheckAt;
        private static float _lastInputAt;
        private static Vector3 _lastMouse;
        private static bool _suppressedUntilInput;
        private static Human _lastHuman;
        private static float _lastChangeAt = float.NegativeInfinity;
        private static float _cooldownNoticeUntil = float.NegativeInfinity;

        public static StandbyLevel Level => StandbyRegistry.Get(InventoryManager.ParentHuman);
        public static WakeCondition Selected { get; set; } = WakeCondition.None;
        public static bool MenuOpen { get; private set; }
        public static bool PausedBySafetyNet { get; private set; }
        public static string LastWakeReason { get; private set; }
        public static WorldSnapshot LastReading { get; private set; }

        // Revision 3 UI feeds.
        public static float CooldownRemaining => ToggleCooldown.Remaining(_lastChangeAt, Time.unscaledTime, StandbyConfig.CooldownSeconds);
        public static bool CooldownNoticeVisible => Time.unscaledTime < _cooldownNoticeUntil && CooldownRemaining > 0f;
        public static float RampSecondsLeft => StandbyRegistry.RampSecondsLeft(InventoryManager.ParentHuman, StandbyClock.Now);

        public static void Tick()
        {
            Human me = InventoryManager.ParentHuman;

            // Final-review I6: all of this is static, so a new session (quit to menu, load,
            // respawn into a new body) must not inherit the last one's menu/pause/cursor state.
            if (!ReferenceEquals(me, _lastHuman))
            {
                ResetSession();
                _lastHuman = me;
            }

            if (me == null || !me.IsArtificial || SaltysDroidStandby.StandbyDisabled)
            {
                return;
            }

            bool blocked = CognitionFloorPatch.IsBlocked(me);
            if (blocked && (Level != StandbyLevel.Normal || MenuOpen))
            {
                MenuOpen = false;
                SetLevel(me, StandbyLevel.Normal);
            }

            // "Until another logs on": a join ends a solo safety pause; the droid stays in Standby.
            // Vanilla holds its own pause while a player connects (NetworkBase.IsPaused) and
            // releases it with SetGamePause(false) when they're in, so only unpause ourselves
            // when vanilla isn't holding one (final-review I7) -- never leave the game stuck.
            if (PausedBySafetyNet && !IsEffectivelySolo())
            {
                PausedBySafetyNet = false;
                if (WorldManager.IsGamePaused && !NetworkBase.IsPaused)
                {
                    WorldManager.SetGamePause(false);
                }
                SaltysDroidStandby.Log("Safety pause ended: another player joined");
            }

            TrackInput();
            HandleKey(me);

            if (MenuOpen && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                StartStandby();
            }

            // Live readings for the open menu.
            if (MenuOpen && Time.time >= _nextCheckAt && _evaluator == null)
            {
                _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
                LastReading = WorldReadings.Read(me);
            }

            // Wake conditions (Standby only). Automatic wakes bypass the toggle cooldown.
            if (Level == StandbyLevel.Standby && _evaluator != null && Time.time >= _nextCheckAt)
            {
                _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
                LastReading = WorldReadings.Read(me);
                string reason = _evaluator.Check(LastReading);
                if (reason != null)
                {
                    Wake(me, reason, automatic: true);
                }
            }

            // Final-review C3: never for a body in a bed/sleeper, dead or unconscious, or while
            // the game is already paused (vanilla Esc pause, or our own), or with the menu open.
            bool canStandby = !blocked && !WorldManager.IsGamePaused && !MenuOpen;
            float battery = WorldReadings.TotalBattery(me);
            if (SafetyNetLogic.ShouldTrigger(Level, battery, Time.unscaledTime - _lastInputAt,
                    StandbyConfig.SafetyBattery, StandbyConfig.SafetyIdleSeconds, _suppressedUntilInput, canStandby))
            {
                // Spec Revision 3: the safety net enters Standby (drain frozen) on the config
                // defaults, without the low-battery wake (final review I5).
                SetLevel(me, StandbyLevel.Standby);
                Selected = StandbyConfig.DefaultWake;
                Arm(me, Selected, wakeOnLow: false);
                NightVisionPatch.ForceOff();
                LastWakeReason = null;
                if (IsEffectivelySolo() && StandbyConfig.SafetyPause)
                {
                    WorldManager.SetGamePause(true);
                    PausedBySafetyNet = true;
                }
                SaltysDroidStandby.Log($"Safety net: Standby at {battery * 100f:F0}% battery");
            }
        }

        private static void ResetSession()
        {
            _evaluator = null;
            _suppressedUntilInput = false;
            MenuOpen = false;
            PausedBySafetyNet = false;
            LastWakeReason = null;
            Selected = WakeCondition.None;
            _lastInputAt = Time.unscaledTime;
            _lastChangeAt = float.NegativeInfinity;
            _cooldownNoticeUntil = float.NegativeInfinity;
            UI.WakePanelPatch.ResetSession();
        }

        // Spec §9: single-player, or a host who is the only one connected.
        private static bool IsEffectivelySolo()
        {
            var hosts = new System.Collections.Generic.List<bool>();
            foreach (var client in NetworkBase.Clients) hosts.Add(client.IsHost);
            return SafetyNetLogic.IsEffectivelySolo(NetworkManager.IsActive, NetworkManager.IsServer, hosts);
        }

        private static void TrackInput()
        {
            Vector3 mouse = Input.mousePosition;
            if (Input.anyKey || (mouse - _lastMouse).sqrMagnitude > 1f)
            {
                _lastInputAt = Time.unscaledTime;
                _suppressedUntilInput = false;
            }
            _lastMouse = mouse;
        }

        private static void HandleKey(Human me)
        {
            Press.LongPressSeconds = StandbyConfig.HoldSeconds;
            // Ignore the key while typing in chat/console, in menus, or paused (phase 1 Review Focus 4).
            // Only this key wakes (spec Revision 2): Ctrl/Alt mouse mode and every other key are left alone.
            bool down = KeyManager.InputState == KeyInputState.Game && Input.GetKey(Patches.KeyBinding.Key);
            Gesture g = Press.Update(down, Time.unscaledTime);
            if (g == Gesture.None) return;

            if (MenuOpen)
            {
                CancelMenu(); // any gesture while the menu is up closes it, back to the previous state
                return;
            }

            KeyAction action = KeyActions.Decide(Level, g);
            if (KeyActions.IsGatedByCooldown(action) && CooldownRemaining > 0f)
            {
                _cooldownNoticeUntil = Time.unscaledTime + 2f; // "systems cycling - ready in N s"
                return;
            }

            switch (action)
            {
                case KeyAction.EnterPowerSave: SetLevel(me, StandbyLevel.PowerSave); break;
                case KeyAction.EnterNormal: SetLevel(me, StandbyLevel.Normal); break;
                case KeyAction.Wake: Wake(me, "manual", automatic: false); break;
                case KeyAction.OpenMenu: OpenMenu(me); break;
            }
        }

        // Opening the menu does not change level: the droid keeps its current state until Start.
        private static void OpenMenu(Human me)
        {
            Selected = StandbyConfig.DefaultWake; // spec §5.2: pre-ticked from config on every open
            LastReading = WorldReadings.Read(me);
            _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
            MenuOpen = true;
        }

        // Called by the menu's Start button or Enter. Waits out the toggle cooldown.
        public static void StartStandby()
        {
            Human me = InventoryManager.ParentHuman;
            if (me == null || !MenuOpen) return;
            if (CooldownRemaining > 0f)
            {
                _cooldownNoticeUntil = Time.unscaledTime + 2f;
                return;
            }
            MenuOpen = false;
            SetLevel(me, StandbyLevel.Standby);
            Arm(me, Selected);
            NightVisionPatch.ForceOff();
            SaltysDroidStandby.Log("Standby started, waking on: " + Selected);
        }

        // Called by the menu's Cancel button or a tap of the key.
        public static void CancelMenu()
        {
            // The level never changes while the menu is open (OpenMenu doesn't touch it), so
            // closing it is all "return to the previous state" needs.
            MenuOpen = false;
        }

        private static void Arm(Human me, WakeCondition conditions, bool wakeOnLow = true)
        {
            LastReading = WorldReadings.Read(me);
            _evaluator = new WakeEvaluator(conditions, StandbyConfig.Thresholds(), LastReading, wakeOnLow);
            _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
        }

        // Spec Revision 3: every wake lands in Power Save, not Normal.
        private static void Wake(Human me, string reason, bool automatic)
        {
            // Final-review I4: only a "battery low" wake stands the safety net down until input.
            bool batteryLow = _evaluator != null && _evaluator.LastWakeWasBatteryLow;
            SetLevel(me, KeyActions.WakeTarget);
            LastWakeReason = automatic ? "Woke into Power Save: " + reason : null;
            _suppressedUntilInput = automatic && batteryLow;
            if (automatic)
            {
                // In-game test 2026-10-08: NarrationPanel was inaudible (vanilla never plays that
                // clip); StageComplete is the helper-hint chime and does play.
                UIAudioManager.Play(UIAudioManager.StageCompleteHash);
                SaltysDroidStandby.Log("Woke: " + reason);
            }
        }

        private static void SetLevel(Human me, StandbyLevel level)
        {
            if (level != StandbyLevel.Standby) _evaluator = null;
            if (level != Level) _lastChangeAt = Time.unscaledTime; // starts the toggle cooldown
            StandbyNetwork.Request(me, level);
        }

        public static void ResumeFromSafetyPause()
        {
            if (PausedBySafetyNet)
            {
                WorldManager.SetGamePause(false);
                PausedBySafetyNet = false;
            }
        }

        public static void DismissWakeMessage() => LastWakeReason = null;
    }
}
