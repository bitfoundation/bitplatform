namespace Bit.BevTools;

/// <summary>Requests to the same endpoint, whatever their query values: a request per keystroke is one endpoint.</summary>
public static class BevToolsHttpEndpoints
{
    /// <summary>"GET https://host/api/products?q": the path as it is, the query by parameter names.</summary>
    public static string Of(string method, string url)
    {
        var hash = url.IndexOf('#');
        if (hash >= 0)
        {
            url = url[..hash];
        }

        var query = url.IndexOf('?');
        if (query < 0)
        {
            return method + " " + url;
        }

        var keys = url[(query + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.IndexOf('=') is var eq and >= 0 ? p[..eq] : p)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        return method + " " + url[..query] + "?" + string.Join("&", keys);
    }

    /// <summary>
    /// <see cref="Of"/> with the path segments that are identifiers replaced by <c>{id}</c>: numbers, GUIDs and long
    /// hexadecimal strings (hashes, object ids). <c>/api/orders/1001</c> and <c>/api/orders/1002</c> are one endpoint.
    /// Words are kept, so <c>/api/orders/recent</c> is another.
    /// </summary>
    public static string Template(string method, string url)
    {
        var endpoint = Of(method, url);
        var start = method.Length + 1;
        var scheme = endpoint.IndexOf("://", start, StringComparison.Ordinal);
        var pathStart = scheme < 0 ? start : endpoint.IndexOf('/', scheme + 3);
        if (pathStart < 0)
        {
            return endpoint;
        }

        var query = endpoint.IndexOf('?', pathStart);
        var pathEnd = query < 0 ? endpoint.Length : query;
        var segments = endpoint[pathStart..pathEnd].Split('/');
        var changed = false;
        for (var i = 0; i < segments.Length; i++)
        {
            if (IsIdentifier(segments[i]))
            {
                segments[i] = "{id}";
                changed = true;
            }
        }

        return changed ? endpoint[..pathStart] + string.Join('/', segments) + endpoint[pathEnd..] : endpoint;
    }

    private static bool IsIdentifier(string segment)
    {
        if (segment.Length == 0)
        {
            return false;
        }

        if (segment.All(char.IsAsciiDigit) || Guid.TryParse(segment, out _))
        {
            return true;
        }

        return segment.Length >= 16 && segment.All(char.IsAsciiHexDigit);
    }

    /// <summary>Requests grouped by <see cref="BevToolsHttpRecord.EndpointTemplate"/>, with their counts and durations.</summary>
    internal static List<BevToolsHttpEndpointSummary> Summarize(IEnumerable<BevToolsHttpRecord> requests) =>
        requests.GroupBy(r => r.EndpointTemplate, StringComparer.Ordinal).Select(g =>
        {
            var durations = g.Where(r => !r.IsPending && !r.IsCancelled && r.DurationMs is not null).Select(r => r.DurationMs!.Value).Order().ToArray();
            var last = g.MaxBy(r => r.StartedAt)!;
            return new BevToolsHttpEndpointSummary(
                g.Key,
                g.Count(),
                g.Count(r => r.IsFailed),
                g.Count(r => r.IsCancelled),
                g.Count(r => r.IsPending),
                durations.Length == 0 ? null : BevToolsStats.Percentile(durations, 50),
                durations.Length == 0 ? null : BevToolsStats.Percentile(durations, 95),
                durations.Length == 0 ? null : durations[^1],
                // A total only when every response reported its size (a chunked body has no Content-Length): a sum
                // of some of them would read as the whole. Requests that got no response received nothing.
                g.Any(r => r.StatusCode is not null && !r.IsCancelled) && g.Where(r => r.StatusCode is not null && !r.IsCancelled).All(r => r.ResponseBytes is not null)
                    ? g.Sum(r => r.ResponseBytes ?? 0)
                    : null,
                last.StartedAt,
                last.IsCancelled ? null : last.StatusCode,
                g.Select(r => r.Url).Distinct(StringComparer.Ordinal).Count())
            {
                Callers = g.Where(r => r.CallerComponent is not null).GroupBy(r => r.CallerComponent!, StringComparer.Ordinal)
                    .OrderByDescending(c => c.Count()).Take(5).Select(c => (c.Key, c.Count())).ToList(),
                CallerCount = g.Where(r => r.CallerComponent is not null).Select(r => r.CallerComponent).Distinct(StringComparer.Ordinal).Count(),
            };
        }).ToList();
}
