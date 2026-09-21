namespace MazureTools.Core;

/// <summary>Outcome of an operation that changes system state. Carries a user-presentable message.</summary>
public sealed record OperationResult(bool Success, string Message, bool RequiresElevation = false)
{
    public static OperationResult Ok(string message) => new(true, message);

    public static OperationResult Fail(string message) => new(false, message);

    public static OperationResult NeedsElevation(string message) => new(false, message, RequiresElevation: true);
}
