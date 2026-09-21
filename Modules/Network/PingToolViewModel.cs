using System.Collections.ObjectModel;
using System.Windows.Input;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.Modules.Network;

public sealed class PingToolViewModel : ViewModelBase
{
    private const int TimeoutMs = 2000;
    private const int MaxCount = 100;

    private readonly IPingService _ping;
    private CancellationTokenSource? _cts;
    private string _host = "1.1.1.1";
    private string _countText = "4";
    private bool _isRunning;
    private string _sent = "—", _received = "—", _lost = "—", _min = "—", _avg = "—", _max = "—";

    public PingToolViewModel(IDialogService dialogs, IPingService ping) : base(dialogs)
    {
        _ping = ping;
        StartCommand = CreateAsyncCommand(StartAsync, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
    }

    public ICommand StartCommand { get; }

    public ICommand StopCommand { get; }

    public ObservableCollection<string> Lines { get; } = [];

    public string Host { get => _host; set => SetProperty(ref _host, value); }

    public string CountText { get => _countText; set => SetProperty(ref _countText, value); }

    public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

    public string Sent { get => _sent; private set => SetProperty(ref _sent, value); }

    public string Received { get => _received; private set => SetProperty(ref _received, value); }

    public string Lost { get => _lost; private set => SetProperty(ref _lost, value); }

    public string Min { get => _min; private set => SetProperty(ref _min, value); }

    public string Avg { get => _avg; private set => SetProperty(ref _avg, value); }

    public string Max { get => _max; private set => SetProperty(ref _max, value); }

    public void Stop() => _cts?.Cancel();

    private async Task StartAsync()
    {
        if (!int.TryParse(CountText, out var count) || count is < 1 or > MaxCount)
        {
            Dialogs.ShowWarning(Loc.T("Ping_Title"), Loc.T("Ping_CountInvalid", MaxCount));
            return;
        }

        Lines.Clear();
        ShowStatistics(new PingStatistics(0, 0, null, null, null));
        IsRunning = true;
        _cts = new CancellationTokenSource();
        try
        {
            var progress = new Progress<PingUpdate>(update =>
            {
                Lines.Add(update.Line);
                ShowStatistics(update.Statistics);
            });

            var final = await _ping.RunAsync(Host, count, TimeoutMs, progress, _cts.Token);
            ShowStatistics(final);
        }
        catch (ArgumentException ex)
        {
            Dialogs.ShowWarning(Loc.T("Ping_Title"), ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsRunning = false;
        }
    }

    private void ShowStatistics(PingStatistics stats)
    {
        Sent = stats.Sent.ToString();
        Received = stats.Received.ToString();
        Lost = stats.Sent == 0 ? "—" : $"{stats.LossPercent:0.#}% ({stats.Lost}/{stats.Sent})";
        Min = stats.MinMs is { } min ? $"{min} ms" : "—";
        Avg = stats.AvgMs is { } avg ? $"{avg:0.#} ms" : "—";
        Max = stats.MaxMs is { } max ? $"{max} ms" : "—";
    }
}
