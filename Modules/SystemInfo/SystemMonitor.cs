using System.Runtime.InteropServices;
using MazureTools.Core.Native;

namespace MazureTools.Modules.SystemInfo;

/// <summary>
/// Shared source of live CPU / RAM / GPU numbers. Sampling runs only while at least one subscriber exists,
/// and every subscriber gets the same snapshot (so two consumers never disturb each other's CPU delta).
/// The expensive GPU counter is only opened while some subscriber asks for it.
/// Subscribe/unsubscribe on the UI thread; handlers are invoked on the UI thread.
/// </summary>
public interface ISystemMonitor
{
    /// <param name="includeGpu">False for consumers that only need CPU/RAM (keeps the GPU counters closed).</param>
    IDisposable Subscribe(Action<MetricsSnapshot> handler, bool includeGpu = true);
}

public sealed class SystemMonitor : ISystemMonitor
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly List<Subscriber> _subscribers = [];
    private CancellationTokenSource? _cts;

    public IDisposable Subscribe(Action<MetricsSnapshot> handler, bool includeGpu = true)
    {
        var subscriber = new Subscriber(handler, includeGpu);
        _subscribers.Add(subscriber);
        if (_subscribers.Count == 1)
            Start();

        return new Subscription(() => Unsubscribe(subscriber));
    }

    private void Unsubscribe(Subscriber subscriber)
    {
        _subscribers.Remove(subscriber);
        if (_subscribers.Count == 0)
        {
            _cts?.Cancel();
            _cts = null;
        }
    }

    private void Start()
    {
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken token)
    {
        PdhGpuCounter? gpu = null;
        try
        {
            var cpu = new CpuSampler();
            Publish(Sample(cpu, null));

            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(token))
            {
                // Open / close the GPU counter as consumers come and go (decided on the UI thread).
                var wantGpu = _subscribers.Any(s => s.IncludeGpu);
                if (wantGpu && gpu is null)
                    gpu = await Task.Run(() => new PdhGpuCounter(), token);
                else if (!wantGpu && gpu is not null)
                {
                    gpu.Dispose();
                    gpu = null;
                }

                var counter = gpu;
                var snapshot = await Task.Run(() => Sample(cpu, counter), token);
                Publish(snapshot);
            }
        }
        catch (OperationCanceledException)
        {
            // Last subscriber left.
        }
        finally
        {
            gpu?.Dispose();
        }
    }

    private void Publish(MetricsSnapshot snapshot)
    {
        foreach (var subscriber in _subscribers.ToArray())
            subscriber.Handler(snapshot);
    }

    private static MetricsSnapshot Sample(CpuSampler cpu, PdhGpuCounter? gpu)
    {
        var status = new NativeMethods.MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>() };
        var memory = NativeMethods.GlobalMemoryStatusEx(ref status)
            ? new MemoryInfo((long)status.ullTotalPhys, (long)status.ullAvailPhys)
            : new MemoryInfo(0, 0);

        return new MetricsSnapshot(
            cpu.Sample(),
            memory,
            gpu?.Sample(),
            gpu?.IsAvailable ?? true, // "not asked for" is not the same as "not supported"
            TimeSpan.FromMilliseconds(Environment.TickCount64));
    }

    private sealed record Subscriber(Action<MetricsSnapshot> Handler, bool IncludeGpu);

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private Action? _onDispose = onDispose;

        public void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }
    }

    /// <summary>Whole-system CPU load from the kernel's idle/kernel/user tick counters.</summary>
    private sealed class CpuSampler
    {
        private ulong _idle;
        private ulong _total;
        private bool _hasBaseline;

        public double? Sample()
        {
            if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
                return null;

            var total = kernel + user; // kernel time already includes idle time
            double? result = null;
            if (_hasBaseline && total > _total)
            {
                var totalDelta = total - _total;
                var idleDelta = idle - _idle;
                result = Math.Clamp((1.0 - (double)idleDelta / totalDelta) * 100.0, 0, 100);
            }

            _idle = idle;
            _total = total;
            _hasBaseline = true;
            return result;
        }
    }
}
