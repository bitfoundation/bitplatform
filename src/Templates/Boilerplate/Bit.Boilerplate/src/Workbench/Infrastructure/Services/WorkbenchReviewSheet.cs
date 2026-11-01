using System.Text;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Localization;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Builds the review sheet: one self-contained HTML file with the views' screenshots, the page's status and its comments.
/// </summary>
public static class WorkbenchReviewSheet
{
    private const string Styles = """
        :root { color-scheme: light; font-family: system-ui, -apple-system, "Segoe UI", sans-serif; color: #1f2328; background: #fff; }
        body { margin: 0; padding: 32px; }
        header { margin-bottom: 24px; }
        h1 { margin: 0 0 4px; font-size: 26px; }
        h2 { margin: 32px 0 12px; font-size: 18px; border-bottom: 1px solid #d0d7de; padding-bottom: 6px; }
        .meta { color: #59636e; font-size: 13px; }
        .status { display: inline-block; margin-inline-start: 8px; padding: 2px 10px; border-radius: 999px; border: 1px solid #d0d7de; font-size: 12px; font-weight: 600; }
        .views { display: grid; gap: 20px; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); align-items: start; }
        figure { margin: 0; break-inside: avoid; }
        figure img { display: block; max-width: 100%; max-height: 560px; margin: 0 auto; border-radius: 8px; box-shadow: 0 0 0 1px #d0d7de; }
        figcaption { margin-top: 8px; font-size: 12px; color: #59636e; text-align: center; }
        .comment { break-inside: avoid; border: 1px solid #d0d7de; border-radius: 10px; padding: 12px 14px; margin-bottom: 10px; }
        .comment-head { display: flex; gap: 10px; align-items: baseline; }
        .number { flex-shrink: 0; display: inline-flex; width: 22px; height: 22px; border-radius: 50%; align-items: center; justify-content: center; font-size: 12px; font-weight: 700; color: #fff; background: #0969da; }
        .number.done { background: #1a7f37; }
        .number.working { background: #9a6700; }
        .element { font-weight: 600; }
        .view { color: #59636e; font-size: 12px; }
        .message { margin: 8px 0 0; padding: 8px 10px; border-radius: 8px; background: #f6f8fa; white-space: pre-wrap; }
        .message.agent { background: #ddf4ff; }
        .author { display: block; font-size: 12px; font-weight: 600; margin-bottom: 2px; }
        ul.tasks { margin: 8px 0 0; padding-inline-start: 18px; font-size: 13px; }
        li.done { color: #59636e; text-decoration: line-through; }
        footer { margin-top: 32px; color: #59636e; font-size: 12px; }
        """;

