namespace Bit.Blex;

/// <summary>Whether a query has data, has failed, or has neither yet.</summary>
public enum BlexQueryStatus
{
    /// <summary>No data and no error yet: the first fetch has not finished (or has not started).</summary>
    Pending,

    /// <summary>The last fetch failed. Data from an earlier success, if any, is kept.</summary>
    Error,

    /// <summary>Data is available.</summary>
    Success,
}
