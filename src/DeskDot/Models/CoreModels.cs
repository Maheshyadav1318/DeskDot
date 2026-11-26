using DeskDot.Core;

namespace DeskDot.Models;

/// <summary>A single media item surfaced in the soundtrack card.</summary>
public sealed record TrackInfo(string Title, string Artist, string Album = "")
{
    public static readonly TrackInfo Empty = new("Quiet for now", "Nothing playing");
}

/// <summary>Point-in-time CPU / memory reading.</summary>
public sealed record SystemSnapshot(double CpuPercent, double UsedMemoryGb, double TotalMemoryGb)
{
    public static readonly SystemSnapshot Empty = new(0, 0, 0);

    public double MemoryPercent => SystemFormat.MemoryPercent(UsedMemoryGb, TotalMemoryGb);

    public string CpuText => SystemFormat.Cpu(CpuPercent);

    public string MemoryText => SystemFormat.Memory(UsedMemoryGb);
}
