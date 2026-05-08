using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System.Linq;

namespace ToDHelperPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;
    private int selectedGiverIndex = -1;
    private int selectedReceiverIndex = -1;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("ToD Helper###ToDHelperMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(1, 1),
            Click = _ => plugin.ToggleConfigUi(),
            ShowTooltip = () => ImGui.SetTooltip("Settings")
        });
    }

    public void Dispose() { }

    private void DrawIntCell(string id, int value, Action<int> deltaSetter)
    {
        ImGui.PushID(id);
        if (ImGui.Button("-")) deltaSetter(-1);
        ImGui.SameLine(0, 5);
        ImGui.Text($"{value}");
        ImGui.SameLine(0, 5);
        if (ImGui.Button("+")) deltaSetter(1);
        ImGui.PopID();
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ToDMainWindowTabBar"))
        {
            if (ImGui.BeginTabItem("Host Panel"))
            {
                
                ImGui.Separator();
                
                // --- Header Area ---
                ImGui.Text("Round Control");
                ImGui.SameLine(ImGui.GetWindowWidth() - 230);
                ImGui.Text($"Round: {this.plugin.GameState.RoundsPlayed}");
                
                int totalConfiguredSeconds = this.plugin.Configuration.TimerMinutes * 60 + this.plugin.Configuration.TimerSeconds;
                int displaySeconds = totalConfiguredSeconds;
                if (this.plugin.GameState.IsRoundActive)
                {
                    var remaining = (this.plugin.GameState.RoundEndTime - System.DateTime.Now).TotalSeconds;
                    displaySeconds = remaining > 0 ? (int)remaining : 0;
                }
                
                ImGui.SameLine(ImGui.GetWindowWidth() - 130);
                ImGui.Text($"Timer: {displaySeconds / 60:D2}:{displaySeconds % 60:D2}");

                if (!this.plugin.GameState.IsRoundActive)
                {
                    if (ImGui.Button("Start Round", new Vector2(-1, 30)))
                    {
                        this.plugin.GameState.StartRound(totalConfiguredSeconds);
                        
                        if (this.plugin.Configuration.EnableRoundStartMsg && this.plugin.Configuration.RoundStartMessages.Count > 0)
                        {
                            var msg = this.plugin.Configuration.RoundStartMessages[new System.Random().Next(this.plugin.Configuration.RoundStartMessages.Count)];
                            msg = msg.Replace("[Timer]", $"{this.plugin.Configuration.TimerMinutes:D2}:{this.plugin.Configuration.TimerSeconds:D2}");
                            msg = msg.Replace("[RoundNumber]", this.plugin.GameState.RoundsPlayed.ToString());
                            ChatSender.SendMessage(this.plugin.Configuration.ChatPrefix.Trim() + " " + msg);
                        }
                    }
                }
                else
                {
                    if (ImGui.Button("End Round Early", new Vector2(-1, 30)))
                    {
                        this.plugin.GameState.EndRound();
                        this.plugin.Configuration.Save();
                        
                        if (this.plugin.Configuration.EnableRoundClosedMsg && this.plugin.Configuration.RoundClosedMessages.Count > 0)
                        {
                            var msg = this.plugin.Configuration.RoundClosedMessages[new System.Random().Next(this.plugin.Configuration.RoundClosedMessages.Count)];
                            msg = msg.Replace("[RoundNumber]", this.plugin.GameState.RoundsPlayed.ToString());
                            ChatSender.SendMessage(this.plugin.Configuration.ChatPrefix.Trim() + " " + msg);
                        }
                    }
                }
                
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // --- Split View ---
                if (ImGui.BeginTable("HostPanelSplit", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("Left", ImGuiTableColumnFlags.WidthStretch, 0.55f);
                    ImGui.TableSetupColumn("Right", ImGuiTableColumnFlags.WidthStretch, 0.45f);
                    ImGui.TableNextRow();

                    // --- Left Column (Players & Rolls) ---
                    ImGui.TableNextColumn();
                    ImGui.Text("Players & Rolls");
                    using (var child = ImRaii.Child("DiceRollsArea", new Vector2(-1, 200), true))
                    {
                        if (child.Success)
                        {
                            if (!this.plugin.Configuration.AutomaticMode)
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Tracking disabled in Manual Mode.");
                            }
                            else if (this.plugin.GameState.CurrentRoundRolls.Count == 0)
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "No rolls yet.");
                            }
                            else
                            {
                                var sortedRolls = this.plugin.GameState.CurrentRoundRolls.OrderByDescending(x => x.Value).ToList();
                                int count = sortedRolls.Count;
                                int maxRoll = count >= 2 ? sortedRolls.First().Value : -1;
                                int minRoll = count >= 2 ? sortedRolls.Last().Value : -1;

                                foreach (var kvp in sortedRolls)
                                {
                                    if (count >= 2 && kvp.Value == maxRoll)
                                    {
                                        ImGui.TextColored(new Vector4(0.8f, 0.4f, 1.0f, 1f), $"{kvp.Key} - {kvp.Value}"); // Purple/Magenta
                                    }
                                    else if (count >= 2 && kvp.Value == minRoll)
                                    {
                                        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1f), $"{kvp.Key} - {kvp.Value}"); // Red
                                    }
                                    else
                                    {
                                        ImGui.Text($"{kvp.Key} - {kvp.Value}");
                                    }
                                }
                            }
                        }
                    }

                    if (ImGui.Button("Announce Players (Dice rolls)", new Vector2(-1, 0)))
                    {
                        var sortedRolls = this.plugin.GameState.CurrentRoundRolls.OrderByDescending(x => x.Value).ToList();
                        if (sortedRolls.Count >= 2)
                        {
                            var giver = sortedRolls.First().Key;
                            var receiver = sortedRolls.Last().Key;

                            if (this.plugin.Configuration.EnableDiceAnnounceMsg && this.plugin.Configuration.DiceAnnounceMessages.Count > 0)
                            {
                                var msg = this.plugin.Configuration.DiceAnnounceMessages[new System.Random().Next(this.plugin.Configuration.DiceAnnounceMessages.Count)];
                                msg = msg.Replace("[GiverName]", giver);
                                msg = msg.Replace("[ReceiverName]", receiver);
                                ChatSender.SendMessage(this.plugin.Configuration.ChatPrefix.Trim() + " " + msg);
                            }

                            if (this.plugin.Configuration.EnableBalanceTracking && !this.plugin.GameState.RoundBalanced)
                            {
                                this.plugin.GameState.RoundBalanced = true;
                                int participants = sortedRolls.Count;
                                float expectedPerPlayer = participants > 0 ? 2.0f / participants : 0f;

                                foreach (var roll in sortedRolls)
                                {
                                    var stats = this.plugin.GameState.GetOrCreatePlayer(roll.Key);
                                    stats.RoundsParticipated++;
                                    stats.ExpectedActions += expectedPerPlayer;
                                }
                                var gStats = this.plugin.GameState.GetOrCreatePlayer(giver);
                                var rStats = this.plugin.GameState.GetOrCreatePlayer(receiver);
                                gStats.TruthsGiven++;
                                rStats.TruthsReceived++;
                                this.plugin.Configuration.Save();
                            }
                        }
                    }

                    // --- Right Column (Balancing & Controls) ---
                    ImGui.TableNextColumn();
                    var enableTracking = this.plugin.Configuration.EnableBalanceTracking;
                    if (ImGui.Checkbox("Enable Balance Tracking", ref enableTracking))
                    {
                        this.plugin.Configuration.EnableBalanceTracking = enableTracking;
                        this.plugin.Configuration.Save();
                    }
                    ImGui.Text("Balancing Summary");
                    int pCount = this.plugin.GameState.Players.Count;
                    float childHeight = 30f;
                    if (pCount > 0)
                    {
                        int lines = 1 + Math.Min(3, pCount);
                        if (pCount > 3) lines += 2 + Math.Max(0, Math.Min(3, pCount - 3));
                        childHeight = 10f + lines * ImGui.GetTextLineHeightWithSpacing();
                    }
                    using (var child = ImRaii.Child("BalancingArea", new Vector2(-1, childHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                    {
                        if (child.Success)
                        {
                            var bPlayers = this.plugin.GameState.Players.Values.ToList();
                            if (bPlayers.Count == 0) {
                                ImGui.Text("No players tracked yet.");
                            } else {
                                bPlayers.Sort((a, b) => {
                                    float GetScore(Data.PlayerStats p) {
                                        if (this.plugin.Configuration.BalancingCalculationMode == 0) {
                                            if (p.RoundsParticipated == 0) return 0f;
                                            return (p.TotalGiven + p.TotalReceived) / (float)p.RoundsParticipated;
                                        } else {
                                            return (p.TotalGiven + p.TotalReceived) - p.ExpectedActions;
                                        }
                                    }
                                    int comp = GetScore(a).CompareTo(GetScore(b));
                                    if (comp == 0) return b.RoundsParticipated.CompareTo(a.RoundsParticipated);
                                    return comp;
                                });
                                
                                ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), "Highest Priority:");
                                for(int i=0; i<Math.Min(3, bPlayers.Count); i++) {
                                    ImGui.Text($"> {bPlayers[i].Name} ({bPlayers[i].TotalGiven}G / {bPlayers[i].TotalReceived}R)");
                                }
                                
                                if (bPlayers.Count > 3) {
                                    ImGui.Spacing();
                                    ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), "Lowest Priority:");
                                    for(int i=Math.Max(3, bPlayers.Count - 3); i<bPlayers.Count; i++) {
                                        ImGui.Text($"> {bPlayers[i].Name} ({bPlayers[i].TotalGiven}G / {bPlayers[i].TotalReceived}R)");
                                    }
                                }
                            }
                        }
                    }
                    
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                    
                    var allPlayers = this.plugin.GameState.Players.Values.Select(p => p.Name).ToList();
                    string[] playerArray = allPlayers.ToArray();

                    if (this.plugin.GameState.NeedsAutoSelection && allPlayers.Count >= 2)
                    {
                        this.plugin.GameState.NeedsAutoSelection = false;

                        var bPlayersForGiver = this.plugin.GameState.Players.Values.ToList();
                        bPlayersForGiver.Sort((a, b) => {
                            float GetScore(Data.PlayerStats p) {
                                if (this.plugin.Configuration.BalancingCalculationMode == 0) {
                                    if (p.RoundsParticipated == 0) return 0f;
                                    return p.TotalGiven / (float)p.RoundsParticipated;
                                } else {
                                    return p.TotalGiven - (p.ExpectedActions / 2f);
                                }
                            }
                            int comp = GetScore(a).CompareTo(GetScore(b));
                            if (comp == 0) return b.RoundsParticipated.CompareTo(a.RoundsParticipated);
                            return comp;
                        });

                        var bPlayersForReceiver = this.plugin.GameState.Players.Values.ToList();
                        bPlayersForReceiver.Sort((a, b) => {
                            float GetScore(Data.PlayerStats p) {
                                if (this.plugin.Configuration.BalancingCalculationMode == 0) {
                                    if (p.RoundsParticipated == 0) return 0f;
                                    return p.TotalReceived / (float)p.RoundsParticipated;
                                } else {
                                    return p.TotalReceived - (p.ExpectedActions / 2f);
                                }
                            }
                            int comp = GetScore(a).CompareTo(GetScore(b));
                            if (comp == 0) return b.RoundsParticipated.CompareTo(a.RoundsParticipated);
                            return comp;
                        });

                        var recommendedGiver = bPlayersForGiver.First().Name;
                        var recommendedReceiver = bPlayersForReceiver.First(p => p.Name != recommendedGiver).Name;

                        selectedGiverIndex = Array.IndexOf(playerArray, recommendedGiver);
                        selectedReceiverIndex = Array.IndexOf(playerArray, recommendedReceiver);
                    }
                    
                    ImGui.Text("Recommended Giver:");
                    if (ImGui.BeginCombo("##GiverCombo", selectedGiverIndex >= 0 && selectedGiverIndex < playerArray.Length ? playerArray[selectedGiverIndex] : "Select Giver..."))
                    {
                        for (int i = 0; i < playerArray.Length; i++)
                        {
                            if (ImGui.Selectable(playerArray[i], selectedGiverIndex == i))
                                selectedGiverIndex = i;
                        }
                        ImGui.EndCombo();
                    }
                    
                    ImGui.Text("Recommended Receiver:");
                    if (ImGui.BeginCombo("##ReceiverCombo", selectedReceiverIndex >= 0 && selectedReceiverIndex < playerArray.Length ? playerArray[selectedReceiverIndex] : "Select Receiver..."))
                    {
                        for (int i = 0; i < playerArray.Length; i++)
                        {
                            if (ImGui.Selectable(playerArray[i], selectedReceiverIndex == i))
                                selectedReceiverIndex = i;
                        }
                        ImGui.EndCombo();
                    }
                    
                    ImGui.Spacing();
                    if (ImGui.Button("Announce Custom Players", new Vector2(-1, 0)))
                    {
                        if (selectedGiverIndex >= 0 && selectedGiverIndex < playerArray.Length &&
                            selectedReceiverIndex >= 0 && selectedReceiverIndex < playerArray.Length)
                        {
                            string cGiver = playerArray[selectedGiverIndex];
                            string cReceiver = playerArray[selectedReceiverIndex];

                            if (this.plugin.Configuration.EnableCustomAnnounceMsg && this.plugin.Configuration.CustomAnnounceMessages.Count > 0)
                            {
                                var msg = this.plugin.Configuration.CustomAnnounceMessages[new System.Random().Next(this.plugin.Configuration.CustomAnnounceMessages.Count)];
                                msg = msg.Replace("[CustomGiverName]", cGiver);
                                msg = msg.Replace("[CustomReceiverName]", cReceiver);
                                ChatSender.SendMessage(this.plugin.Configuration.ChatPrefix.Trim() + " " + msg);
                            }

                            if (this.plugin.Configuration.EnableBalanceTracking)
                            {
                                var gStats = this.plugin.GameState.GetOrCreatePlayer(cGiver);
                                var rStats = this.plugin.GameState.GetOrCreatePlayer(cReceiver);
                                gStats.TruthsGiven++;
                                rStats.TruthsReceived++;
                                this.plugin.Configuration.Save();
                            }
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button("Reset Game"))
                {
                    ImGui.OpenPopup("ResetGameConfirm");
                }
                
                bool popupResetGame = true;
                if (ImGui.BeginPopupModal("ResetGameConfirm", ref popupResetGame, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Are you sure you want to reset the ENTIRE game?\nThis clears all players, rolls, and the round counter.");
                    ImGui.Separator();
                    if (ImGui.Button("Yes", new Vector2(120, 0)))
                    {
                        this.plugin.GameState.Players.Clear();
                        this.plugin.GameState.CurrentRoundRolls.Clear();
                        this.plugin.GameState.RoundsPlayed = 0;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("No", new Vector2(120, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Balancing"))
            {
                if (ImGui.Button("Reset Tracking"))
                {
                    ImGui.OpenPopup("ResetTrackingConfirm");
                }
                
                bool popupOpen = true;
                if (ImGui.BeginPopupModal("ResetTrackingConfirm", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Are you sure you want to clear all balancing data?\nThis action cannot be undone.");
                    ImGui.Separator();
                    if (ImGui.Button("Yes", new Vector2(120, 0)))
                    {
                        this.plugin.GameState.Players.Clear();
                        this.plugin.Configuration.Save();
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("No", new Vector2(120, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }

                ImGui.Spacing();
                
                var separateTracking = this.plugin.Configuration.SeparateBalancingTracking;
                int columns = separateTracking ? 7 : 5;
                if (this.plugin.GameState.Players.Count == 0)
                {
                    ImGui.Text("No Players");
                }
                else if (ImGui.BeginTable("BalancingTableV3", columns, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings))
                {
                    ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 100f);
                    ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                    if (separateTracking)
                    {
                        ImGui.TableSetupColumn("T. Given", ImGuiTableColumnFlags.WidthFixed, 65f);
                        ImGui.TableSetupColumn("T. Recv", ImGuiTableColumnFlags.WidthFixed, 65f);
                        ImGui.TableSetupColumn("D. Given", ImGuiTableColumnFlags.WidthFixed, 65f);
                        ImGui.TableSetupColumn("D. Recv", ImGuiTableColumnFlags.WidthFixed, 65f);
                    }
                    else
                    {
                        ImGui.TableSetupColumn("Given", ImGuiTableColumnFlags.WidthFixed, 65f);
                        ImGui.TableSetupColumn("Received", ImGuiTableColumnFlags.WidthFixed, 65f);
                    }
                    ImGui.TableSetupColumn("Rounds", ImGuiTableColumnFlags.WidthFixed, 65f);
                    ImGui.TableHeadersRow();

                    var players = this.plugin.GameState.Players.Values.ToList();
                    players.Sort((a, b) => {
                        float GetScore(Data.PlayerStats p) {
                            if (this.plugin.Configuration.BalancingCalculationMode == 0) { // Percentage
                                if (p.RoundsParticipated == 0) return 0f;
                                return (p.TotalGiven + p.TotalReceived) / (float)p.RoundsParticipated;
                            } else { // Deficit
                                return (p.TotalGiven + p.TotalReceived) - p.ExpectedActions;
                            }
                        }
                        
                        int comp = GetScore(a).CompareTo(GetScore(b));
                        if (comp == 0) {
                            return b.RoundsParticipated.CompareTo(a.RoundsParticipated);
                        }
                        return comp;
                    });
                    
                    int totalPlayers = players.Count;
                    int index = 0;

                    foreach (var player in players)
                    {
                        index++;
                        ImGui.PushID(player.Name);
                        ImGui.TableNextRow();
                        
                        ImGui.TableNextColumn();
                        float score = 0f;
                        if (this.plugin.Configuration.BalancingCalculationMode == 0) {
                            score = player.RoundsParticipated == 0 ? 0f : (player.TotalGiven + player.TotalReceived) / (float)player.RoundsParticipated;
                            if (index <= totalPlayers / 3.0f + 0.5f) {
                                ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), $"[UP] {score:P0}");
                            } else if (index >= totalPlayers * 2.0f / 3.0f && totalPlayers >= 3) {
                                ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), $"[REST] {score:P0}");
                            } else {
                                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), $"- {score:P0}");
                            }
                        } else {
                            score = player.ExpectedActions - (player.TotalGiven + player.TotalReceived);
                            if (index <= totalPlayers / 3.0f + 0.5f) {
                                ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), $"[UP] {(score>0?"+":"")}{score:F1}");
                            } else if (index >= totalPlayers * 2.0f / 3.0f && totalPlayers >= 3) {
                                ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), $"[REST] {score:F1}"); // expected - actual, so if negative it means they played more than expected
                            } else {
                                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), $"- {(score>0?"+":"")}{score:F1}");
                            }
                        }
                        
                        ImGui.TableNextColumn(); 
                        ImGui.Text(player.Name);
                        
                        if (this.plugin.Configuration.EnableDynamicTags)
                        {
                            if (separateTracking)
                            {
                                if (player.TruthsReceived >= player.DaresReceived + 2)
                                {
                                    ImGui.SameLine(); ImGui.TextColored(new Vector4(1.0f, 0.4f, 1.0f, 1.0f), "[Needs Dare]");
                                }
                                else if (player.DaresReceived >= player.TruthsReceived + 2)
                                {
                                    ImGui.SameLine(); ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), "[Needs Truth]");
                                }
                            }

                            if (player.TotalReceived >= player.TotalGiven + 2)
                            {
                                ImGui.SameLine(); ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), "[Needs to Ask]");
                            }
                            else if (player.TotalGiven >= player.TotalReceived + 2)
                            {
                                ImGui.SameLine(); ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.2f, 1.0f), "[Needs to Answer]");
                            }
                        }
                        
                        if (separateTracking)
                        {
                            ImGui.TableNextColumn(); DrawIntCell("TG", player.TruthsGiven, d => { player.TruthsGiven = Math.Max(0, player.TruthsGiven + d); this.plugin.Configuration.Save(); });
                            ImGui.TableNextColumn(); DrawIntCell("TR", player.TruthsReceived, d => { player.TruthsReceived = Math.Max(0, player.TruthsReceived + d); this.plugin.Configuration.Save(); });
                            ImGui.TableNextColumn(); DrawIntCell("DG", player.DaresGiven, d => { player.DaresGiven = Math.Max(0, player.DaresGiven + d); this.plugin.Configuration.Save(); });
                            ImGui.TableNextColumn(); DrawIntCell("DR", player.DaresReceived, d => { player.DaresReceived = Math.Max(0, player.DaresReceived + d); this.plugin.Configuration.Save(); });
                        }
                        else
                        {
                            ImGui.TableNextColumn(); DrawIntCell("CG", player.TotalGiven, d => {
                                if (d > 0) player.TruthsGiven++;
                                else if (player.TruthsGiven > 0) player.TruthsGiven--;
                                else if (player.DaresGiven > 0) player.DaresGiven--;
                                this.plugin.Configuration.Save();
                            });
                            ImGui.TableNextColumn(); DrawIntCell("CR", player.TotalReceived, d => {
                                if (d > 0) player.TruthsReceived++;
                                else if (player.TruthsReceived > 0) player.TruthsReceived--;
                                else if (player.DaresReceived > 0) player.DaresReceived--;
                                this.plugin.Configuration.Save();
                            });
                        }
                        ImGui.TableNextColumn(); DrawIntCell("RP", player.RoundsParticipated, d => { player.RoundsParticipated = Math.Max(0, player.RoundsParticipated + d); this.plugin.Configuration.Save(); });
                        ImGui.PopID();
                    }

                    ImGui.EndTable();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text("Add Players from Vicinity:");
                ImGui.SetNextItemWidth(250f);
                if (ImGui.BeginCombo("##VicinityPlayers", "Select nearby player..."))
                {
                    // ObjectTable is only iterated when the dropdown is actively open, saving resources.
                    foreach (var obj in Plugin.ObjectTable)
                    {
                        if (obj != null && obj is Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter)
                        {
                            var name = obj.Name.TextValue;
                            if (!this.plugin.GameState.Players.ContainsKey(name) && ImGui.Selectable(name))
                            {
                                this.plugin.GameState.GetOrCreatePlayer(name);
                                this.plugin.Configuration.Save();
                            }
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Checkbox("Separate Truth/Dare Tracking", ref separateTracking))
                {
                    this.plugin.Configuration.SeparateBalancingTracking = separateTracking;
                    this.plugin.Configuration.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Only for manual tracking");
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}
