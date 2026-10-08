using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using SaltysDroidStandby.Patches;
using UnityEngine;

namespace SaltysDroidStandby.Game
{
    // Everything that belongs to the local player, run once per frame by the plugin's
    // Update: the key, wake checks, the safety net, and resetting the local mirror when the
    // server-side rules would have (death, bed/sleeper). Server effects live in the patches.
    public static class LocalController
    {
        private static readonly PressDetector Press = new PressDetector(0.6f);
        private static WakeEvaluator _evaluator;
        private static float _nextCheckAt;
        private static float _lastInputAt;
        private static Vector3 _lastMouse;
        private static bool _suppressedUntilInput;

        public static StandbyLevel Level => StandbyRegistry.Get(InventoryManager.ParentHuman);
        public static WakeCondition Selected { get; set; } = WakeCondition.None;
        public static bool PanelOpen { get; set; }
        public static bool PausedBySafetyNet { get; private set; }
        public static string LastWakeReason { get; private set; }
        public static WorldSnapshot LastReading { get; private set; }

        public static void Tick()
        {
            Human me = InventoryManager.ParentHuman;
            if (me == null || !me.IsArtificial || SaltysDroidStandby.StandbyDisabled)
            {
                return;
            }

            if (Level != StandbyLevel.Normal && CognitionFloorPatch.IsBlocked(me))
            {
                SetLevel(me, StandbyLevel.Normal);
            }

            // "Until another logs on": a join ends a solo safety pause; the droid stays in Deep.
            if (PausedBySafetyNet && !IsEffectivelySolo())
            {
                ResumeFromSafetyPause();
                SaltysDroidStandby.Log("Safety pause ended: another player joined");
            }

            TrackInput();
            HandleKey(me);

            if (Level == StandbyLevel.Deep && _evaluator != null && Time.time >= _nextCheckAt)
            {
                _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
                LastReading = WorldReadings.Read(me);
                string reason = _evaluator.Check(LastReading);
                if (reason != null)
                {
                    Wake(me, reason, automatic: true);
                }
            }

            float battery = WorldReadings.TotalBattery(me);
            if (SafetyNetLogic.ShouldTrigger(Level, battery, Time.unscaledTime - _lastInputAt,
                    StandbyConfig.SafetyBattery, StandbyConfig.SafetyIdleSeconds, _suppressedUntilInput))
            {
                EnterDeep(me);
                PanelOpen = false;
                LastWakeReason = null;
                if (IsEffectivelySolo() && StandbyConfig.SafetyPause)
                {
                    WorldManager.SetGamePause(true);
                    PausedBySafetyNet = true;
                }
                SaltysDroidStandby.Log($"Safety net: Deep Standby at {battery * 100f:F0}% battery");
            }
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
            Press.LongPressSeconds = StandbyConfig.LongPressSeconds;
            // Ignore the key while typing in chat/console, in menus, or paused (Review Focus 4).
            bool down = KeyManager.InputState == KeyInputState.Game && Input.GetKey(StandbyConfig.StandbyKey);
            Gesture g = Press.Update(down, Time.unscaledTime);
            if (g == Gesture.None) return;

            StandbyLevel next = LevelTransitions.Next(Level, g);
            if (next == StandbyLevel.Deep)
            {
                EnterDeep(me);
            }
            else if (Level == StandbyLevel.Deep)
            {
                Wake(me, "manual", automatic: false);
            }
            else
            {
                SetLevel(me, next);
            }
        }

        private static void EnterDeep(Human me)
        {
            SetLevel(me, StandbyLevel.Deep);
            Selected = Selected == WakeCondition.None ? StandbyConfig.DefaultWake : Selected;
            LastReading = WorldReadings.Read(me);
            _evaluator = new WakeEvaluator(Selected, StandbyConfig.Thresholds(), LastReading);
            _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
            PanelOpen = true;
        }

        private static void Wake(Human me, string reason, bool automatic)
        {
            SetLevel(me, StandbyLevel.Normal);
            _evaluator = null;
            PanelOpen = false;
            LastWakeReason = automatic ? "Woke: " + reason : null;
            _suppressedUntilInput = automatic;
            if (automatic) SaltysDroidStandby.Log("Woke: " + reason);
        }

        private static void SetLevel(Human me, StandbyLevel level)
        {
            if (level != StandbyLevel.Deep)
            {
                _evaluator = null;
                PanelOpen = false;
            }
            StandbyNetwork.Request(me, level);
        }

        // Called by the panel when the player presses Confirm: rebuild the evaluator with the
        // conditions they ticked, baselined on the current readings.
        public static void ConfirmPanel()
        {
            Human me = InventoryManager.ParentHuman;
            if (me == null || Level != StandbyLevel.Deep) return;
            LastReading = WorldReadings.Read(me);
            _evaluator = new WakeEvaluator(Selected, StandbyConfig.Thresholds(), LastReading);
            PanelOpen = false;
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
