using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace ToDHelperPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;


    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("ToD Helper Configuration###ToDHelperConfigWindow")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        this.configuration = plugin.Configuration;
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

                ImGui.Separator();
                ImGui.Text("Balancing Mode");
                
                var balMode = configuration.BalancingCalculationMode;
                if (ImGui.RadioButton("Percentage (Actions / Rounds)", ref balMode, 0)) { configuration.BalancingCalculationMode = 0; configuration.Save(); }
                if (ImGui.RadioButton("Deficit (Expected vs Actual)", ref balMode, 1)) { configuration.BalancingCalculationMode = 1; configuration.Save(); }

                ImGui.Spacing();
                var enableTags = configuration.EnableDynamicTags;
                if (ImGui.Checkbox("Enable Dynamic Warning Tags", ref enableTags))
                {
                    configuration.EnableDynamicTags = enableTags;
                    configuration.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Shows colored tags next to player names if their stats are unbalanced (difference >= 2).\n\n" +
                                     "[Needs Dare] - Received 2+ more Truths than Dares\n" +
                                     "[Needs Truth] - Received 2+ more Dares than Truths\n" +
                                     "[Needs to Ask] - Received 2+ more actions than given\n" +
                                     "[Needs to Answer] - Given 2+ more actions than received");
                }

                ImGui.Spacing();
                var avoidConsecutive = configuration.AvoidConsecutiveTurns;
                if (ImGui.Checkbox("Avoid Consecutive Turns", ref avoidConsecutive))
                {
                    configuration.AvoidConsecutiveTurns = avoidConsecutive;
                    configuration.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Prevents players who just had a turn (last round's Giver/Receiver) from being recommended in the next round, and highlights them in selection lists.");
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Messages"))
            {
                using (var child = Dalamud.Interface.Utility.Raii.ImRaii.Child("MessagesScrollArea", new Vector2(-1, -1), false))
                {
                    if (child.Success)
                    {
                        ImGui.Spacing();
                        
                        var chatPrefix = configuration.ChatPrefix;
                        ImGui.SetNextItemWidth(150f);
                        if (ImGui.InputText("Chat Prefix", ref chatPrefix, 50))
                        {
                            configuration.ChatPrefix = chatPrefix;
                            configuration.Save();
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Prefix added to all automated messages (e.g., /p, /a, /fc, /s)");
                        }
                        
                        ImGui.Separator();
                        ImGui.Spacing();

                        configuration.EnableRoundStartMsg = DrawMessageCategory("Round Start", configuration.EnableRoundStartMsg, configuration.RoundStartMessages);
                        ImGui.Separator();
                        configuration.EnableRoundReminderMsg = DrawMessageCategory("Round Reminder", configuration.EnableRoundReminderMsg, configuration.RoundReminderMessages);
                        if (configuration.EnableRoundReminderMsg)
                        {
                            var reminderTime = configuration.RoundReminderTime;
                            ImGui.SetNextItemWidth(100f);
                            if (ImGui.InputInt("Remaining time to trigger reminder (seconds)", ref reminderTime))
                            {
                                if (reminderTime < 1) reminderTime = 1;
                                configuration.RoundReminderTime = reminderTime;
                                configuration.Save();
                            }
                            ImGui.Spacing();
                        }
                        ImGui.Separator();
                        configuration.EnableRoundClosedMsg = DrawMessageCategory("Round Closed", configuration.EnableRoundClosedMsg, configuration.RoundClosedMessages);
                        ImGui.Separator();
                        configuration.EnableDiceAnnounceMsg = DrawMessageCategory("Dice Announce", configuration.EnableDiceAnnounceMsg, configuration.DiceAnnounceMessages);
                        ImGui.Separator();
                        configuration.EnableCustomAnnounceMsg = DrawMessageCategory("Custom Announce", configuration.EnableCustomAnnounceMsg, configuration.CustomAnnounceMessages);

                        ImGui.Spacing();
                        ImGui.Separator();
                         if (ImGui.CollapsingHeader("Available Placeholders"))
                        {
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[Timer] - Total round time (e.g., 01:30)");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[RemainingTimer] - Time left (e.g., 00:15)");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[RoundNumber] - Current round index");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[TotalRounds] - Total rounds played");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[GiverName] - Dice winner (highest roll)");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[GiverRoll] - Roll number of the dice winner");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[ReceiverName] - Dice loser (lowest roll)");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[ReceiverRoll] - Roll number of the dice loser");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[CustomGiverName] - Manually selected giver");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[CustomGiverRoll] / [GiverRoll] - Roll of manually selected giver");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[CustomReceiverName] - Manually selected receiver");
                            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "[CustomReceiverRoll] / [ReceiverRoll] - Roll of manually selected receiver");
                        }
                    }
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Support & Community"))
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Support & Community");
                ImGui.Spacing();
                if (ImGui.Button("Join Support Discord"))
                {
                    Dalamud.Utility.Util.OpenLink("https://discord.gg/PvxW4mXaWp");
                }
                ImGui.SameLine();
                if (ImGui.Button("Support on Ko-fi"))
                {
                    Dalamud.Utility.Util.OpenLink("https://ko-fi.com/kararemy");
                }
                ImGui.Spacing();
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
