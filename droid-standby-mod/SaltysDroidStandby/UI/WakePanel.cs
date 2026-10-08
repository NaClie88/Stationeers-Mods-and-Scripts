using Assets.Scripts;
using Assets.Scripts.Inventory;
using HarmonyLib;
using ImGuiNET;
using SaltysDroidStandby.Game;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace SaltysDroidStandby.UI
{
    // ImGuiManager.RenderOverlay calls ImGuiWindowManager.Draw() every frame between
    // ImGui.NewFrame and ImGui.Render (and never on loading screens), so a postfix there is a
    // safe place to draw. While the menu is open the cursor is freed through vanilla's own
    // MouseModeController modal -- the mechanism ConsoleWindow uses (final-review I1: writing
    // Cursor.lockState directly gets re-locked by MouseModeController.Check() every frame).
    [HarmonyPatch(typeof(ImGuiWindowManager), nameof(ImGuiWindowManager.Draw))]
    public static class WakePanelPatch
    {
        private sealed class PanelModal : IModal
        {
            public bool UnlockCursor => true;
        }

        private static readonly PanelModal Modal = new PanelModal();
        private static bool _cursorFreed;
        private static bool _disabledShown;
        private static float _disabledShownAt = -1f;

        private static readonly Vector4 Amber = new Vector4(1f, 0.75f, 0.3f, 1f);

        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 1f);
        private static readonly Vector3[] Corners = new Vector3[4];

        // Bottom-centre anchor just above the hand-slot panel (user: "raised by the height of the
        // notification card ... it is overlaying the hands item displays"). ImGui y is top-down,
        // Unity screen y bottom-up. In-game test 2026-10-08: the measured panel top still put the
        // overlay over the hand cards (their top is ~90 % down the screen), so the result is capped
        // at 80 % down -- one card height above them -- and the measurement is logged once.
        private const float LowestAnchor = 0.8f;
        private static bool _anchorLogged;

        private static Vector2 Anchor()
        {
            float y = Screen.height * LowestAnchor;
            try
            {
                GameObject hands = InventoryManager.Instance != null ? InventoryManager.Instance.PanelHandsGameObject : null;
                if (hands != null && hands.activeInHierarchy && hands.transform is RectTransform rt)
                {
                    rt.GetWorldCorners(Corners);
                    Canvas canvas = rt.GetComponentInParent<Canvas>();
                    Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    float topUnity = RectTransformUtility.WorldToScreenPoint(cam, Corners[1]).y; // [1] = top-left
                    float measured = Screen.height - topUnity - 8f;
                    y = Mathf.Clamp(measured, Screen.height * 0.3f, Screen.height * LowestAnchor);
                    if (!_anchorLogged)
                    {
                        _anchorLogged = true;
                        SaltysDroidStandby.Log($"Overlay anchor: screen {Screen.width}x{Screen.height}, hands panel top {topUnity:F0}px from bottom (canvas {canvas?.renderMode}), measured y {measured:F0}, used y {y:F0}");
                    }
                }
            }
            catch (System.Exception)
            {
                // keep the fallback position
            }
            return new Vector2(Screen.width * 0.5f, y);
        }

        public static void Postfix()
        {
            if (InventoryManager.ParentHuman == null) return;

            // Stay out of the way of vanilla menus: the Escape/game menu (InventoryManager.ShowMenu,
            // which drives GameMenuPanel) and a hidden HUD (ShowUi). Hand the cursor back while
            // they're up; it's re-freed when the menu closes (user report, first test).
            if (InventoryManager.ShowMenu || !InventoryManager.ShowUi)
            {
                SetCursorFree(false);
                return;
            }

            // Spec §14: a one-time on-screen note when standby switched itself off.
            if (SaltysDroidStandby.StandbyDisabled && !_disabledShown && InventoryManager.ParentHuman.IsArtificial)
            {
                if (_disabledShownAt < 0f) _disabledShownAt = Time.unscaledTime;
                DrawLine("Salty's Droid Standby is disabled (a patch failed) - see BepInEx/LogOutput.log");
                if (Time.unscaledTime - _disabledShownAt > 10f) _disabledShown = true;
                return;
            }

            bool interactive = LocalController.MenuOpen || LocalController.PausedBySafetyNet;
            SetCursorFree(interactive);

            if (LocalController.PausedBySafetyNet) { DrawSafetyNet(); return; }
            if (LocalController.MenuOpen) { DrawMenu(); return; }
            switch (LocalController.Level)
            {
                case StandbyLevel.PowerSave: DrawLine("Power Save Mode"); break;
                case StandbyLevel.Standby: DrawLine("Standby - tap the standby key to wake"); break;
                case StandbyLevel.DeepStandby: DrawLine("Deep Standby - waking on: " + Describe(LocalController.Selected) + "  (tap the standby key to wake)"); break;
            }
            if (LocalController.LastWakeReason != null) DrawWakeMessage();
        }

        private static void DrawMenu()
        {
            WorldSnapshot r = LocalController.LastReading;
            WakeThresholds t = StandbyConfig.Thresholds();
            ImGui.SetNextWindowBgAlpha(0.92f);
            ImGui.SetNextWindowPos(Anchor(), ImGuiCond.Always, BottomCenter);
            ImGui.Begin("Deep Standby (Time Skip)##SaltysDroidStandby", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.TextColored(Amber, "Deep Standby - wake me when:");
            ImGui.Separator();
            Toggle(WakeCondition.Light, $"Light above {t.LightPercent:F0}%  (now {r.LightPercent:F0}%)");
            Toggle(WakeCondition.Wind, $"Wind above {t.WindPercent:F0}%  (now {r.WindPercent:F0}%)");
            Toggle(WakeCondition.Storm, "Storm starts or ends, incl. solar  (" + (r.Outdoors ? "outdoors" : "indoors - paused") + ")");
            Toggle(WakeCondition.Battery, $"Battery charged to {t.BatteryChargedRatio * 100f:F0}% or down to {t.BatteryLowRatio * 100f:F0}%  (now {r.BatteryRatio * 100f:F0}%)");
            Toggle(WakeCondition.Danger, "Danger: damage, pressure swing, temperature");
            ImGui.Separator();
            ImGui.Text("Controls and battery drain freeze until you wake.");
            if (ImGui.Button("Start")) LocalController.StartDeep();
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) LocalController.CancelMenu();
            ImGui.Text("Enter = Start. Tap the standby key to cancel.");
            ImGui.End();
        }

        private static void Toggle(WakeCondition c, string label)
        {
            bool on = (LocalController.Selected & c) != 0;
            if (ImGui.Checkbox(label, ref on))
            {
                LocalController.Selected = on ? LocalController.Selected | c : LocalController.Selected & ~c;
            }
        }

        private static void DrawSafetyNet()
        {
            ImGui.SetNextWindowBgAlpha(0.95f);
            ImGui.SetNextWindowPos(Anchor(), ImGuiCond.Always, BottomCenter);
            ImGui.Begin("Saved you##SaltysDroidStandbySafety", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.TextColored(Amber, $"Saved you at {LocalController.LastReading.BatteryRatio * 100f:F0}% battery.");
            ImGui.Text("You were idle with a low battery, so your droid entered Standby.");
            if (ImGui.Button("Resume (stay in Standby)")) LocalController.ResumeFromSafetyPause();
            ImGui.End();
        }

        private static void DrawLine(string text)
        {
            ImGui.SetNextWindowBgAlpha(0.6f);
            ImGui.SetNextWindowPos(Anchor(), ImGuiCond.Always, BottomCenter);
            ImGui.Begin("##SaltysDroidStandbyLine", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs);
            ImGui.TextColored(Amber, text);
            ImGui.End();
        }

        private static float _wakeShownAt = -1f;

        private static void DrawWakeMessage()
        {
            if (_wakeShownAt < 0f) _wakeShownAt = Time.unscaledTime;
            DrawLine(LocalController.LastWakeReason);
            if (Time.unscaledTime - _wakeShownAt > 6f)
            {
                LocalController.DismissWakeMessage();
                _wakeShownAt = -1f;
            }
        }

        private static string Describe(WakeCondition w)
        {
            if (w == WakeCondition.None) return "nothing (key only)";
            return w.ToString().ToLowerInvariant();
        }

        private static void SetCursorFree(bool free)
        {
            if (free == _cursorFreed) return;
            if (free)
            {
                MouseModeController.AddModal(Modal);
            }
            else
            {
                MouseModeController.RemoveModal(Modal);
                MouseModeController.Reset();
            }
            _cursorFreed = free;
        }

        // Called by LocalController when the session/body changes (final-review I6).
        public static void ResetSession()
        {
            if (_cursorFreed)
            {
                MouseModeController.RemoveModal(Modal);
                MouseModeController.Reset();
                _cursorFreed = false;
            }
            _wakeShownAt = -1f;
        }
    }
}
