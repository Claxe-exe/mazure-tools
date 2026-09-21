namespace MazureTools.Modules.Storage;

public interface IStorageService
{
    IReadOnlyList<DiskInfo> GetDisks();

    Task<FolderSizeResult> CalculateFolderSizeAsync(string path, IProgress<FolderSizeProgress>? progress, CancellationToken cancellationToken);
}

public sealed class StorageService : IStorageService
{
    public IReadOnlyList<DiskInfo> GetDisks()
    {
        var disks = new List<DiskInfo>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                // Empty card readers / disconnected network drives are not ready; there is nothing to show for them.
                if (!drive.IsReady)
                    continue;

                disks.Add(new DiskInfo(
                    drive.Name,
                    drive.VolumeLabel,
                    drive.DriveType.ToString(),
                    drive.DriveFormat,
                    drive.TotalSize,
                    drive.AvailableFreeSpace));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Drive went away between enumeration and query; skip it.
            }
        }

        return disks;
    }

    public Task<FolderSizeResult> CalculateFolderSizeAsync(string path, IProgress<FolderSizeProgress>? progress, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);

        return Task.Run(() =>
        {
            // Symbolic links / junctions are skipped so linked folders are not counted twice and cycles cannot loop.
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
                ReturnSpecialDirectories = false
            };

            long files = 0, bytes = 0;
            var lastReport = Environment.TickCount64;
            foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();
                files++;
                bytes += file.Length;

                if (progress is not null && Environment.TickCount64 - lastReport >= 150)
                {
                    progress.Report(new FolderSizeProgress(files, bytes));
                    lastReport = Environment.TickCount64;
                }
            }

            return new FolderSizeResult(path, files, bytes);
        }, cancellationToken);
    }
}
