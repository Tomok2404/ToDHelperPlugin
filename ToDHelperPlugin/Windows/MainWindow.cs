using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;

namespace ToDHelperPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

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

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ToDMainWindowTabBar"))
        {
            if (ImGui.BeginTabItem("Host Panel"))
            {
                
                ImGui.Separator();
                
                // --- Header Area ---
                ImGui.Text("Round Control");
                ImGui.SameLine(ImGui.GetWindowWidth() - 120);
                ImGui.Text("Timer: 00:00");

                if (ImGui.Button("Start Round", new Vector2(-1, 30)))
                {
                    // Logic to start the round
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
                            ImGui.Text("Player A - 99");
                            ImGui.Text("Player B - 12");
                        }
                    }

                    if (ImGui.Button("Announce Players (Dice rolls)", new Vector2(-1, 0)))
                    {
                        // Logic to announce winners of the dice roll
                    }

                    // --- Right Column (Balancing & Controls) ---
                    ImGui.TableNextColumn();
                    ImGui.Text("Balancing Summary");
                    using (var child = ImRaii.Child("BalancingArea", new Vector2(-1, 80), true))
                    {
                        if (child.Success)
                        {
                            ImGui.Text("Player A: 0 given, 1 received");
                            ImGui.Text("Player B: 1 given, 0 received");
                        }
                    }
                    
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                    
                    ImGui.Text("Giver:");
                    if (ImGui.BeginCombo("##GiverCombo", "Select Giver..."))
                    {
                        ImGui.EndCombo();
                    }
                    
                    ImGui.Text("Receiver:");
                    if (ImGui.BeginCombo("##ReceiverCombo", "Select Receiver..."))
                    {
                        ImGui.EndCombo();
                    }
                    
                    ImGui.Spacing();
                    if (ImGui.Button("Announce Custom Players", new Vector2(-1, 0)))
                    {
                        // Logic to announce manually selected players
                    }

                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Balancing"))
            {
                ImGui.Text("Player balancing and statistics will appear here.");
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}
