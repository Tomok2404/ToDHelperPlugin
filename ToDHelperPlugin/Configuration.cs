using Dalamud.Configuration;
using System;

namespace ToDHelperPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool AutomaticMode { get; set; } = true;
    public int TimerMinutes { get; set; } = 1;
    public int TimerSeconds { get; set; } = 30;

    // 0 = /random, 1 = /dice
    public int RollMode { get; set; } = 0;

    public bool WatchParty { get; set; } = true;
    public bool WatchAlliance { get; set; } = true;
    public bool WatchFC { get; set; } = true;
    public bool WatchOthers { get; set; } = false;

    public bool EnableBalanceTracking { get; set; } = true;
    public bool SeparateBalancingTracking { get; set; } = false;
    public int BalancingCalculationMode { get; set; } = 0; // 0 = Percentage, 1 = Deficit
    public bool EnableDynamicTags { get; set; } = true;

    // Messages Settings
    public string ChatPrefix { get; set; } = "/p";
    public int RoundReminderTime { get; set; } = 15;

    public bool EnableRoundStartMsg { get; set; } = true;
    public System.Collections.Generic.List<string> RoundStartMessages { get; set; } = new();

    public bool EnableRoundReminderMsg { get; set; } = true;
    public System.Collections.Generic.List<string> RoundReminderMessages { get; set; } = new();

    public bool EnableRoundClosedMsg { get; set; } = true;
    public System.Collections.Generic.List<string> RoundClosedMessages { get; set; } = new();

    public bool EnableDiceAnnounceMsg { get; set; } = true;
    public System.Collections.Generic.List<string> DiceAnnounceMessages { get; set; } = new();

    public bool EnableCustomAnnounceMsg { get; set; } = true;
    public System.Collections.Generic.List<string> CustomAnnounceMessages { get; set; } = new();

    public System.Collections.Generic.Dictionary<string, Data.PlayerStats> SavedPlayers { get; set; } = new();
    public System.Collections.Generic.List<Data.RecordedTurn> SavedTurnLog { get; set; } = new();

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
