using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ToDHelperPlugin.Data;

public class InspirationManager
{
    private readonly string filePath;
    private InspirationDatabase database = new();
    private readonly Random random = new();

    public List<InspirationEntry> Inspirations => database.Inspirations;

    public InspirationManager(string configDirectory)
    {
        if (!Directory.Exists(configDirectory))
        {
            try
            {
                Directory.CreateDirectory(configDirectory);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to create config directory: {ex.Message}");
            }
        }
        filePath = Path.Combine(configDirectory, "tod_inspirations.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var db = JsonSerializer.Deserialize<InspirationDatabase>(json);
                if (db != null)
                {
                    database = db;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to load ToD Inspirations: {ex.Message}");
        }

        // Default fallback or first-time initialization
        ResetToDefaults();
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(database, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to save ToD Inspirations: {ex.Message}");
        }
    }

    public void AddEntry(string content, string type, string category)
    {
        database.Inspirations.Add(new InspirationEntry
        {
            Content = content,
            Type = type,
            Category = category
        });
        Save();
    }

    public void UpdateEntry(Guid id, string content, string type, string category)
    {
        var entry = database.Inspirations.FirstOrDefault(e => e.Id == id);
        if (entry != null)
        {
            entry.Content = content;
            entry.Type = type;
            entry.Category = category;
            Save();
        }
    }

    public void DeleteEntry(Guid id)
    {
        database.Inspirations.RemoveAll(e => e.Id == id);
        Save();
    }

    public InspirationEntry? GetRandom(string? type = null, string? category = null)
    {
        var query = database.Inspirations.AsEnumerable();
        if (!string.IsNullOrEmpty(type) && !type.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrEmpty(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();
        if (list.Count == 0) return null;
        return list[random.Next(list.Count)];
    }

    public List<string> GetCategories()
    {
        return database.Inspirations
            .Select(e => e.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }

    public void ResetToDefaults()
    {
        database = new InspirationDatabase();

        // Preset Truths
        AddDefault("What is your biggest gear set/glam regret?", "Truth", "Glamour");
        AddDefault("Have you ever accidentally rolled Need on a minion or mount you already owned?", "Truth", "FFXIV");
        AddDefault("Who is your favorite Scion and why?", "Truth", "FFXIV");
        AddDefault("What is the longest you've gone without changing your main job?", "Truth", "Gameplay");
        AddDefault("Which alliance raid boss is your absolute nemesis?", "Truth", "Battles");
        AddDefault("What's the most gil you've spent on a single cosmetic item?", "Truth", "Gil");
        AddDefault("Which city-state has the best background music?", "Truth", "General");
        AddDefault("Have you ever fallen off the edge of an arena in a trial immediately after being raised?", "Truth", "Battles");
        AddDefault("What is your least favorite dungeon to run in leveling roulette?", "Truth", "Gameplay");
        AddDefault("If you could marry any NPC in FFXIV, who would it be?", "Truth", "FFXIV");

        // Preset Dares
        AddDefault("Do a backflip off the Kugane Tower (or try to).", "Dare", "Gameplay");
        AddDefault("Run a level 50 dungeon unsynced using only your auto-attack.", "Dare", "Gameplay");
        AddDefault("Send a /tell to a random player saying 'The prophecy has been fulfilled!' and log out.", "Dare", "Social");
        AddDefault("Equip your ugliest gear set and walk around Limsa for 5 minutes.", "Dare", "Glamour");
        AddDefault("Perform a dance emote in front of a grand company leader.", "Dare", "Social");
        AddDefault("Say your favorite Dad Joke in Party Chat.", "Dare", "Social");
        AddDefault("Discard a single, cheap potion or standard item from your inventory in front of everyone.", "Dare", "Social");
        AddDefault("Switch to a job you haven't played in months and put together a hotbar blindly for a target dummy.", "Dare", "Gameplay");
        AddDefault("Change your title to something silly or unusual for the rest of the day.", "Dare", "General");
        AddDefault("Stand on a table in a tavern/inn and shout a motivational quote.", "Dare", "Social");

        Save();
    }

    private void AddDefault(string content, string type, string category)
    {
        database.Inspirations.Add(new InspirationEntry
        {
            Content = content,
            Type = type,
            Category = category
        });
    }
}
