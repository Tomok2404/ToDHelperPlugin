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

    public int TotalGiven => TruthsGiven + DaresGiven;
    public int TotalReceived => TruthsReceived + DaresReceived;
}

public class GameState
{
    // Settings toggles
    public bool IsAutomaticMode { get; set; } = false;
    public bool IsPrivateMode { get; set; } = false; // Only watch specific chat
    
    // Player Database
    public Dictionary<string, PlayerStats> Players { get; set; } = new();
    
    // Current Round State
    public bool IsRoundActive { get; set; } = false;
    public DateTime RoundEndTime { get; set; }
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
            CurrentRoundRolls[name] = roll;
            
            // Mark as participated if not already counted for this round
            // (We will handle incrementing RoundsParticipated properly when a round starts/ends)
        }
    }

    public void StartRound(int durationSeconds)
    {
        CurrentRoundRolls.Clear();
        IsRoundActive = true;
        RoundEndTime = DateTime.Now.AddSeconds(durationSeconds);
    }

    public void EndRound()
    {
        IsRoundActive = false;
        
        // Update participated rounds for anyone who rolled
        foreach (var player in CurrentRoundRolls.Keys)
        {
            GetOrCreatePlayer(player).RoundsParticipated++;
        }
    }
}
