using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ToDHelperPlugin.Windows;
using ToDHelperPlugin.Data;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace ToDHelperPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName1 = "/tod";
    private const string CommandName2 = "/todhelper";

    public Configuration Configuration { get; init; }
    public GameState GameState { get; init; } = new();

    public readonly WindowSystem WindowSystem = new("ToDHelperPlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName1, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the ToD Helper main window"
        });

        CommandManager.AddHandler(CommandName2, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the ToD Helper main window"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
        
        ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        CommandManager.RemoveHandler(CommandName1);
        CommandManager.RemoveHandler(CommandName2);
        
        ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        // Only monitor if Automatic Mode and Round is active
        if (!Configuration.AutomaticMode || !GameState.IsRoundActive)
            return;

        // Mode 0: /random. FFXIV usually sends standard /random rolls as XivChatType.Standard or SystemMessage.
        // Mode 1: /dice. We have to filter by the user's selected channels.
        
        if (Configuration.RollMode == 0)
        {
            // Usually /random is public, we would check if it's a dice roll.
        }
        else if (Configuration.RollMode == 1)
        {
            // For /dice, we check if the channel matches the allowed ones.
            bool allowed = false;
            if (Configuration.WatchParty && type == XivChatType.Party) allowed = true;
            if (Configuration.WatchAlliance && type == XivChatType.Alliance) allowed = true;
            if (Configuration.WatchFC && type == XivChatType.FreeCompany) allowed = true;
            if (Configuration.WatchOthers)
            {
                // Linkshells and CrossWorldLinkshells are types 16-23 and 37-44 usually
                if (type >= XivChatType.Ls1 && type <= XivChatType.Ls8) allowed = true;
                if (type >= XivChatType.CrossLinkShell1 && type <= XivChatType.CrossLinkShell8) allowed = true;
            }
            
            if (!allowed) return;
        }

        // Logic to extract roll value from 'message.TextValue' and player from 'sender.TextValue' goes here
        // e.g. GameState.AddRoll(sender.TextValue, parsedRoll);
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