    public static string Build(WorkbenchPageInfo page,
                               IReadOnlyList<(WorkbenchScenario Scenario, byte[] Png)> views,
                               IReadOnlyList<WorkbenchReviewThread> threads,
                               WorkbenchPageReview? review,
                               IStringLocalizer localizer)
    {
        var html = HtmlEncoder.Default;
        var title = localizer[page.TitleKey].Value;
        var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
        var builder = new StringBuilder();

        builder.Append($"<!DOCTYPE html><html lang=\"{CultureInfo.CurrentUICulture.Name}\" dir=\"{(isRtl ? "rtl" : "ltr")}\"><head><meta charset=\"utf-8\" />");
        builder.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.Append($"<title>{html.Encode($"{title} · {localizer[nameof(WorkbenchStrings.ReviewSheet)]}")}</title><style>{Styles}</style></head><body>");

        builder.Append($"<header><h1>{html.Encode(title)}");

        if (review is not null)
        {
            builder.Append($"<span class=\"status\">{html.Encode(localizer[$"PageStatus{review.Status}"])}</span>");
        }

        builder.Append("</h1>");
        builder.Append($"<div class=\"meta\">{html.Encode(localizer[page.DescriptionKey])}</div>");

        if (review is { By: not null, At: not null })
        {
            builder.Append($"<div class=\"meta\">{html.Encode(localizer[nameof(WorkbenchStrings.PageStatusSetBy), review.By, review.At.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture)])}</div>");
        }

        builder.Append("</header>");

        builder.Append($"<h2>{html.Encode(localizer[nameof(WorkbenchStrings.ReviewSheetViews)])}</h2><div class=\"views\">");

        foreach (var (scenario, png) in views)
        {
            builder.Append($"<figure><img alt=\"{html.Encode(Describe(scenario, localizer))}\" src=\"data:image/png;base64,{Convert.ToBase64String(png)}\" />");
            builder.Append($"<figcaption>{html.Encode(Describe(scenario, localizer))}</figcaption></figure>");
        }

        builder.Append("</div>");

        builder.Append($"<h2>{html.Encode(localizer[nameof(WorkbenchStrings.ReviewSheetComments)])}</h2>");

        if (threads.Count == 0)
        {
            builder.Append($"<p class=\"meta\">{html.Encode(localizer[nameof(WorkbenchStrings.ReviewSheetNoComments)])}</p>");
        }

        foreach (var thread in threads)
        {
            var element = string.Join(" · ", new[] { thread.Anchor.Component, thread.Anchor.Text is null ? null : $"“{thread.Anchor.Text}”" }.Where(part => part is not null));

            builder.Append($"<div class=\"comment\"><div class=\"comment-head\"><span class=\"number {thread.Status.ToString().ToLowerInvariant()}\">{thread.Number}</span>");
            builder.Append($"<span><span class=\"element\">{html.Encode(element.Length > 0 ? element : localizer[nameof(WorkbenchStrings.ThisSpot)])}</span> · {html.Encode(localizer[$"ReviewStatus{thread.Status}"])}<br /><span class=\"view\">{html.Encode(Describe(thread.Scenario, localizer))}</span></span></div>");

            foreach (var message in thread.Messages)
            {
                builder.Append($"<div class=\"message{(message.FromAgent ? " agent" : null)}\"><span class=\"author\">{html.Encode(message.Author)} · {html.Encode(message.At.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture))}</span>{html.Encode(message.Text)}</div>");
            }

            if (thread.Tasks.Length > 0)
            {
                builder.Append("<ul class=\"tasks\">");

                foreach (var task in thread.Tasks)
                {
                    builder.Append($"<li class=\"{(task.Done ? "done" : null)}\">{html.Encode(task.Text)}</li>");
                }

                builder.Append("</ul>");
            }

            builder.Append("</div>");
        }

        builder.Append($"<footer>{html.Encode(localizer[nameof(WorkbenchStrings.ReviewSheetMade), DateTimeOffset.Now.ToString("g", CultureInfo.CurrentUICulture)])}</footer>");
        builder.Append("</body></html>");

        return builder.ToString();
    }

    /// <summary>A view's device, theme, language, data state and sign-in, as one line.</summary>
    private static string Describe(WorkbenchScenario scenario, IStringLocalizer localizer)
    {
        var device = scenario.DevicePreset;
        var (width, height) = device.GetViewport(scenario.Rotated);
        var parts = new List<string> { device.Key == DevicePresets.CustomKey ? FormattableString.Invariant($"{width}×{height}") : device.Name };

        parts.Add(localizer[$"Theme{scenario.Theme}"]);
        parts.Add(CultureInfoManager.SupportedCultures.FirstOrDefault(sc => sc.Culture.Name == (scenario.Culture ?? CultureInfoManager.DefaultCulture.Name)).DisplayName ?? scenario.Culture ?? string.Empty);
        parts.Add(localizer[$"Data{scenario.Data}"]);

        if (scenario.Auth is not WorkbenchAuthState.SignedIn) parts.Add(localizer[scenario.Auth.ToString()]);

        return string.Join(" · ", parts.Where(part => string.IsNullOrEmpty(part) is false));
    }
}
