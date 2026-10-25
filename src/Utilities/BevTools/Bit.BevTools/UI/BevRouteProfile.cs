namespace Bit.BevTools;

/// <summary>What rendered while one page was shown, over every visit to it.</summary>
/// <param name="Route">The page, as the navigation named it: "/products", or a route template on .NET 10 ("/products/{id}").</param>
/// <param name="Visits">Navigations to it the timeline still holds.</param>
/// <param name="Renders">Component renders while it was shown.</param>
/// <param name="RenderMs">Their BuildRenderTree time.</param>
/// <param name="Top">The components that cost the most there, with their renders and time.</param>
internal sealed record BevRouteCost(string Route, int Visits, int Renders, double RenderMs, IReadOnlyList<(string Component, int Renders, double Ms)> Top);

/// <summary>
/// The renders BevTools still holds (each component's last <see cref="BitBevToolsOptions.MaxRendersPerComponent"/>), each
/// put on the page that was shown when it happened: the latest navigation at or before it in the timeline. A navigation
/// is dated from its first render (the router renders the new page before anything else hears of the navigation; see
/// <c>BevToolsPanel.OnLocationChanged</c>). Renders older than the oldest navigation the timeline still holds - its
/// buffer drops the oldest events first, and renders are events too - are on a row of their own.
/// </summary>
internal static class BevRouteProfile
{
    internal const string BeforeFirst = "(before the oldest navigation BevTools holds)";

    public static List<BevRouteCost> Build(BevToolsSession session)
    {
        var navigations = session.Timeline.Snapshot()
            .Where(e => e.Kind == BevToolsEventKind.Navigation)
            .OrderBy(e => e.Timestamp)
            .ToList();
        var starts = navigations.Select(n => n.Timestamp).ToArray();
        var routes = navigations.Select(n => RouteOf(n.Title)).ToArray();

        var totals = new Dictionary<string, (int Renders, double Ms, Dictionary<string, (int Renders, double Ms)> Components)>(StringComparer.Ordinal);
        foreach (var record in session.Components.All)
        {
            if (record.IsBevTools || record.IsHidden)
            {
                continue;
            }

            foreach (var sample in record.Renders)
            {
                // The latest navigation at or before the render: of several at the same instant (a redirect, on a clock
                // of a millisecond), the last.
                var index = UpperBound(starts, sample.At) - 1;
                var route = index >= 0 ? routes[index] : BeforeFirst;
                var (renders, ms, components) = totals.TryGetValue(route, out var known) ? known : (0, 0d, new Dictionary<string, (int, double)>(StringComparer.Ordinal));
                var (componentRenders, componentMs) = components.GetValueOrDefault(record.DisplayName);
                components[record.DisplayName] = (componentRenders + 1, componentMs + sample.DurationMs);
                totals[route] = (renders + 1, ms + sample.DurationMs, components);
            }
        }

        var visits = routes.GroupBy(r => r, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        return totals
            .Select(t => new BevRouteCost(
                t.Key,
                visits.GetValueOrDefault(t.Key),
                t.Value.Renders,
                t.Value.Ms,
                t.Value.Components.OrderByDescending(c => c.Value.Ms).Take(3).Select(c => (c.Key, c.Value.Renders, c.Value.Ms)).ToList()))
            .OrderByDescending(r => r.RenderMs)
            .ToList();
    }

    // "Navigate to /products" and "Enhanced navigation to https://shop/products" are one page.
    private static string RouteOf(string title) =>
        title.StartsWith("Navigate to ", StringComparison.Ordinal) ? title["Navigate to ".Length..]
        : title.StartsWith("Enhanced navigation to ", StringComparison.Ordinal) ? BevToolsHttpMessageHandler.StripOrigin(title["Enhanced navigation to ".Length..])
        : title;

    /// <summary>The index of the first element later than <paramref name="value"/>.</summary>
    private static int UpperBound(DateTimeOffset[] sorted, DateTimeOffset value)
    {
        int low = 0, high = sorted.Length;
        while (low < high)
        {
            var mid = (low + high) >>> 1;
            if (sorted[mid] <= value)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }
}
