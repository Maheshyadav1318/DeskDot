using System.Runtime.InteropServices;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Samples CPU load and physical memory usage using only Win32 APIs, so DeskDot has no
/// NuGet dependencies: CPU from a GetSystemTimes delta, memory from GlobalMemoryStatusEx.
/// </summary>
public sealed class SystemStatsService : IDisposable
{
    private readonly object _gate = new();
    private ulong _lastIdle;
    private ulong _lastKernel;
    private ulong _lastUser;
    private bool _hasPreviousSample;
    private bool _disposed;

    public SystemStatsService()
    {
        // Prime the GetSystemTimes baseline; the first Sample() then has a delta to use.
        SampleCpu();
    }

    public SystemSnapshot Sample()
    {
        var cpu = SampleCpu();
        var (usedGb, totalGb) = ReadMemory();
        return new SystemSnapshot(Math.Clamp(cpu, 0, 100), usedGb, totalGb);
    }

    private double SampleCpu()
    {
        if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return 0;
        }

        var idleTicks = ToUInt64(idle);
        var kernelTicks = ToUInt64(kernel);
        var userTicks = ToUInt64(user);

        lock (_gate)
        {
            if (!_hasPreviousSample)
            {
                _lastIdle = idleTicks;
                _lastKernel = kernelTicks;
                _lastUser = userTicks;
                _hasPreviousSample = true;
                return 0;
            }

            var idleDelta = idleTicks - _lastIdle;
            var totalDelta = (kernelTicks - _lastKernel) + (userTicks - _lastUser);

            _lastIdle = idleTicks;
            _lastKernel = kernelTicks;
            _lastUser = userTicks;

            if (totalDelta == 0)
            {
                return 0;
            }

            // Kernel time already includes idle time.
            return (totalDelta - idleDelta) * 100.0 / totalDelta;
        }
    }

    private static (double UsedGb, double TotalGb) ReadMemory()
    {
        var status = new NativeMethods.MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>()
        };

        if (!NativeMethods.GlobalMemoryStatusEx(ref status) || status.TotalPhys == 0)
        {
            return (0, 0);
        }

        const double bytesPerGb = 1024d * 1024d * 1024d;
        return ((status.TotalPhys - status.AvailPhys) / bytesPerGb, status.TotalPhys / bytesPerGb);
    }

    private static ulong ToUInt64(NativeMethods.FileTime time) =>
        ((ulong)time.HighDateTime << 32) | time.LowDateTime;

    public void Dispose() => _disposed = true;
}
