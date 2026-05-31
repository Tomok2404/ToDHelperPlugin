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
using Dalamud.Game.Chat;

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
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

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
        GameState.Players = Configuration.SavedPlayers;

        // Ensure default messages exist if lists are completely empty
        if (Configuration.RoundStartMessages.Count == 0)
            Configuration.RoundStartMessages.Add("A new round has started! You have [Timer] to roll!");
        if (Configuration.RoundReminderMessages.Count == 0)
            Configuration.RoundReminderMessages.Add("Hurry up! Only [RemainingTimer] left!");
        if (Configuration.RoundClosedMessages.Count == 0)
            Configuration.RoundClosedMessages.Add("Round [RoundNumber] closed! Thank you for participating.");
        if (Configuration.DiceAnnounceMessages.Count == 0)
            Configuration.DiceAnnounceMessages.Add("The dice have spoken! [GiverName] asks [ReceiverName]!");
        if (Configuration.CustomAnnounceMessages.Count == 0)
            Configuration.CustomAnnounceMessages.Add("Get ready! [CustomGiverName] asks [CustomReceiverName]!");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

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
        Framework.Update += OnUpdate;
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
        Framework.Update -= OnUpdate;
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    private void OnChatMessage(IHandleableChatMessage message)
    {
        // Only monitor if Automatic Mode and Round is active
        if (!Configuration.AutomaticMode || !GameState.IsRoundActive)
            return;

        var type = message.LogKind;
        var sender = message.Sender;
        var msg = message.Message;

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

        // Parse standard roll text like "You roll a 59 (out of 100)." or "Player Name rolls a 59 (out of 100)."
        // Parse standard roll text like "Random! You roll a 🎲777." or "Random! Player Name rolls a 🎲59."
        var match = System.Text.RegularExpressions.Regex.Match(msg.TextValue, @"(?:Random!\s+)?(?:(?<name>.+?)\s+rolls?|You roll)\s+a\s+[^\d]*(?<roll>\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string playerName = match.Groups["name"].Success ? match.Groups["name"].Value : ObjectTable[0]?.Name.TextValue ?? "You";
            if (int.TryParse(match.Groups["roll"].Value, out int parsedRoll))
            {
                GameState.AddRoll(playerName, parsedRoll);
            }
        }
    }

    private void OnUpdate(IFramework framework)
    {
        if (!GameState.IsRoundActive) return;

        var now = System.DateTime.Now;
        var remaining = (GameState.RoundEndTime - now).TotalSeconds;

        if (remaining <= 0)
        {
            // End Round
            GameState.EndRound();
            Configuration.Save();
            if (Configuration.EnableRoundClosedMsg && Configuration.RoundClosedMessages.Count > 0)
            {
                var msg = Configuration.RoundClosedMessages[new System.Random().Next(Configuration.RoundClosedMessages.Count)];
                msg = msg.Replace("[RoundNumber]", GameState.RoundsPlayed.ToString());
                ChatSender.SendMessage(Configuration.ChatPrefix.Trim() + " " + msg);
            }
        }
        else if (!GameState.RoundReminderTriggered && remaining <= Configuration.RoundReminderTime)
        {
            GameState.RoundReminderTriggered = true;
            if (Configuration.EnableRoundReminderMsg && Configuration.RoundReminderMessages.Count > 0)
            {
                var msg = Configuration.RoundReminderMessages[new System.Random().Next(Configuration.RoundReminderMessages.Count)];
                msg = msg.Replace("[RemainingTimer]", $"{(int)remaining}s");
                msg = msg.Replace("[RoundNumber]", GameState.RoundsPlayed.ToString());
                ChatSender.SendMessage(Configuration.ChatPrefix.Trim() + " " + msg);
            }
        }
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
