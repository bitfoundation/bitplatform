using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Bit.Bswup.Tests.Harness.Web;

/// <summary>
/// The server side of one test: which service worker the apps get and with what settings, which
/// deployment version is live, whether the network is up, which requests fail, and every request the
/// browser made. A browser keeps service workers and CacheStorage per origin, so every test runs on an
/// origin of its own (<c>&lt;session&gt;.localhost</c>) and the host keys all of this by that name.
/// </summary>
public sealed class HarnessSession(string id)
{
    private readonly object _faultsLock = new();
    private readonly List<HarnessFault> _faults = [];
    private readonly ConcurrentQueue<HarnessRequestRecord> _requests = new();
    private readonly long _startedAt = Environment.TickCount64;

    private volatile HarnessSessionOptions _options = new();
    private volatile int _version = 1;
    private volatile bool _offline;

    public string Id { get; } = id;

    public HarnessSessionOptions Options
    {
        get => _options;
        set => _options = value;
    }

    /// <summary>The deployment version the host serves: 1 is what was built, every later one a new release.</summary>
    public int Version
    {
        get => _version;
        set => _version = value;
    }

    /// <summary>While set, every request outside /_harness/ is dropped, the way a lost connection fails it.</summary>
    public bool Offline
    {
        get => _offline;
        set => _offline = value;
    }

    public void AddFault(HarnessFault fault)
    {
        // Compiled here so a malformed pattern fails the test that set it, not a later request.
        _ = new Regex(fault.Path);

        lock (_faultsLock) _faults.Add(fault);
    }

    public void ClearFaults()
    {
        lock (_faultsLock) _faults.Clear();
    }

    /// <summary>The first fault matching <paramref name="pathAndQuery"/>, consuming one of its <see cref="HarnessFault.Times"/>.</summary>
    public HarnessFault? TakeFault(string pathAndQuery)
    {
        lock (_faultsLock)
        {
            foreach (var fault in _faults)
            {
                if (fault.Times is <= 0) continue;
                if (Regex.IsMatch(pathAndQuery, fault.Path, RegexOptions.IgnoreCase) is false) continue;

                if (fault.Times is not null) fault.Times--;
                return fault;
            }
        }

        return null;
    }

    public HarnessRequestRecord Record(HttpContext context)
    {
        var headers = context.Request.Headers;
        var record = new HarnessRequestRecord
        {
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? "/",
            Query = context.Request.QueryString.Value ?? string.Empty,
            Destination = headers["Sec-Fetch-Dest"].ToString(),
            FetchMode = headers["Sec-Fetch-Mode"].ToString(),
            ServiceWorker = headers["Service-Worker"].ToString(),
            Range = headers.Range.ToString(),
            At = Environment.TickCount64 - _startedAt,
        };

        _requests.Enqueue(record);
        return record;
    }

    public IReadOnlyList<HarnessRequestRecord> Requests => [.. _requests];

    public void ClearRequests() => _requests.Clear();
}

/// <summary>What <c>PUT /_harness/options</c> sets. Every member has the default a plain app would have.</summary>
public sealed class HarnessSessionOptions
{
    /// <summary><see cref="HarnessWorkers"/>: the service-worker script the apps are served.</summary>
    public string Worker { get; set; } = HarnessWorkers.Bswup;

    /// <summary>
    /// <c>self.*</c> settings written ahead of <c>importScripts</c>, over each app's defaults (a null value removes
    /// a default). A RegExp is written as <c>{ "$regex": "source", "flags": "i" }</c>.
    /// </summary>
    public System.Text.Json.Nodes.JsonObject? WorkerSettings { get; set; }

    /// <summary>Attributes of the bit-bswup.js script tag, over each app's defaults (a null value removes a default).</summary>
    public Dictionary<string, string?>? ScriptAttributes { get; set; }

    public HarnessProgressOptions Progress { get; set; } = new();

    /// <summary>References blazor.web.js through <c>@Assets</c>, i.e. by its fingerprinted name (.NET 9 and later).</summary>
    public bool FingerprintedBlazorScript { get; set; }
}

/// <summary>The BswupProgress parameters of the Blazor Web App harness, and where it is rendered.</summary>
public sealed class HarnessProgressOptions
{
    /// <summary>"document" (the host page, the common setup), "interactive" (inside the interactive root) or "none".</summary>
    public string Placement { get; set; } = "document";

    public bool AutoReload { get; set; } = true;

    public bool ShowAssets { get; set; }

    public bool HideApp { get; set; }

    public bool AutoHide { get; set; }

    public bool ShowOnUpdate { get; set; } = true;
}

public static class HarnessWorkers
{
    /// <summary>service-worker.js imports bit-bswup.sw.js.</summary>
    public const string Bswup = "bswup";

    /// <summary>service-worker.js imports bit-bswup.sw-cleanup.js: the documented way to back an app out of Bswup.</summary>
    public const string Cleanup = "cleanup";

    /// <summary>service-worker.js answers 404.</summary>
    public const string Missing = "missing";
}

/// <summary>How the host mistreats requests whose path and query match <see cref="Path"/>.</summary>
public sealed class HarnessFault
{
    /// <summary>A case-insensitive regular expression tested against the path and query.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>How many requests it applies to; null for all of them.</summary>
    public int? Times { get; set; }

    /// <summary>Milliseconds to hold the request before anything else happens.</summary>
    public int DelayMs { get; set; }

    /// <summary>Drop the connection instead of answering.</summary>
    public bool Abort { get; set; }

    /// <summary>Answer with this status and no body.</summary>
    public int? Status { get; set; }

    /// <summary>Serve the file with its bytes altered, as a rewriting proxy or a tampered CDN would.</summary>
    public bool Corrupt { get; set; }
}

public sealed class HarnessRequestRecord
{
    public string Method { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public string Query { get; init; } = string.Empty;

    /// <summary>Sec-Fetch-Dest: "document" for a navigation, "serviceworker" for a worker script, "empty" for fetch().</summary>
    public string Destination { get; init; } = string.Empty;

    public string FetchMode { get; init; } = string.Empty;

    public string ServiceWorker { get; init; } = string.Empty;

    public string Range { get; init; } = string.Empty;

    /// <summary>Milliseconds since the session was created.</summary>
    public long At { get; init; }

    /// <summary>The status the host answered with; 0 when the connection was dropped.</summary>
    public int Status { get; set; }
}
