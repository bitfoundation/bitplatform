namespace Bit.Bcrdt;

/// <summary>The few things the browser lifecycle needs from the browser.</summary>
/// <remarks>
/// An interface so the decisions in <see cref="BcrdtBrowserLifecycle"/> can be exercised without a browser; the
/// one implementation that ships, <see cref="BcrdtButilBrowser"/>, is a thin translation of Bit.Butil's services.
/// </remarks>
internal interface IBcrdtBrowser
{
    /// <summary>Whether this process runs in a browser, so the events below exist.</summary>
    bool IsAvailable { get; }

    /// <summary>Starts reporting the signals; disposing the result stops it.</summary>
    ValueTask<IAsyncDisposable> SubscribeAsync(Func<BcrdtBrowserSignal, Task> handler, CancellationToken cancellationToken);

    /// <summary><c>navigator.onLine</c>.</summary>
    ValueTask<bool> IsOnlineAsync(CancellationToken cancellationToken);

    /// <summary>Asks for persistent storage unless it is already granted; whether it is.</summary>
    ValueTask<bool> RequestPersistentStorageAsync(CancellationToken cancellationToken);
}
