using DeskDot.Models;

namespace DeskDot.Core;

/// <summary>Formatting rules for the CPU/RAM read-outs (pure, unit tested).</summary>
public static class SystemFormat
{
    /// <summary>Compact CPU text, e.g. <c>14%</c>.</summary>
    public static string Cpu(double percent) => $"{Math.Clamp(percent, 0, 100):0}%";

    /// <summary>
    /// Compact memory text for the pill: <c>11G</c> above 10 GB, otherwise one decimal
    /// (<c>8.4G</c>) so small values stay informative.
    /// </summary>
    public static string Memory(double usedGb) =>
        usedGb >= 10 ? $"{usedGb:0}G" : $"{Math.Max(0, usedGb):0.0}G";

    /// <summary>"8.4 / 16G" style summary used inside the system card.</summary>
    public static string MemorySummary(SystemSnapshot snapshot) =>
        snapshot.TotalMemoryGb <= 0
            ? "—"
            : $"{snapshot.UsedMemoryGb:0.0} / {snapshot.TotalMemoryGb:0}G";

    /// <summary>Memory usage as a 0..100 percentage, safe against a zero total.</summary>
    public static double MemoryPercent(double usedGb, double totalGb) =>
        totalGb <= 0 ? 0 : Math.Clamp(usedGb / totalGb * 100, 0, 100);
}
