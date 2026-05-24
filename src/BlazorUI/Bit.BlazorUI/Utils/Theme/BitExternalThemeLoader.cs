namespace Bit.BlazorUI;

/// <summary>
/// Loads optional alternate CSS bundles by id (creates or updates a <c>&lt;link rel="stylesheet"&gt;</c>).
/// Use only trusted/same-origin <paramref name="href"/> values.
/// </summary>
public sealed class BitExternalThemeLoader
{
    private readonly IJSRuntime _js;

    public BitExternalThemeLoader(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);

        _js = js;
    }

    public ValueTask AttachStylesheetAsync(string linkElementId, string href)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkElementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(href);

        return _js.BitExternalThemeAttach(linkElementId, href);
    }

    public ValueTask DetachStylesheetAsync(string linkElementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkElementId);

        return _js.BitExternalThemeDetach(linkElementId);
    }
}
