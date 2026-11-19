namespace Bit.Blex;

/// <summary>A cached query as <see cref="BlexQueryClient.GetQueries"/> reports it, for inspection and diagnostics.</summary>
/// <param name="Key">The query's key.</param>
/// <param name="DataType">The type of its data.</param>
/// <param name="Status">Whether it has data, an error, or neither.</param>
/// <param name="FetchStatus">Whether a fetch is running.</param>
/// <param name="ObserverCount">How many observers watch it; with none, it is removed once its GC time passes.</param>
/// <param name="DataUpdatedAt">When its data was fetched or set.</param>
/// <param name="IsInvalidated">Whether it was invalidated (or failed) since.</param>
/// <param name="Error">The last fetch's error, if it failed.</param>
public sealed record BlexQueryInfo(
    BlexQueryKey Key,
    Type DataType,
    BlexQueryStatus Status,
    BlexFetchStatus FetchStatus,
    int ObserverCount,
    DateTimeOffset? DataUpdatedAt,
    bool IsInvalidated,
    Exception? Error);
