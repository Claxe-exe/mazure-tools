using System.Runtime.InteropServices;

namespace MazureTools.Core.Native;

/// <summary>
/// Reads GPU utilisation from the Windows "GPU Engine" performance counters through PDH
/// (the same source Task Manager uses). Works for any vendor on Windows 10 1709+ with a WDDM 2.0+ driver.
/// </summary>
internal sealed class PdhGpuCounter : IDisposable
{
    private const string CounterPath = @"\GPU Engine(*)\Utilization Percentage";
    private const uint PdhFmtDouble = 0x00000200;
    private const uint PdhMoreData = 0x800007D2;
    private const uint CStatusValidData = 0;
    private const uint CStatusNewData = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct CounterValueItem
    {
        public IntPtr Name;
        public uint Status;
        public double Value;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQueryW(string? dataSource, IntPtr userData, out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddEnglishCounterW(IntPtr query, string counterPath, IntPtr userData, out IntPtr counter);

    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterArrayW(IntPtr counter, uint format, ref uint bufferSize, out uint itemCount, IntPtr buffer);

    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(IntPtr query);

    private IntPtr _query;
    private IntPtr _counter;
    private IntPtr _buffer = IntPtr.Zero;
    private uint _bufferSize;

    /// <summary>False when this machine exposes no GPU counters (old drivers, some VMs).</summary>
    public bool IsAvailable { get; }

    public PdhGpuCounter()
    {
        try
        {
            if (PdhOpenQueryW(null, IntPtr.Zero, out _query) != 0)
                return;

            if (PdhAddEnglishCounterW(_query, CounterPath, IntPtr.Zero, out _counter) != 0)
            {
                PdhCloseQuery(_query);
                _query = IntPtr.Zero;
                return;
            }

            // Prime the query: utilisation is a rate and needs two samples.
            PdhCollectQueryData(_query);
            IsAvailable = true;
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    /// <summary>
    /// Highest per-engine utilisation (0-100), or null when no valid sample exists yet.
    /// Instance names look like <c>pid_1234_luid_0x0_0xD8E7_phys_0_eng_0_engtype_3D</c>;
    /// values are summed across processes per engine and the busiest engine is reported.
    /// </summary>
    public double? Sample()
    {
        if (!IsAvailable || PdhCollectQueryData(_query) != 0)
            return null;

        var size = _bufferSize;
        var status = PdhGetFormattedCounterArrayW(_counter, PdhFmtDouble, ref size, out var count, _buffer);
        if (status == PdhMoreData)
        {
            EnsureBuffer(size);
            size = _bufferSize;
            status = PdhGetFormattedCounterArrayW(_counter, PdhFmtDouble, ref size, out count, _buffer);
        }

        if (status != 0 || count == 0)
            return null;

        var itemSize = Marshal.SizeOf<CounterValueItem>();
        var perEngine = new Dictionary<string, double>();
        for (var i = 0; i < count; i++)
        {
            var item = Marshal.PtrToStructure<CounterValueItem>(_buffer + i * itemSize);
            if (item.Status is not (CStatusValidData or CStatusNewData))
                continue;

            var name = Marshal.PtrToStringUni(item.Name);
            var luidIndex = name?.IndexOf("luid_", StringComparison.Ordinal) ?? -1;
            if (luidIndex < 0)
                continue;

            var engine = name![luidIndex..];
            perEngine[engine] = perEngine.GetValueOrDefault(engine) + item.Value;
        }

        return perEngine.Count == 0 ? null : Math.Clamp(perEngine.Values.Max(), 0, 100);
    }

    private void EnsureBuffer(uint size)
    {
        if (_buffer != IntPtr.Zero)
            Marshal.FreeHGlobal(_buffer);

        _buffer = Marshal.AllocHGlobal((int)size);
        _bufferSize = size;
    }

    public void Dispose()
    {
        if (_query != IntPtr.Zero)
        {
            PdhCloseQuery(_query);
            _query = IntPtr.Zero;
        }

        if (_buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
        }
    }
}
