using System.Windows.Input;
using System.Windows.Threading;
using MazureTools.Core;
using MazureTools.Core.Native;
using MazureTools.Modules.Network;
using MazureTools.Modules.Storage;
using MazureTools.Modules.SystemInfo;
using MazureTools.Services;

namespace MazureTools.ViewModels;

/// <summary>
/// Brain of Mazu, the desktop mascot: turns CPU / memory / disk / network / user-idle state into a mood
/// and a short speech bubble. Only CPU and RAM are sampled continuously (cheap); the GPU counters stay closed.
/// </summary>
public sealed class MascotViewModel : ObservableObject
{
    private const double CpuBusyPercent = 90;
    private const double CpuCalmPercent = 75;
    private const int CpuBusySamples = 10;       // 10 s of load before Mazu gets tired
    private const double RamWorryPercent = 92;
    private const double RamCalmPercent = 88;
    private const double DiskLowFreePercent = 10;
    private const int DiskCheckEveryTicks = 60;
    private static readonly TimeSpan SleepyAfter = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan BubbleDuration = TimeSpan.FromSeconds(6);

    // Several lines per situation so Mazu does not get boring; the same line is never picked twice in a row.
    private static readonly string[] HelloLines = ["Mascot_Hello", "Mascot_Hello2", "Mascot_Hello3"];
    private static readonly string[] HappyLines = ["Mascot_Happy1", "Mascot_Happy2", "Mascot_Happy3", "Mascot_Happy4", "Mascot_Happy5", "Mascot_Happy6"];
    private static readonly string[] TiredLines = ["Mascot_Tired1", "Mascot_Tired2", "Mascot_Tired3", "Mascot_Tired4", "Mascot_Tired5"];
    private static readonly string[] SleepyLines = ["Mascot_Sleepy1", "Mascot_Sleepy2", "Mascot_Sleepy3", "Mascot_Sleepy4", "Mascot_Sleepy5"];
    private static readonly string[] OfflineLines = ["Mascot_Offline1", "Mascot_Offline2", "Mascot_Offline3", "Mascot_Offline4"];
    private static readonly string[] RamLines = ["Mascot_WorriedRam", "Mascot_WorriedRam2", "Mascot_WorriedRam3"];
    private static readonly string[] DiskLines = ["Mascot_WorriedDisk", "Mascot_WorriedDisk2", "Mascot_WorriedDisk3"];
    private static readonly string[] WakeLines = ["Mascot_Wake", "Mascot_Wake2", "Mascot_Wake3"];
    private static readonly string[] OnlineLines = ["Mascot_Online", "Mascot_Online2"];
    private static readonly string[] ReliefLines = ["Mascot_Relief", "Mascot_Relief2", "Mascot_Relief3"];
    private const int TicklishPokes = 5;
    private static readonly TimeSpan TicklishWindow = TimeSpan.FromSeconds(8);

    private readonly ISystemMonitor _monitor;
    private readonly IStorageService _storage;
    private readonly INetworkService _network;
    private readonly IWindowController _window;
    private readonly DispatcherTimer _bubbleTimer = new() { Interval = BubbleDuration };
    private readonly Random _random = new();
    private readonly Queue<DateTime> _recentPokes = new();
    private string _lastPicked = string.Empty;
    private readonly MascotMood? _forcedMood;

    private IDisposable? _subscription;
    private MascotMood _mood = MascotMood.Happy;
    private string _bubbleText = string.Empty;
    private int _ticks;
    private int _cpuStreak;
    private bool _ramWorried;
    private double _ramPercent;
    private (string Drive, double FreePercent)? _lowDisk;
    private bool _networkUp = true;
    private bool _started;

