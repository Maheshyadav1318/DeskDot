using DeskDot.Core;

namespace DeskDot.Models;

/// <summary>A single media item surfaced in the island's now-playing card.</summary>
public sealed record TrackInfo(string Title, string Artist, string Album = "")
{
    public static readonly TrackInfo Empty = new("Nothing playing", "Start something in any player");

    public bool IsEmpty => ReferenceEquals(this, Empty);
}

/// <summary>Point-in-time CPU / memory reading.</summary>
public sealed record SystemSnapshot(double CpuPercent, double UsedMemoryGb, double TotalMemoryGb)
{
    public static readonly SystemSnapshot Empty = new(0, 0, 0);

    public double MemoryPercent => SystemFormat.MemoryPercent(UsedMemoryGb, TotalMemoryGb);

    public string CpuText => SystemFormat.Cpu(CpuPercent);

    public string MemoryText => SystemFormat.Memory(UsedMemoryGb);
}

/// <summary>An upcoming calendar-style item shown under the clock.</summary>
public sealed record AgendaItem(string Time, string Title)
{
    public static readonly AgendaItem None = new(string.Empty, "Nothing on the agenda");
}
