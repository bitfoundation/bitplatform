namespace Bit.Butil;

/// <summary>
/// The state of one
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BackgroundFetchRegistration">background fetch</see>:
/// how much has moved, and how it ended if it has.
/// </summary>
public class BackgroundFetchRegistrationInfo
{
    /// <summary>The id the fetch was started with. Unique per service-worker registration while the fetch is live.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Total bytes to upload, as the browser computed it from the requests.</summary>
    public long UploadTotal { get; set; }

    /// <summary>Bytes uploaded so far.</summary>
    public long Uploaded { get; set; }

    /// <summary>
    /// Bytes expected to download - the figure passed to <see cref="BackgroundFetch.Fetch"/>, not a
    /// measurement. It is what the browser's progress UI counts against, and 0 leaves that UI
    /// without a total to show.
    /// </summary>
    public long DownloadTotal { get; set; }

    /// <summary>Bytes downloaded so far.</summary>
    public long Downloaded { get; set; }

    /// <summary>
    /// Empty while the fetch is still running, then <c>"success"</c> or <c>"failure"</c>.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Why the fetch failed, when it did: <c>"aborted"</c>, <c>"bad-status"</c>,
    /// <c>"fetch-error"</c>, <c>"quota-exceeded"</c>, <c>"download-total-exceeded"</c>. Empty while
    /// it is running or when it succeeded.
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// True while the individual request/response records can still be read. It goes false once the
    /// fetch is over and the browser has released them, which is why
    /// <see cref="BackgroundFetch.ReadRecordText"/> is a thing to do from the service worker's
    /// <c>backgroundfetchsuccess</c> handler rather than from the page afterwards.
    /// </summary>
    public bool RecordsAvailable { get; set; }
}