    public MascotViewModel(ISystemMonitor monitor, IStorageService storage, INetworkService network, IWindowController window)
    {
        _monitor = monitor;
        _storage = storage;
        _network = network;
        _window = window;

        // Development/screenshot aid: MAZURE_MASCOT_MOOD=Tired (Happy, Tired, Worried, Sleepy, Surprised).
        if (Enum.TryParse<MascotMood>(Environment.GetEnvironmentVariable("MAZURE_MASCOT_MOOD"), true, out var forced))
            _forcedMood = forced;

        _bubbleTimer.Tick += (_, _) => HideBubble();
        PokeCommand = new RelayCommand(Poke);
        OpenAppCommand = new RelayCommand(() => _window.Show());
        HideCommand = new RelayCommand(() => HideRequested?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Raised when the user picks "Hide Mazu" from the mascot's menu.</summary>
    public event EventHandler? HideRequested;

    public ICommand PokeCommand { get; }

    public ICommand OpenAppCommand { get; }

    public ICommand HideCommand { get; }

    public MascotMood Mood
    {
        get => _mood;
        private set => SetProperty(ref _mood, value);
    }

    public string BubbleText
    {
        get => _bubbleText;
        private set
        {
            if (SetProperty(ref _bubbleText, value))
                OnPropertyChanged(nameof(IsBubbleVisible));
        }
    }

    public bool IsBubbleVisible => !string.IsNullOrEmpty(_bubbleText);

    /// <summary>Starts watching the system. Must be called on the UI thread.</summary>
    public void Start()
    {
        if (_started)
            return;

        _started = true;
        _ticks = 0;
        _cpuStreak = 0;
        Mood = _forcedMood ?? MascotMood.Happy;

        _network.NetworkChanged += OnNetworkChanged;
        _ = RefreshNetworkAsync();
        _ = RefreshDisksAsync();
        _subscription = _monitor.Subscribe(OnMetrics, includeGpu: false);

        ShowBubble(Loc.T(Pick(HelloLines)));
    }

    public void Stop()
    {
        if (!_started)
            return;

        _started = false;
        _network.NetworkChanged -= OnNetworkChanged;
        _subscription?.Dispose();
        _subscription = null;
        HideBubble();
    }

    private void OnMetrics(MetricsSnapshot snapshot)
    {
        if (snapshot.CpuPercent is { } cpu)
        {
            if (cpu >= CpuBusyPercent)
                _cpuStreak++;
            else if (cpu < CpuCalmPercent)
                _cpuStreak = 0;
        }

        _ramPercent = snapshot.Memory.UsedPercent;
        if (_ramPercent >= RamWorryPercent)
            _ramWorried = true;
        else if (_ramPercent < RamCalmPercent)
            _ramWorried = false;

        if (++_ticks % DiskCheckEveryTicks == 0)
            _ = RefreshDisksAsync();

        Apply(_forcedMood ?? Decide(NativeMethods.GetIdleTime()));
    }

    /// <summary>Most urgent problem wins: network, then memory/disk, then a busy CPU, then sleep.</summary>
    private MascotMood Decide(TimeSpan idle)
    {
        if (!_networkUp)
            return MascotMood.Surprised;
        if (_ramWorried || _lowDisk is not null)
            return MascotMood.Worried;
        if (_cpuStreak >= CpuBusySamples)
            return MascotMood.Tired;
        if (idle >= SleepyAfter)
            return MascotMood.Sleepy;
        return MascotMood.Happy;
    }

    private void Apply(MascotMood next)
    {
        if (next == Mood)
            return;

        var previous = Mood;
        Mood = next;
        ShowBubble(next switch
        {
            MascotMood.Surprised => Loc.T(Pick(OfflineLines)),
            MascotMood.Worried => WorriedText(),
            MascotMood.Tired => Loc.T(Pick(TiredLines)),
            MascotMood.Sleepy => Loc.T(Pick(SleepyLines)),
            _ => previous switch
            {
                MascotMood.Sleepy => Loc.T(Pick(WakeLines)),
                MascotMood.Surprised => Loc.T(Pick(OnlineLines)),
                _ => Loc.T(Pick(ReliefLines))
            }
        });
    }

    private string WorriedText() =>
        _ramWorried || _lowDisk is null
            ? Loc.T(Pick(RamLines), (int)Math.Round(_ramPercent))
            : Loc.T(Pick(DiskLines), _lowDisk!.Value.Drive, (int)Math.Round(_lowDisk.Value.FreePercent));

    private void Poke()
    {
        // Poking five times within a few seconds tickles.
        var now = DateTime.UtcNow;
        _recentPokes.Enqueue(now);
        while (_recentPokes.Count > 0 && now - _recentPokes.Peek() > TicklishWindow)
            _recentPokes.Dequeue();

        if (_recentPokes.Count >= TicklishPokes)
        {
            _recentPokes.Clear();
            ShowBubble(Loc.T("Mascot_Ticklish"));
            return;
        }

        ShowBubble(PokeText());
    }

    private string PokeText() => Mood switch
    {
        MascotMood.Surprised => Loc.T(Pick(OfflineLines)),
        MascotMood.Worried => WorriedText(),
        MascotMood.Tired => Loc.T(Pick(TiredLines)),
        MascotMood.Sleepy => Loc.T(Pick(SleepyLines)),
        _ => Loc.T(Pick(HappyLines))
    };

    private string Pick(string[] keys)
    {
        string key;
        do
        {
            key = keys[_random.Next(keys.Length)];
        }
        while (keys.Length > 1 && key == _lastPicked);

        _lastPicked = key;
        return key;
    }

    private void ShowBubble(string text)
    {
        BubbleText = text;
        _bubbleTimer.Stop();
        _bubbleTimer.Start();
    }

    private void HideBubble()
    {
        _bubbleTimer.Stop();
        BubbleText = string.Empty;
    }

    private void OnNetworkChanged(object? sender, EventArgs e) => PostToUi(() => _ = RefreshNetworkAsync());

    private static void PostToUi(Action action) =>
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(action);

    private async Task RefreshNetworkAsync()
    {
        try
        {
            _networkUp = (await Task.Run(_network.GetOverview)).IsConnected;
        }
        catch (System.Net.NetworkInformation.NetworkInformationException)
        {
            _networkUp = false;
        }
    }

    private async Task RefreshDisksAsync()
    {
        try
        {
            var disks = await Task.Run(_storage.GetDisks);
            var low = disks
                .Where(d => d.DriveType == "Fixed" && d.TotalBytes > 0)
                .Select(d => (Drive: d.RootPath.TrimEnd('\\'), Free: d.FreeBytes * 100.0 / d.TotalBytes))
                .Where(d => d.Free < DiskLowFreePercent)
                .OrderBy(d => d.Free)
                .ToList();
            _lowDisk = low.Count > 0 ? (low[0].Drive, low[0].Free) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _lowDisk = null;
        }
    }
}
