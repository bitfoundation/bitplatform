using Bit.Bmage.Text;

namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>
/// The fonts the editor draws text and step numbers with. <c>BmageEditor</c> turns text into outlines, which needs
/// TrueType fonts registered with the <see cref="FontTextRasterizer"/> the services provide - a browser's own fonts are
/// out of its reach. This site ships Noto Sans for Latin, Greek and Cyrillic and Vazirmatn for Persian and Arabic (both
/// SIL Open Font License, Server/wwwroot/fonts), and fetches them the first time a page with an editor asks, rather
/// than holding up the start of every page for them.
/// </summary>
/// <remarks>
/// Noto Sans is registered first, so it is what the editor's "sans-serif" means; a character it lacks - every Persian
/// letter - is drawn with Vazirmatn, shaped, joined and ordered right to left by the rasteriser.
/// </remarks>
public sealed class DemoFonts(HttpClient http, FontTextRasterizer fonts)
{
    private Task? _loading;

    /// <summary>Registers the fonts the first time it is called; every later call waits for that one download.</summary>
    public Task EnsureAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            if (fonts.HasFonts) return;
            // All four or none: a half-registered set would stop a later retry from adding the rest.
            var files = await Task.WhenAll(
                http.GetByteArrayAsync("fonts/NotoSans-Regular.ttf"),
                http.GetByteArrayAsync("fonts/NotoSans-Bold.ttf"),
                http.GetByteArrayAsync("fonts/Vazirmatn-Regular.ttf"),
                http.GetByteArrayAsync("fonts/Vazirmatn-Bold.ttf"));
            var loaded = files.Select(file => FontFile.Load(file)).ToArray();
            fonts.Register("Noto Sans", loaded[0])
                 .Register("Noto Sans", loaded[1], bold: true)
                 .Register("Vazirmatn", loaded[2])
                 .Register("Vazirmatn", loaded[3], bold: true);
        }
        catch
        {
            // A later page may try again: a failed download is not the last word.
            _loading = null;
            throw;
        }
    }
}
