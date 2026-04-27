using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace ToDHelperPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("ToD Helper Configuration###ToDHelperConfigWindow")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Add or remove flags before Draw
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ToDConfigTabBar"))
        {
            if (ImGui.BeginTabItem("Game Settings"))
            {
                ImGui.Spacing();
                var autoMode = configuration.AutomaticMode;
                if (ImGui.Checkbox("Automatic Mode", ref autoMode))
                {
                    configuration.AutomaticMode = autoMode;
                    configuration.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Automatically tracks dice rolls when a round is active.");
                }

                ImGui.Separator();
                ImGui.Text("Round Timer Setup");
                
                var mins = configuration.TimerMinutes;
                var secs = configuration.TimerSeconds;

                ImGui.SetNextItemWidth(100f);
                if (ImGui.InputInt("Minutes", ref mins))
                {
                    if (mins < 0) mins = 0;
                    configuration.TimerMinutes = mins;
                    configuration.Save();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                if (ImGui.InputInt("Seconds", ref secs))
                {
                    if (secs < 0) secs = 0;
                    if (secs > 59) secs = 59;
                    configuration.TimerSeconds = secs;
                    configuration.Save();
                }

                ImGui.Separator();
                ImGui.Text("Roll Mode");
                
                var rollMode = configuration.RollMode;
                if (ImGui.RadioButton("/random", ref rollMode, 0) || ImGui.RadioButton("/dice", ref rollMode, 1))
                {
                    configuration.RollMode = rollMode;
                    configuration.Save();
                }

                if (configuration.RollMode == 1)
                {
                    ImGui.Spacing();
                    ImGui.Text("Watched Channels (for /dice):");
                    var wp = configuration.WatchParty;
                    var wa = configuration.WatchAlliance;
                    var wf = configuration.WatchFC;
                    var wo = configuration.WatchOthers;

                    if (ImGui.Checkbox("Party", ref wp)) { configuration.WatchParty = wp; configuration.Save(); }
                    ImGui.SameLine();
                    if (ImGui.Checkbox("Alliance", ref wa)) { configuration.WatchAlliance = wa; configuration.Save(); }
                    ImGui.SameLine();
                    if (ImGui.Checkbox("FC", ref wf)) { configuration.WatchFC = wf; configuration.Save(); }
                    ImGui.SameLine();
                    if (ImGui.Checkbox("Others (LS/CWLS)", ref wo)) { configuration.WatchOthers = wo; configuration.Save(); }
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Messages"))
            {
                ImGui.Spacing();
                ImGui.Text("Available Placeholders:");
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[Timer] - Total round time (e.g., 01:30)");
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[RemainingTimer] - Time left (e.g., 00:15)");
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[RoundNumber] - Current round index");
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[TotalRounds] - Total rounds played");
                ImGui.Separator();
                ImGui.Spacing();

                configuration.EnableRoundStartMsg = DrawMessageCategory("Round Start", configuration.EnableRoundStartMsg, configuration.RoundStartMessages);
                ImGui.Separator();
                configuration.EnableRoundReminderMsg = DrawMessageCategory("Round Reminder", configuration.EnableRoundReminderMsg, configuration.RoundReminderMessages);
                ImGui.Separator();
                configuration.EnableRoundClosedMsg = DrawMessageCategory("Round Closed", configuration.EnableRoundClosedMsg, configuration.RoundClosedMessages);

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("ToD Database"))
            {
                ImGui.Text("Manage your Truths and Dares here.");
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private bool DrawMessageCategory(string label, bool enabled, System.Collections.Generic.List<string> messages)
    {
        if (ImGui.Checkbox($"Enable {label} Messages", ref enabled))
        {
            configuration.Save();
        }

        if (!enabled) return enabled;

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionMax().X - 80f);
            if (ImGui.InputText($"##{label}_{i}", ref msg, 256))
            {
                messages[i] = msg;
                configuration.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button($"-##remove_{label}_{i}") && messages.Count > 1)
            {
                messages.RemoveAt(i);
                configuration.Save();
                i--;
            }
        }

        if (ImGui.Button($"+ Add Variation##add_{label}"))
        {
            messages.Add("");
            configuration.Save();
        }

        return enabled;
    }
}
