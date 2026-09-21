using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.Modules.Processes;

public sealed class ProcessesViewModel : PageViewModel
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly IProcessService _processes;
    private readonly IProcessGuard _guard;
    private readonly IElevationService _elevation;
    private readonly Dictionary<int, ProcessEntry> _byPid = [];
    private CancellationTokenSource? _cts;
    private ProcessEntry? _selected;
    private string _searchText = string.Empty;
    private LocText _status = LocText.Of("Proc_Loading");
    private string? _statusOverride;

    public ProcessesViewModel(IDialogService dialogs, IProcessService processes, IProcessGuard guard, IElevationService elevation)
        : base(dialogs, PageId.Processes, "")
    {
        _processes = processes;
        _guard = guard;
        _elevation = elevation;

        View = CollectionViewSource.GetDefaultView(Items);
        View.SortDescriptions.Add(new SortDescription(nameof(ProcessEntry.Name), ListSortDirection.Ascending));
        View.Filter = FilterEntry;
        if (View is ICollectionViewLiveShaping live && live.CanChangeLiveSorting)
        {
            live.LiveSortingProperties.Add(nameof(ProcessEntry.CpuPercent));
            live.LiveSortingProperties.Add(nameof(ProcessEntry.MemoryBytes));
            live.IsLiveSorting = true;
        }

        Loc.LanguageChanged += (_, _) =>
        {
            foreach (var entry in Items)
                entry.RefreshLocalization();
        };

        EndProcessCommand = CreateAsyncCommand(EndSelectedAsync, () => SelectedProcess is not null);
        RestartProcessCommand = CreateAsyncCommand(RestartSelectedAsync, () => SelectedProcess is not null);
    }

    public ObservableCollection<ProcessEntry> Items { get; } = [];

    public ICollectionView View { get; }

    public ICommand EndProcessCommand { get; }

    public ICommand RestartProcessCommand { get; }

    public ProcessEntry? SelectedProcess
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                View.Refresh();
        }
    }

    /// <summary>Either a localised state ("N processes") or the message of the last action.</summary>
    public string Status => _statusOverride ?? _status.ToString();

    private void SetStatus(LocText state, string? text = null)
    {
        _status = state;
        _statusOverride = text;
        OnPropertyChanged(nameof(Status));
    }

    protected override void OnActivated()
    {
        _cts = new CancellationTokenSource();
        _ = RunRefreshLoopAsync(_cts.Token);
    }

    protected override void OnDeactivated()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private bool FilterEntry(object item) =>
        string.IsNullOrWhiteSpace(SearchText)
        || item is ProcessEntry entry
        && (entry.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)
            || entry.Pid.ToString() == SearchText.Trim());

    private async Task RunRefreshLoopAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(RefreshInterval);
            do
            {
                await RefreshAsync(token);
            }
            while (await timer.WaitForNextTickAsync(token));
        }
        catch (OperationCanceledException)
        {
            // Page left.
        }
        catch (Exception ex)
        {
            SetStatus(LocText.Of("Proc_ReadFail"));
            ReportError(ex);
        }
    }

    private async Task RefreshAsync(CancellationToken token)
    {
        var samples = await Task.Run(_processes.Sample, token);
        token.ThrowIfCancellationRequested();

        var alive = new HashSet<int>(samples.Count);
        foreach (var sample in samples)
        {
            alive.Add(sample.Pid);
            if (_byPid.TryGetValue(sample.Pid, out var entry) && entry.Name == sample.Name)
            {
                entry.Update(sample);
                continue;
            }

            if (entry is not null)
                Items.Remove(entry); // PID was reused by a different program

            entry = new ProcessEntry(sample.Pid, sample.Name, _guard.Assess(sample.Pid, sample.Name).Risk);
            entry.Update(sample);
            _byPid[sample.Pid] = entry;
            Items.Add(entry);
        }

        foreach (var pid in _byPid.Keys.Where(p => !alive.Contains(p)).ToList())
        {
            Items.Remove(_byPid[pid]);
            _byPid.Remove(pid);
        }

        SetStatus(LocText.Of("Proc_Count", Items.Count)); // also clears the message of the last action
    }

    private async Task EndSelectedAsync()
    {
        if (SelectedProcess is not { } target)
            return;

        var assessment = _guard.Assess(target.Pid, target.Name);
        if (assessment.Risk == ProcessRisk.Critical)
        {
            Dialogs.ShowWarning(Loc.T("Proc_ProtectedTitle"), Loc.T("Proc_ProtectedBody", target.Name, target.Pid, assessment.Reason));
            return;
        }

        var message = Loc.T("Proc_EndBody", target.Name, target.Pid);
        if (assessment.Risk == ProcessRisk.System)
            message += Loc.T("Proc_EndSystemWarn", assessment.Reason);

        if (!Dialogs.Confirm(Loc.T("Proc_End"), message, Loc.T("Proc_End"), isDangerous: true))
            return;

        var result = await Task.Run(() => _processes.Terminate(target.Pid, target.Name));
        Report(Loc.T("Proc_End"), result);
    }

    private async Task RestartSelectedAsync()
    {
        if (SelectedProcess is not { } target)
            return;

        var assessment = _guard.Assess(target.Pid, target.Name);
        if (assessment.Risk != ProcessRisk.Normal)
        {
            Dialogs.ShowWarning(Loc.T("Proc_RestartCannotTitle"), Loc.T("Proc_RestartCannotBody", target.Name, assessment.Reason));
            return;
        }

        var message = Loc.T("Proc_RestartBody", target.Name, target.Pid);
        if (!Dialogs.Confirm(Loc.T("Proc_RestartTitle"), message, Loc.T("Proc_RestartTitle"), isDangerous: true))
            return;

        var result = await Task.Run(() => _processes.Restart(target.Pid, target.Name));
        Report(Loc.T("Proc_RestartTitle"), result);
    }

    private void Report(string title, OperationResult result)
    {
        if (result.RequiresElevation)
        {
            _elevation.OfferRestartAsAdministrator(result.Message);
            return;
        }

        if (result.Success)
            SetStatus(default, result.Message);
        else
            Dialogs.ShowError(title, result.Message);
    }
}
