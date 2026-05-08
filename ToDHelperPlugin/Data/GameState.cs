using System;
using System.Collections.Generic;

namespace ToDHelperPlugin.Data;

public class PlayerStats
{
    public string Name { get; set; } = string.Empty;

    // Tracking
    public int TruthsGiven { get; set; }
    public int DaresGiven { get; set; }
    public int TruthsReceived { get; set; }
    public int DaresReceived { get; set; }
    
    public int RoundsParticipated { get; set; }
    public float ExpectedActions { get; set; }

    public int TotalGiven => TruthsGiven + DaresGiven;
    public int TotalReceived => TruthsReceived + DaresReceived;
}

public class GameState
{
    // Settings toggles
    public bool IsAutomaticMode { get; set; } = false;
    public bool IsPrivateMode { get; set; } = false; // Only watch specific chat
    
    public int RoundsPlayed { get; set; } = 0;
    
    // Player Database
    public Dictionary<string, PlayerStats> Players { get; set; } = new();
    
    // Current Round State
    public bool IsRoundActive { get; set; } = false;
    public bool RoundBalanced { get; set; } = false;
    public bool NeedsAutoSelection { get; set; } = false;
    public DateTime RoundEndTime { get; set; }
    public int TotalDurationSeconds { get; set; }
    public bool RoundReminderTriggered { get; set; }
    public Dictionary<string, int> CurrentRoundRolls { get; set; } = new();

    public PlayerStats GetOrCreatePlayer(string name)
    {
        if (!Players.TryGetValue(name, out var stats))
        {
            stats = new PlayerStats { Name = name };
            Players[name] = stats;
        }
        return stats;
    }

    public void AddRoll(string name, int roll)
    {
        if (IsRoundActive)
        {
            if (CurrentRoundRolls.ContainsKey(name)) return;
            CurrentRoundRolls[name] = roll;
            GetOrCreatePlayer(name);
            // Mark as participated if not already counted for this round
            // (We will handle incrementing RoundsParticipated properly when a round starts/ends)
        }
    }

    public void StartRound(int durationSeconds)
    {
        CurrentRoundRolls.Clear();
        IsRoundActive = true;
        RoundBalanced = false;
        NeedsAutoSelection = false;
        RoundsPlayed++;
        TotalDurationSeconds = durationSeconds;
        RoundReminderTriggered = false;
        RoundEndTime = DateTime.Now.AddSeconds(durationSeconds);
    }

    public void EndRound()
    {
        IsRoundActive = false;
        NeedsAutoSelection = true;
    }
}
