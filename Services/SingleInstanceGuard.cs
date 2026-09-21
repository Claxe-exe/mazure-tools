namespace MazureTools.Services;

/// <summary>
/// Keeps a single Mazure Tools process per user session. A second launch asks the first one to show its window.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = @"Local\MazureTools.Instance";
    private const string ShowEventName = @"Local\MazureTools.Show";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _registration;
    private bool _isOwner;

    /// <summary>True when this process is the (only) running instance.</summary>
    public bool TryAcquire(TimeSpan wait)
    {
        try
        {
            _mutex = new Mutex(false, MutexName);
            try
            {
                _isOwner = _mutex.WaitOne(wait);
            }
            catch (AbandonedMutexException)
            {
                _isOwner = true; // previous owner was killed; we own it now
            }
        }
        catch (UnauthorizedAccessException)
        {
            _isOwner = false; // owned by an instance running at a higher (administrator) level
        }

        return _isOwner;
    }

    /// <summary>Asks the running instance to show itself. Returns false if it could not be reached.</summary>
    public bool SignalExistingInstance()
    {
        try
        {
            using var handle = EventWaitHandle.OpenExisting(ShowEventName);
            return handle.Set();
        }
        catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void ListenForSignals(Action onSignal)
    {
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _registration = ThreadPool.RegisterWaitForSingleObject(_showEvent, (_, _) => onSignal(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    public void Dispose()
    {
        _registration?.Unregister(null);
        _showEvent?.Dispose();

        if (_isOwner)
        {
            try { _mutex?.ReleaseMutex(); }
            catch (ApplicationException) { /* released from a different thread; process exit frees it */ }
        }

        _mutex?.Dispose();
    }
}
