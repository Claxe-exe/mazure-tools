using MazureTools.Core;

namespace MazureTools.Modules.Storage;

public sealed record DiskInfo(
    string RootPath,
    string Label,
    string DriveType,
    string FileSystem,
    long TotalBytes,
    long FreeBytes)
{
    public long UsedBytes => TotalBytes - FreeBytes;

    public double UsedPercent => TotalBytes == 0 ? 0 : UsedBytes * 100.0 / TotalBytes;

    // Presentation helpers used by the shared disk template.
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? RootPath : $"{RootPath}  {Label}";

    // The words between the numbers ("used of", "free") come from the language dictionary in the template.
    public string UsedText => ByteFormatter.Format(UsedBytes);

    public string TotalText => ByteFormatter.Format(TotalBytes);

    public string FreeAmountText => ByteFormatter.Format(FreeBytes);

    public string Details => $"{DriveType} · {FileSystem}";
}

public sealed record FolderSizeProgress(long Files, long Bytes);

public sealed record FolderSizeResult(string Path, long Files, long Bytes);
