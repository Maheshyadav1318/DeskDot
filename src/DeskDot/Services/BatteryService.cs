using DeskDot.Core;

namespace DeskDot.Services;

/// <summary>Battery / AC state via <c>GetSystemPowerStatus</c>. Desktops report "No battery".</summary>
public sealed class BatteryService
{
    public BatteryReading Read()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            return BatteryReading.Unknown;
        }

        // 255 means "unknown" in the Win32 contract.
        var hasBattery = status.BatteryFlag != 128 && status.BatteryLifePercent != 255;
        var percent = hasBattery ? status.BatteryLifePercent : -1;
        var charging = (status.BatteryFlag & 8) != 0 || status.AclLineStatus == 1;

        return new BatteryReading(
            hasBattery,
            percent,
            charging && hasBattery,
            PowerStatus.Describe(percent, charging, hasBattery),
            PowerStatus.StateLabel(percent, charging, hasBattery),
            PowerStatus.IsLow(percent, charging, hasBattery));
    }
}

/// <summary>One battery reading.</summary>
public sealed record BatteryReading(
    bool HasBattery,
    int Percent,
    bool IsCharging,
    string Display,
    string StateLabel,
    bool IsLow)
{
    public static readonly BatteryReading Unknown = new(false, -1, false, "No battery", "Desktop", false);
}
