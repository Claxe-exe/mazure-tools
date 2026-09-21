using System.Collections.ObjectModel;
using System.Windows.Input;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.Modules.Storage;

public sealed class StorageViewModel : PageViewModel
{
    private readonly IStorageService _storage;
    private CancellationTokenSource? _cts;
    private string _folderPath = string.Empty;
    private bool _isCalculating;
    private LocText _result = LocText.Of("Stor_Prompt");

    public StorageViewModel(IDialogService dialogs, IStorageService storage)
        : base(dialogs, PageId.Storage, "")
    {
        _storage = storage;
        RefreshCommand = CreateAsyncCommand(RefreshDisksAsync);
        BrowseCommand = new RelayCommand(Browse, () => !IsCalculating);
        CalculateCommand = CreateAsyncCommand(CalculateAsync, () => !IsCalculating);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsCalculating);
    }

    public ObservableCollection<DiskInfo> Disks { get; } = [];

    public ICommand RefreshCommand { get; }

    public ICommand BrowseCommand { get; }

    public ICommand CalculateCommand { get; }

    public ICommand CancelCommand { get; }

    public string FolderPath { get => _folderPath; set => SetProperty(ref _folderPath, value); }

    public bool IsCalculating { get => _isCalculating; private set => SetProperty(ref _isCalculating, value); }

    public string ResultText => _result.ToString();

    private void SetResult(string key, params object?[] args)
    {
        _result = LocText.Of(key, args);
        OnPropertyChanged(nameof(ResultText));
    }

    protected override void OnActivated() => RefreshCommand.Execute(null);

    protected override void OnDeactivated() => _cts?.Cancel();

    private async Task RefreshDisksAsync() => Disks.ReplaceWith(await Task.Run(_storage.GetDisks));

    private void Browse()
    {
        if (Dialogs.PickFolder(Loc.T("Stor_PickTitle")) is { } folder)
            FolderPath = folder;
    }

    private async Task CalculateAsync()
    {
        var path = FolderPath.Trim().Trim('"');
        if (path.Length == 0)
        {
            Dialogs.ShowWarning(Loc.T("Stor_FolderSize"), Loc.T("Stor_ChooseFirst"));
            return;
        }

        IsCalculating = true;
        _cts = new CancellationTokenSource();
        try
        {
            var progress = new Progress<FolderSizeProgress>(p =>
                SetResult("Stor_CountingProgress", p.Files, ByteFormatter.Format(p.Bytes)));
            SetResult("Stor_Counting");

            var result = await _storage.CalculateFolderSizeAsync(path, progress, _cts.Token);
            SetResult("Stor_Result", ByteFormatter.Format(result.Bytes), result.Bytes, result.Files);
        }
        catch (OperationCanceledException)
        {
            SetResult("Stor_Cancelled");
        }
        catch (DirectoryNotFoundException)
        {
            SetResult("Stor_NotFound");
            Dialogs.ShowWarning(Loc.T("Stor_FolderSize"), Loc.T("Stor_NotFoundDialog", path));
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsCalculating = false;
        }
    }
}
