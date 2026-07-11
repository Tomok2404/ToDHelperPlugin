using System;
using System.Collections.Generic;
using System.Linq;

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
    
    public int RoundsPlayed
    {
        get => TurnLog.Count + (IsRoundActive ? 1 : 0);
        set { }
    }
    
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
    public List<RecordedTurn> TurnLog { get; set; } = new();

    public PlayerStats GetOrCreatePlayer(string name)
    {
        name = Plugin.NormalizePlayerName(name, "");
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
        TotalDurationSeconds = durationSeconds;
        RoundReminderTriggered = false;
        RoundEndTime = DateTime.Now.AddSeconds(durationSeconds);
    }

    public void EndRound()
    {
        IsRoundActive = false;
        NeedsAutoSelection = true;
    }

    public void RecordTurn(string giver, string receiver, bool isDare)
    {
        var gStats = GetOrCreatePlayer(giver);
        var rStats = GetOrCreatePlayer(receiver);

        if (isDare)
        {
            gStats.DaresGiven++;
            rStats.DaresReceived++;
        }
        else
        {
            gStats.TruthsGiven++;
            rStats.TruthsReceived++;
        }

        // Increment rounds participated and expected actions for everyone in the game
        int totalPlayers = Players.Count;
        float expectedPerPlayer = totalPlayers > 0 ? 2.0f / totalPlayers : 0f;
        foreach (var player in Players.Values)
        {
            player.RoundsParticipated++;
            player.ExpectedActions += expectedPerPlayer;
        }

        TurnLog.Add(new RecordedTurn
        {
            Giver = giver,
            Receiver = receiver,
            IsDare = isDare,
            Timestamp = DateTime.Now
        });
    }

    public void UndoLastTurn()
    {
        if (TurnLog.Count == 0) return;

        var lastTurn = TurnLog.Last();
        TurnLog.RemoveAt(TurnLog.Count - 1);

        if (Players.TryGetValue(lastTurn.Giver, out var gStats))
        {
            if (lastTurn.IsDare)
                gStats.DaresGiven = Math.Max(0, gStats.DaresGiven - 1);
            else
                gStats.TruthsGiven = Math.Max(0, gStats.TruthsGiven - 1);
        }

        if (Players.TryGetValue(lastTurn.Receiver, out var rStats))
        {
            if (lastTurn.IsDare)
                rStats.DaresReceived = Math.Max(0, rStats.DaresReceived - 1);
            else
                rStats.TruthsReceived = Math.Max(0, rStats.TruthsReceived - 1);
        }

        // Decrement rounds participated and expected actions for everyone in the game
        int totalPlayers = Players.Count;
        float expectedPerPlayer = totalPlayers > 0 ? 2.0f / totalPlayers : 0f;
        foreach (var player in Players.Values)
        {
            player.RoundsParticipated = Math.Max(0, player.RoundsParticipated - 1);
            player.ExpectedActions = Math.Max(0f, player.ExpectedActions - expectedPerPlayer);
        }
    }

    public void UpdateRecordedTurn(int index, string newGiver, string newReceiver, bool newIsDare)
    {
        if (index < 0 || index >= TurnLog.Count) return;

        var oldTurn = TurnLog[index];

        // 1. Revert stats for the old turn (given/received counts only)
        if (Players.TryGetValue(oldTurn.Giver, out var oldGStats))
        {
            if (oldTurn.IsDare)
                oldGStats.DaresGiven = Math.Max(0, oldGStats.DaresGiven - 1);
            else
                oldGStats.TruthsGiven = Math.Max(0, oldGStats.TruthsGiven - 1);
        }

        if (Players.TryGetValue(oldTurn.Receiver, out var oldRStats))
        {
            if (oldTurn.IsDare)
                oldRStats.DaresReceived = Math.Max(0, oldRStats.DaresReceived - 1);
            else
                oldRStats.TruthsReceived = Math.Max(0, oldRStats.TruthsReceived - 1);
        }

        // 2. Update the turn object
        oldTurn.Giver = newGiver;
        oldTurn.Receiver = newReceiver;
        oldTurn.IsDare = newIsDare;

        // 3. Apply stats for the new turn (given/received counts only)
        var newGStats = GetOrCreatePlayer(newGiver);
        var newRStats = GetOrCreatePlayer(newReceiver);

        if (newIsDare)
        {
            newGStats.DaresGiven++;
            newRStats.DaresReceived++;
        }
        else
        {
            newGStats.TruthsGiven++;
            newRStats.TruthsReceived++;
        }
    }
}
