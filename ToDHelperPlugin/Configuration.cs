using Dalamud.Configuration;
using System;

namespace ToDHelperPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool AutomaticMode { get; set; } = false;
    public int TimerMinutes { get; set; } = 1;
    public int TimerSeconds { get; set; } = 30;

    // 0 = /random, 1 = /dice
    public int RollMode { get; set; } = 0;

    public bool WatchParty { get; set; } = true;
    public bool WatchAlliance { get; set; } = true;
    public bool WatchFC { get; set; } = true;
    public bool WatchOthers { get; set; } = false;

    // Messages Settings
    public bool EnableRoundStartMsg { get; set; } = true;
    public System.Collections.Generic.List<string> RoundStartMessages { get; set; } = new() { "A new round has started! You have [Timer] to roll!" };

    public bool EnableRoundReminderMsg { get; set; } = true;
    public System.Collections.Generic.List<string> RoundReminderMessages { get; set; } = new() { "Hurry up! Only [RemainingTimer] left!" };

    public bool EnableRoundClosedMsg { get; set; } = true;
    public System.Collections.Generic.List<string> RoundClosedMessages { get; set; } = new() { "Round [RoundNumber] closed! Thank you for participating." };

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
