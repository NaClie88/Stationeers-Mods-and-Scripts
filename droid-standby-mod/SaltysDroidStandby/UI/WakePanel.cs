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
    // safe place to draw. While the panel is open the cursor is freed through vanilla's own
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

        // Placement (user request): centre of the screen, lower fifth.
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 1f);
        private static Vector2 LowerFifth() => new Vector2(Screen.width * 0.5f, Screen.height * 0.9f);
        private static Vector2 BottomEdge() => new Vector2(Screen.width * 0.5f, Screen.height * 0.97f);

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

            bool interactive = LocalController.PanelOpen || LocalController.PausedBySafetyNet;
            SetCursorFree(interactive);

            if (LocalController.PausedBySafetyNet) { DrawSafetyNet(); return; }
            if (LocalController.Level == StandbyLevel.Deep)
            {
                if (LocalController.PanelOpen) DrawPanel(); else DrawStatus();
                return;
            }
            if (LocalController.Level == StandbyLevel.PowerSave) DrawLine("Power Save Mode");
            if (LocalController.LastWakeReason != null) DrawWakeMessage();
        }

        private static void DrawPanel()
        {
            WorldSnapshot r = LocalController.LastReading;
            WakeThresholds t = StandbyConfig.Thresholds();
            ImGui.SetNextWindowBgAlpha(0.92f);
            ImGui.SetNextWindowPos(BottomEdge(), ImGuiCond.Always, BottomCenter);
            ImGui.Begin("Deep Standby##SaltysDroidStandby", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.TextColored(Amber, "Deep Standby - wake me when:");
            ImGui.Separator();
            Toggle(WakeCondition.Light, $"Light above {t.LightPercent:F0}%  (now {r.LightPercent:F0}%)");
            Toggle(WakeCondition.Wind, $"Wind above {t.WindPercent:F0}%  (now {r.WindPercent:F0}%)");
            Toggle(WakeCondition.Storm, "Storm starts or ends, incl. solar  (" + (r.Outdoors ? "outdoors" : "indoors - paused") + ")");
            Toggle(WakeCondition.Battery, $"Battery charged to {t.BatteryChargedRatio * 100f:F0}% or down to {t.BatteryLowRatio * 100f:F0}%  (now {r.BatteryRatio * 100f:F0}%)");
            Toggle(WakeCondition.Danger, "Danger: damage, pressure swing, temperature");
            ImGui.Separator();
            if (ImGui.Button("Confirm")) LocalController.ConfirmPanel();
            ImGui.Text("Enter or Confirm to apply. Press the standby key to wake at any time.");
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

        private static void DrawStatus()
        {
            ImGui.SetNextWindowBgAlpha(0.75f);
            ImGui.SetNextWindowPos(LowerFifth(), ImGuiCond.Always, Center);
            ImGui.Begin("##SaltysDroidStandbyStatus", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.TextColored(Amber, "Deep Standby - waking on: " + Describe(LocalController.Selected));
            if (ImGui.IsWindowHovered() && Input.GetMouseButtonDown(0)) LocalController.PanelOpen = true;
            ImGui.End();
        }

        private static void DrawSafetyNet()
        {
            ImGui.SetNextWindowBgAlpha(0.95f);
            ImGui.SetNextWindowPos(BottomEdge(), ImGuiCond.Always, BottomCenter);
            ImGui.Begin("Saved you##SaltysDroidStandbySafety", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.TextColored(Amber, $"Saved you at {LocalController.LastReading.BatteryRatio * 100f:F0}% battery.");
            ImGui.Text("You were idle with a low battery, so your droid entered Deep Standby.");
            if (ImGui.Button("Resume in standby")) LocalController.ResumeFromSafetyPause();
            ImGui.End();
        }

        private static void DrawLine(string text)
        {
            ImGui.SetNextWindowBgAlpha(0.6f);
            ImGui.SetNextWindowPos(LowerFifth(), ImGuiCond.Always, Center);
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
