using System;

namespace ToDHelperPlugin.Data;

public class InspirationEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "Truth"; // "Truth" or "Dare"
    public string Category { get; set; } = "General"; // e.g. "FFXIV", "Glamour", "Social", "General"
}

public class InspirationDatabase
{
    public System.Collections.Generic.List<InspirationEntry> Inspirations { get; set; } = new();
}
