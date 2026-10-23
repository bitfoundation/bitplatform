using Bit.Bmage.Text;

namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>
/// The font the editor draws text and step numbers with. <c>BmageEditor</c> turns text into outlines, which needs a
/// TrueType font registered with the <see cref="FontTextRasterizer"/> the services provide - a browser's own fonts are
/// out of its reach. This site ships Noto Sans (SIL Open Font License, Server/wwwroot/fonts) and fetches it the first
/// time a page with an editor asks, rather than holding up the start of every page for it.
/// </summary>
/// <remarks>
/// Noto Sans covers Latin, Greek and Cyrillic. The font rasteriser does not shape complex scripts or reorder
/// right-to-left text, so a Persian label typed into the editor has no glyphs to draw.
/// </remarks>
public sealed class DemoFonts(HttpClient http, FontTextRasterizer fonts)
{
    private Task? _loading;

    /// <summary>Registers the font the first time it is called; every later call waits for that one download.</summary>
    public Task EnsureAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            if (fonts.HasFonts) return;
            var regular = http.GetByteArrayAsync("fonts/NotoSans-Regular.ttf");
            var bold = http.GetByteArrayAsync("fonts/NotoSans-Bold.ttf");
            // The first family registered is also the one a style naming no registered family gets, the editor's
            // "sans-serif" included, bold face and all.
            fonts.Register("Noto Sans", FontFile.Load(await regular))
                 .Register("Noto Sans", FontFile.Load(await bold), bold: true);
        }
        catch
        {
            // A later page may try again: a failed download is not the last word.
            _loading = null;
            throw;
        }
    }
}
