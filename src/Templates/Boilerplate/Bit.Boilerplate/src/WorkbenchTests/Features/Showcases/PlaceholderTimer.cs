namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>Times from the shell how long a view shows a placeholder before its data, which a test looking in from outside can't see exactly.</summary>
public static class PlaceholderTimer
{
    /// <summary>The least a slow load takes in milliseconds, a little under the shortest wait the slow state picks, while a live one takes a tenth of that.</summary>
    public const double SlowLoad = 1100;

    /// <summary>Starts timing on every page opened from now on, so it comes before the page opens.</summary>
    public static Task Watch(IPage page, string placeholder, string data) => page.AddInitScriptAsync($$"""
        if (window.top === window) {
            let shownAt = null;
            const watch = setInterval(() => {
                const view = document.querySelector('iframe[data-pane-id]')?.contentDocument;
                if (!view) return;
                if (shownAt === null && view.querySelector({{JsonSerializer.Serialize(placeholder)}})) shownAt = performance.now();
                if (view.querySelector({{JsonSerializer.Serialize(data)}})) {
                    window.__placeholderTime = shownAt === null ? -1 : performance.now() - shownAt;
                    clearInterval(watch);
                }
            }, 10);
        }
        """);

    /// <summary>Waits until the data has come and returns how long the placeholder stayed before it, or -1 if there was none.</summary>
    public static Task<double> Read(IPage page) => WorkbenchTestReviewer.WaitFor(() => page.EvaluateAsync<double>("() => window.__placeholderTime ?? -2"), time => time > -2);
}
