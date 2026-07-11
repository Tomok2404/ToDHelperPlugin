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
using System.Collections.Generic;

namespace ToDHelperPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private int selectedGiverIndex = -1;
    private int selectedReceiverIndex = -1;
    private string turnGiverName = string.Empty;
    private string turnReceiverName = string.Empty;
    private bool turnIsDare = false;
    private string rolledInspirationText = string.Empty;
    private string insTypeFilter = "All";
    private string insCategoryFilter = "All";

    // Edit Turn state
    private int editingTurnIndex = -1;
    private string editGiverName = string.Empty;
    private string editReceiverName = string.Empty;
    private bool editIsDare = false;
    private string playerToRemove = string.Empty;

    // ToD Database state
    private string searchFilter = string.Empty;
    private string categoryFilter = "All";
    private string typeFilter = "All";
    private string newEntryContent = string.Empty;
    private int newEntryTypeIndex = 0; // 0 = Truth, 1 = Dare
    private string newEntryCategory = "General";
    private Guid? editingEntryId = null;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("ToD Helper###ToDHelperMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

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
            if (ImGui.BeginTabItem("Dice Mode"))
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
                                
                                this.plugin.GameState.TurnLog.Add(new Data.RecordedTurn
                                {
                                    Giver = giver,
                                    Receiver = receiver,
                                    IsDare = false,
                                    Timestamp = DateTime.Now
                                });
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
                            }

                            this.plugin.GameState.TurnLog.Add(new Data.RecordedTurn
                            {
                                Giver = cGiver,
                                Receiver = cReceiver,
                                IsDare = false,
                                Timestamp = DateTime.Now
                            });
                            this.plugin.Configuration.Save();
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
                ImGui.SameLine();
                DrawInspirationButtonAndPopup();
                
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

            if (ImGui.BeginTabItem("Turns"))
            {
                ImGui.Separator();

                // Fetch vicinity players dynamically (available for both recorder and edit popup)
                var vicinityPlayers = new List<string>();
                foreach (var obj in Plugin.ObjectTable)
                {
                    if (obj != null && obj is Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter)
                    {
                        var name = obj.Name.TextValue;
                        if (!vicinityPlayers.Contains(name))
                            vicinityPlayers.Add(name);
                    }
                }
                var localPlayer = Plugin.ObjectTable[0]?.Name.TextValue;
                if (!string.IsNullOrEmpty(localPlayer) && !vicinityPlayers.Contains(localPlayer))
                {
                    vicinityPlayers.Add(localPlayer);
                }
                vicinityPlayers.Sort();

                bool openEditPopup = false;

                // --- Split View ---
                if (ImGui.BeginTable("TurnPanelSplit", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("Left", ImGuiTableColumnFlags.WidthStretch, 0.55f);
                    ImGui.TableSetupColumn("Right", ImGuiTableColumnFlags.WidthStretch, 0.45f);
                    ImGui.TableNextRow();

                    // --- Left Column (Turn Log List) ---
                    ImGui.TableNextColumn();
                    ImGui.Text("Turn Log (List)");
                    
                    ImGui.SameLine();
                    if (ImGui.Button("Clear Log##turn_mode"))
                    {
                        ImGui.OpenPopup("ClearTurnLogConfirm");
                    }
                    
                    bool popupClear = true;
                    if (ImGui.BeginPopupModal("ClearTurnLogConfirm", ref popupClear, ImGuiWindowFlags.AlwaysAutoResize))
                    {
                        ImGui.Text("Are you sure you want to clear the entire turn log?\nThis does NOT reset player statistics, just the log history.");
                        ImGui.Separator();
                        if (ImGui.Button("Yes", new Vector2(120, 0)))
                        {
                            this.plugin.GameState.TurnLog.Clear();
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

                    using (var child = ImRaii.Child("TurnLogArea", new Vector2(-1, -45f), true))
                    {
                        if (child.Success)
                        {
                            var turnLog = this.plugin.GameState.TurnLog;
                            if (turnLog.Count == 0)
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "No turns recorded yet.");
                            }
                            else
                            {
                                for (int i = 0; i < turnLog.Count; i++)
                                {
                                    var turn = turnLog[i];
                                    ImGui.PushID($"turn_{i}");
                                    
                                    string typeStr = turn.IsDare ? "Dare" : "Truth";
                                    Vector4 typeColor = turn.IsDare ? new Vector4(1f, 0.4f, 0.4f, 1f) : new Vector4(0.4f, 0.8f, 1f, 1f);
                                    
                                    ImGui.BeginGroup();
                                    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), $"{i + 1}.");
                                    ImGui.SameLine();
                                    ImGui.Text(turn.Giver);
                                    ImGui.SameLine();
                                    ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "→");
                                    ImGui.SameLine();
                                    if (i == turnLog.Count - 1)
                                    {
                                        ImGui.TextColored(new Vector4(1.0f, 0.85f, 0.0f, 1.0f), turn.Receiver);
                                    }
                                    else
                                    {
                                        ImGui.Text(turn.Receiver);
                                    }
                                    
                                    if (this.plugin.Configuration.SeparateBalancingTracking)
                                    {
                                        ImGui.SameLine();
                                        ImGui.TextColored(typeColor, $"({typeStr})");
                                    }
                                    ImGui.EndGroup();
                                    
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.SetTooltip($"Logged at {turn.Timestamp:HH:mm:ss}\nRight-click to edit");
                                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                                        {
                                            editingTurnIndex = i;
                                            editGiverName = turn.Giver;
                                            editReceiverName = turn.Receiver;
                                            editIsDare = turn.IsDare;
                                            openEditPopup = true;
                                        }
                                    }
                                    
                                    ImGui.PopID();
                                }
                            }
                        }
                    }

                    // --- Right Column (Turn Recorder & Balancing) ---
                    ImGui.TableNextColumn();
                    
                    ImGui.Text("Record Turn");

                    if (string.IsNullOrEmpty(turnGiverName) && this.plugin.GameState.TurnLog.Count > 0)
                    {
                        turnGiverName = this.plugin.GameState.TurnLog.Last().Receiver;
                    }

                    ImGui.Text("Giver (Who asks):");
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginCombo("##TurnGiverCombo", !string.IsNullOrEmpty(turnGiverName) ? turnGiverName : "Select Giver..."))
                    {
                        foreach (var name in vicinityPlayers)
                        {
                            if (ImGui.Selectable(name, turnGiverName == name))
                                turnGiverName = name;
                        }
                        ImGui.EndCombo();
                    }

                    ImGui.Text("Receiver (Who answers):");
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginCombo("##TurnReceiverCombo", !string.IsNullOrEmpty(turnReceiverName) ? turnReceiverName : "Select Receiver..."))
                    {
                        foreach (var name in vicinityPlayers)
                        {
                            if (ImGui.Selectable(name, turnReceiverName == name))
                                turnReceiverName = name;
                        }
                        ImGui.EndCombo();
                    }

                    if (this.plugin.Configuration.SeparateBalancingTracking)
                    {
                        if (ImGui.RadioButton("Truth##turn_mode", !turnIsDare)) turnIsDare = false;
                        ImGui.SameLine();
                        if (ImGui.RadioButton("Dare##turn_mode", turnIsDare)) turnIsDare = true;
                        ImGui.Spacing();
                    }
                    else
                    {
                        turnIsDare = false;
                    }
                    
                    bool canRecord = !string.IsNullOrEmpty(turnGiverName) && 
                                     !string.IsNullOrEmpty(turnReceiverName) && 
                                     turnGiverName != turnReceiverName;

                    if (!canRecord) ImGui.BeginDisabled();
                    if (ImGui.Button("Record Turn", new Vector2(-1, 35)))
                    {
                        this.plugin.GameState.RecordTurn(turnGiverName, turnReceiverName, turnIsDare);
                        this.plugin.Configuration.Save();
                        
                        // Auto advance
                        turnGiverName = turnReceiverName;
                        turnReceiverName = string.Empty;
                    }
                    if (!canRecord) ImGui.EndDisabled();

                    ImGui.Spacing();
                    
                    bool canUndo = this.plugin.GameState.TurnLog.Count > 0;
                    if (!canUndo) ImGui.BeginDisabled();
                    if (ImGui.Button("Undo Last Turn", new Vector2(-1, 35)))
                    {
                        this.plugin.GameState.UndoLastTurn();
                        this.plugin.Configuration.Save();
                        turnGiverName = string.Empty;
                        turnReceiverName = string.Empty;
                    }
                    if (!canUndo) ImGui.EndDisabled();

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Balancing Summary
                    ImGui.Text("Balancing Summary");
                    int pCount = this.plugin.GameState.Players.Count;
                    float childHeight = 30f;
                    if (pCount > 0)
                    {
                        int lines = 1 + Math.Min(3, pCount);
                        if (pCount > 3) lines += 2 + Math.Max(0, Math.Min(3, pCount - 3));
                        childHeight = 10f + lines * ImGui.GetTextLineHeightWithSpacing();
                    }
                    
                    using (var child = ImRaii.Child("TurnBalancingArea", new Vector2(-1, childHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
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

                    ImGui.EndTable();
                }

                if (openEditPopup)
                {
                    ImGui.OpenPopup("EditTurnPopup");
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                DrawInspirationButtonAndPopup();

                // Edit Turn Modal Popup
                bool editPopupOpen = true;
                if (ImGui.BeginPopupModal("EditTurnPopup", ref editPopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text($"Edit Log Entry #{editingTurnIndex + 1}");
                    ImGui.Separator();

                    // Generate list of players for dropdowns (including the ones currently stored in the turn)
                    var editPlayersList = new List<string>(vicinityPlayers);
                    if (!string.IsNullOrEmpty(editGiverName) && !editPlayersList.Contains(editGiverName))
                        editPlayersList.Add(editGiverName);
                    if (!string.IsNullOrEmpty(editReceiverName) && !editPlayersList.Contains(editReceiverName))
                        editPlayersList.Add(editReceiverName);
                    editPlayersList.Sort();

                    ImGui.Text("Giver (Who asks):");
                    ImGui.SetNextItemWidth(250f);
                    if (ImGui.BeginCombo("##EditGiverCombo", !string.IsNullOrEmpty(editGiverName) ? editGiverName : "Select Giver..."))
                    {
                        foreach (var name in editPlayersList)
                        {
                            if (ImGui.Selectable(name, editGiverName == name))
                                editGiverName = name;
                        }
                        ImGui.EndCombo();
                    }

                    ImGui.Text("Receiver (Who answers):");
                    ImGui.SetNextItemWidth(250f);
                    if (ImGui.BeginCombo("##EditReceiverCombo", !string.IsNullOrEmpty(editReceiverName) ? editReceiverName : "Select Receiver..."))
                    {
                        foreach (var name in editPlayersList)
                        {
                            if (ImGui.Selectable(name, editReceiverName == name))
                                editReceiverName = name;
                        }
                        ImGui.EndCombo();
                    }

                    if (this.plugin.Configuration.SeparateBalancingTracking)
                    {
                        ImGui.Spacing();
                        if (ImGui.RadioButton("Truth##edit_turn", !editIsDare)) editIsDare = false;
                        ImGui.SameLine();
                        if (ImGui.RadioButton("Dare##edit_turn", editIsDare)) editIsDare = true;
                        ImGui.Spacing();
                    }
                    else
                    {
                        editIsDare = false;
                    }

                    ImGui.Separator();
                    ImGui.Spacing();

                    bool editValid = !string.IsNullOrEmpty(editGiverName) && 
                                     !string.IsNullOrEmpty(editReceiverName) && 
                                     editGiverName != editReceiverName;

                    if (!editValid) ImGui.BeginDisabled();
                    if (ImGui.Button("Save Changes", new Vector2(120, 0)))
                    {
                        this.plugin.GameState.UpdateRecordedTurn(editingTurnIndex, editGiverName, editReceiverName, editIsDare);
                        this.plugin.Configuration.Save();
                        ImGui.CloseCurrentPopup();
                    }
                    if (!editValid) ImGui.EndDisabled();

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel##edit_turn_cancel", new Vector2(120, 0)))
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
                
                ImGui.SameLine();
                if (ImGui.Button("Export to Clipboard"))
                {
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(this.plugin.GameState.Players);
                        var base64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
                        ImGui.SetClipboardText(base64);
                    }
                    catch { }
                }
                
                ImGui.SameLine();
                if (ImGui.Button("Import from Clipboard"))
                {
                    ImGui.OpenPopup("ImportConfirm");
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

                bool popupImport = true;
                if (ImGui.BeginPopupModal("ImportConfirm", ref popupImport, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Are you sure you want to overwrite current tracking data with the clipboard content?");
                    ImGui.Separator();
                    if (ImGui.Button("Yes", new Vector2(120, 0)))
                    {
                        try
                        {
                            var base64 = ImGui.GetClipboardText();
                            var json = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64));
                            var imported = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, Data.PlayerStats>>(json);
                            if (imported != null)
                            {
                                this.plugin.GameState.Players = imported;
                                this.plugin.Configuration.Save();
                            }
                        }
                        catch { }
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
                    bool openRemovePlayerPopup = false;

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
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Right-click to remove");
                            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                            {
                                playerToRemove = player.Name;
                                openRemovePlayerPopup = true;
                            }
                        }
                        
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
                            uint colorPurple = ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.2f, 0.8f, 0.25f));
                            uint colorRed = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.2f, 0.25f));

                            ImGui.TableNextColumn(); 
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, colorPurple);
                            DrawIntCell("TG", player.TruthsGiven, d => { player.TruthsGiven = Math.Max(0, player.TruthsGiven + d); this.plugin.Configuration.Save(); });
                            
                            ImGui.TableNextColumn(); 
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, colorPurple);
                            DrawIntCell("TR", player.TruthsReceived, d => { player.TruthsReceived = Math.Max(0, player.TruthsReceived + d); this.plugin.Configuration.Save(); });
                            
                            ImGui.TableNextColumn(); 
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, colorRed);
                            DrawIntCell("DG", player.DaresGiven, d => { player.DaresGiven = Math.Max(0, player.DaresGiven + d); this.plugin.Configuration.Save(); });
                            
                            ImGui.TableNextColumn(); 
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, colorRed);
                            DrawIntCell("DR", player.DaresReceived, d => { player.DaresReceived = Math.Max(0, player.DaresReceived + d); this.plugin.Configuration.Save(); });
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

                    if (openRemovePlayerPopup)
                    {
                        ImGui.OpenPopup("RemovePlayerConfirm");
                    }

                    ImGui.Spacing();
                    float btnWidth = ImGui.CalcTextSize("+1 Round to All").X + 20f;
                    ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - btnWidth);
                    if (ImGui.Button("+1 Round to All"))
                    {
                        foreach (var p in this.plugin.GameState.Players.Values)
                        {
                            p.RoundsParticipated++;
                        }
                        this.plugin.Configuration.Save();
                    }
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
                            if (name.Contains("@"))
                            {
                                name = name.Split('@')[0];
                            }
                            if (!this.plugin.GameState.Players.ContainsKey(name) && ImGui.Selectable(name))
                            {
                                this.plugin.GameState.GetOrCreatePlayer(name);
                                this.plugin.Configuration.Save();
                            }
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                if (ImGui.Button("Add all players in Vicinity"))
                {
                    ImGui.OpenPopup("AddAllVicinityConfirm");
                }

                bool popupAddAll = true;
                if (ImGui.BeginPopupModal("AddAllVicinityConfirm", ref popupAddAll, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Are you sure you want to add all players in the vicinity?");
                    ImGui.Separator();
                    if (ImGui.Button("Yes", new Vector2(120, 0)))
                    {
                        foreach (var obj in Plugin.ObjectTable)
                        {
                            if (obj != null && obj is Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter)
                            {
                                var name = obj.Name.TextValue;
                                if (name.Contains("@"))
                                {
                                    name = name.Split('@')[0];
                                }
                                if (!this.plugin.GameState.Players.ContainsKey(name))
                                {
                                    this.plugin.GameState.GetOrCreatePlayer(name);
                                }
                            }
                        }
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

                bool popupRemovePlayer = true;
                if (ImGui.BeginPopupModal("RemovePlayerConfirm", ref popupRemovePlayer, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text($"Are you sure you want to remove {playerToRemove}?");
                    ImGui.Separator();
                    if (ImGui.Button("Yes", new Vector2(120, 0)))
                    {
                        if (!string.IsNullOrEmpty(playerToRemove))
                        {
                            this.plugin.GameState.Players.Remove(playerToRemove);
                            this.plugin.Configuration.Save();
                        }
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

            if (ImGui.BeginTabItem("ToD Database"))
            {
                var manager = plugin.InspirationManager;

                // Add / Edit Entry form
                ImGui.Text(editingEntryId.HasValue ? "Edit Entry:" : "Add New Entry:");
                ImGui.PushItemWidth(ImGui.GetWindowContentRegionMax().X - 100f);

                // Content
                ImGui.InputText("Content##new_entry_content", ref newEntryContent, 512);

                // Type (Truth/Dare)
                string[] types = { "Truth", "Dare" };
                ImGui.SetNextItemWidth(120f);
                ImGui.Combo("Type##new_entry_type", ref newEntryTypeIndex, types, types.Length);

                ImGui.SameLine();
                // Category
                ImGui.SetNextItemWidth(150f);
                ImGui.InputText("Category##new_entry_cat", ref newEntryCategory, 100);

                ImGui.SameLine();
                if (ImGui.Button(editingEntryId.HasValue ? "Save##btn" : "Add##btn"))
                {
                    if (!string.IsNullOrWhiteSpace(newEntryContent))
                    {
                        string entryType = types[newEntryTypeIndex];
                        string cat = string.IsNullOrWhiteSpace(newEntryCategory) ? "General" : newEntryCategory.Trim();

                        if (editingEntryId.HasValue)
                        {
                            manager.UpdateEntry(editingEntryId.Value, newEntryContent.Trim(), entryType, cat);
                            editingEntryId = null;
                        }
                        else
                        {
                            manager.AddEntry(newEntryContent.Trim(), entryType, cat);
                        }

                        newEntryContent = string.Empty;
                    }
                }

                if (editingEntryId.HasValue)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel##btn"))
                    {
                        editingEntryId = null;
                        newEntryContent = string.Empty;
                    }
                }

                ImGui.PopItemWidth();

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Filters & Search
                ImGui.Text("Filters & Search:");

                // Search Content
                ImGui.SetNextItemWidth(150f);
                ImGui.InputText("Search##db_search", ref searchFilter, 100);

                ImGui.SameLine();
                // Filter by Type
                string[] typeFilters = { "All", "Truth", "Dare" };
                int currentTypeFilterIndex = Array.IndexOf(typeFilters, typeFilter);
                if (currentTypeFilterIndex < 0) currentTypeFilterIndex = 0;
                ImGui.SetNextItemWidth(100f);
                if (ImGui.Combo("Type Filter##db_type_filter", ref currentTypeFilterIndex, typeFilters, typeFilters.Length))
                {
                    typeFilter = typeFilters[currentTypeFilterIndex];
                }

                ImGui.SameLine();
                // Filter by Category
                var categories = new List<string> { "All" };
                categories.AddRange(manager.GetCategories());
                string[] catFilters = categories.ToArray();
                int currentCatFilterIndex = Array.IndexOf(catFilters, categoryFilter);
                if (currentCatFilterIndex < 0) currentCatFilterIndex = 0;
                ImGui.SetNextItemWidth(120f);
                if (ImGui.Combo("Category Filter##db_cat_filter", ref currentCatFilterIndex, catFilters, catFilters.Length))
                {
                    categoryFilter = catFilters[currentCatFilterIndex];
                }

                ImGui.Spacing();

                // Entries Table
                var filtered = manager.Inspirations.Where(e => {
                    if (!string.IsNullOrEmpty(searchFilter) &&
                        !e.Content.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) &&
                        !e.Category.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (typeFilter != "All" && !e.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (categoryFilter != "All" && !e.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                        return false;

                    return true;
                }).ToList();

                // Display Table
                if (ImGui.BeginTable("InspirationsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new Vector2(-1, 200)))
                {
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60f);
                    ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 100f);
                    ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 90f);
                    ImGui.TableHeadersRow();

                    foreach (var entry in filtered)
                    {
                        ImGui.PushID(entry.Id.ToString());
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        Vector4 typeCol = entry.Type.Equals("Dare", StringComparison.OrdinalIgnoreCase) ? new Vector4(1f, 0.4f, 0.4f, 1f) : new Vector4(0.4f, 0.8f, 1f, 1f);
                        ImGui.TextColored(typeCol, entry.Type);

                        ImGui.TableNextColumn();
                        ImGui.Text(entry.Category);

                        ImGui.TableNextColumn();
                        ImGui.Text(entry.Content);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(entry.Content);
                        }

                        ImGui.TableNextColumn();
                        if (ImGui.Button("Edit"))
                        {
                            editingEntryId = entry.Id;
                            newEntryContent = entry.Content;
                            newEntryTypeIndex = entry.Type.Equals("Dare", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                            newEntryCategory = entry.Category;
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Del"))
                        {
                            manager.DeleteEntry(entry.Id);
                            if (editingEntryId == entry.Id)
                            {
                                editingEntryId = null;
                                newEntryContent = string.Empty;
                            }
                        }

                        ImGui.PopID();
                    }

                    ImGui.EndTable();
                }

                ImGui.Spacing();
                if (ImGui.Button("Reset to Default Presets"))
                {
                    ImGui.OpenPopup("ResetDatabaseConfirm");
                }

                bool popupReset = true;
                if (ImGui.BeginPopupModal("ResetDatabaseConfirm", ref popupReset, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Are you sure you want to reset the inspiration database?\nThis will overwrite all custom entries and restore defaults.");
                    ImGui.Separator();
                    if (ImGui.Button("Yes", new Vector2(120, 0)))
                    {
                        manager.ResetToDefaults();
                        editingEntryId = null;
                        newEntryContent = string.Empty;
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

            ImGui.EndTabBar();
        }
    }

    private void DrawInspirationButtonAndPopup()
    {
        if (ImGui.Button("Get Inspiration"))
        {
            ImGui.OpenPopup("InspirationToolPopup");
        }

        bool openInspiration = true;
        if (ImGui.BeginPopupModal("InspirationToolPopup", ref openInspiration, ImGuiWindowFlags.AlwaysAutoResize))
        {
            var manager = plugin.InspirationManager;

            // Filters
            string[] types = { "All", "Truth", "Dare" };
            int typeIdx = Array.IndexOf(types, insTypeFilter);
            if (typeIdx < 0) typeIdx = 0;
            ImGui.SetNextItemWidth(100f);
            if (ImGui.Combo("Type##ins_tool", ref typeIdx, types, types.Length))
            {
                insTypeFilter = types[typeIdx];
            }

            ImGui.SameLine();

            var categories = new List<string> { "All" };
            categories.AddRange(manager.GetCategories());
            string[] cats = categories.ToArray();
            int catIdx = Array.IndexOf(cats, insCategoryFilter);
            if (catIdx < 0) catIdx = 0;
            ImGui.SetNextItemWidth(120f);
            if (ImGui.Combo("Category##ins_tool", ref catIdx, cats, cats.Length))
            {
                insCategoryFilter = cats[catIdx];
            }

            ImGui.Spacing();

            if (ImGui.Button("Roll Inspiration", new Vector2(230, 30)))
            {
                var entry = manager.GetRandom(insTypeFilter, insCategoryFilter);
                rolledInspirationText = entry != null ? entry.Content : "No entries found matching filters.";
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextWrapped(rolledInspirationText);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Copy to Clipboard", new Vector2(120, 0)) && !string.IsNullOrEmpty(rolledInspirationText))
            {
                ImGui.SetClipboardText(rolledInspirationText);
            }

            ImGui.SameLine();
            if (ImGui.Button("Send to Chat", new Vector2(120, 0)) && !string.IsNullOrEmpty(rolledInspirationText))
            {
                ChatSender.SendMessage(this.plugin.Configuration.ChatPrefix.Trim() + " " + rolledInspirationText);
            }

            ImGui.SameLine();
            if (ImGui.Button("Close", new Vector2(80, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }
}
