using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

// Media embeds (YouTube/Vimeo/video/audio) and horizontal rule.
public partial class BitRichTextEditor
{
    private bool _showMediaInput;
    private string _mediaUrl = "";

    private void ToggleMediaInput()
    {
        _showMediaInput = !_showMediaInput;
        _mediaUrl = "";
        ClearInlineError();
    }

    private async Task ApplyMediaAsync()
    {
        if (ControlsDisabled) return;

        var url = _mediaUrl.Trim();
        if (string.IsNullOrWhiteSpace(url) || url.Length > 2048
            || Uri.TryCreate(url, UriKind.Absolute, out var uri) is false
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-url", Label("media-url-invalid", "That media URL is not valid.")));
            return;
        }

        var html = BuildMediaEmbed(uri);
        if (html is null)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("media-not-allowed", Label("media-not-allowed", "That media type or host is not supported.")));
            return;
        }

        // Only tear down the input UI when the insert actually succeeded; on rejection the bridge
        // raises an error via OnClientError, so leave the panel, url, and any error message intact.
        var inserted = await _js.BitRichTextEditorInsertMedia(_editorRef, html);
        if (inserted is false) return;
        // The media embedded successfully, so clear any stale validation message.
        ClearInlineError();
        _showMediaInput = false;
        _mediaUrl = "";
    }

    private static string? BuildMediaEmbed(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();
        var url = uri.AbsoluteUri;

        // YouTube
        var ytId = TryGetYouTubeId(uri);
        if (ytId is not null)
            return $"<iframe width=\"560\" height=\"315\" src=\"https://www.youtube-nocookie.com/embed/{Esc(ytId)}\" " +
                   "frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";

        // Vimeo
        if (IsHostOrSubdomainOf(host, "vimeo.com"))
        {
            var m = Regex.Match(uri.AbsolutePath, @"/(\d+)");
            if (m.Success)
                return $"<iframe src=\"https://player.vimeo.com/video/{m.Groups[1].Value}\" width=\"560\" height=\"315\" frameborder=\"0\" allow=\"autoplay; fullscreen; picture-in-picture\" allowfullscreen></iframe>";
        }

        // Direct media files
        var path = uri.AbsolutePath.ToLowerInvariant();
        if (path.EndsWith(".mp4") || path.EndsWith(".webm") || path.EndsWith(".ogv"))
            return $"<video src=\"{Esc(url)}\" controls width=\"560\"></video>";
        if (path.EndsWith(".mp3") || path.EndsWith(".ogg") || path.EndsWith(".wav"))
            return $"<audio src=\"{Esc(url)}\" controls></audio>";

        return null;
    }

    private static string? TryGetYouTubeId(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();
        string? id = null;
        if (IsHostOrSubdomainOf(host, "youtu.be"))
        {
            id = uri.AbsolutePath.Trim('/').Split('/')[0];
        }
        else if (IsHostOrSubdomainOf(host, "youtube.com"))
        {
            var v = GetQueryValue(uri.Query, "v");
            if (string.IsNullOrEmpty(v) is false)
            {
                id = v;
            }
            else
            {
                var m = Regex.Match(uri.AbsolutePath, @"/embed/([\w-]+)");
                if (m.Success) id = m.Groups[1].Value;
            }
        }
        return IsValidYouTubeId(id) ? id : null;
    }

    // YouTube ids are short, URL-safe base64-ish tokens; constrain before embedding in HTML.
    private static bool IsValidYouTubeId(string? id)
        => id is { Length: > 0 and <= 20 } && id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

    // Exact host or a true subdomain match (e.g. "www.youtube.com" but not "youtube.com.evil.test").
    private static bool IsHostOrSubdomainOf(string host, string domain)
        => string.Equals(host, domain, StringComparison.OrdinalIgnoreCase)
           || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return null;
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            if (pair.AsSpan(0, eq).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                // Malformed percent-encoding throws; treat an invalid encoded value as missing so
                // ApplyMediaAsync surfaces its existing validation error instead of crashing.
                try { return Uri.UnescapeDataString(pair[(eq + 1)..]); }
                catch (UriFormatException) { return null; }
                catch (ArgumentException) { return null; }
            }
        }
        return null;
    }

    private static string Esc(string s)
        => s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");

    // ---- horizontal rule ----
    private async Task InsertRuleAsync()
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorExec(_editorRef, "insertHorizontalRule", null);
    }
}
