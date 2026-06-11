using System;

namespace ToDHelperPlugin.Data;

[Serializable]
public class RecordedTurn
{
    public string Giver { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public bool IsDare { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
