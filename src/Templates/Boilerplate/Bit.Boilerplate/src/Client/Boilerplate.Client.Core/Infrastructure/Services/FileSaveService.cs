namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Hands a produced file to the user through the one mechanism every head shares: a blob URL on a hidden
/// <c>&lt;a download&gt;</c> that gets clicked - the hybrid heads render in a WebView, so they take the same path.
/// A service rather than a helper because an authenticated download cannot be a plain link: the token travels in a
/// header, so the bytes arrive over HttpClient.
/// </summary>
public partial class FileSaveService
{
    [AutoInject] private readonly ObjectUrls objectUrls = default!;

    private string? previousObjectUrl;

    /// <summary>
    /// The anchor to download through. A service cannot render an element, so <c>AppFileSaveAnchor</c> renders one per
    /// layout and leaves it here.
    /// </summary>
    internal ElementReference? Anchor { get; set; }

    public virtual async Task Save(string fileName, string contentType, byte[] content)
    {
        if (Anchor is not { } anchor)
            throw new InvalidOperationException($"No anchor to download through. {nameof(AppFileSaveAnchor)} has to be rendered by the layout.");

        // The previous URL, not this one: the browser reads the blob after the click returns, so revoking the one
        // just handed over cancels the download. track:false because this owns the lifetime instead.
        if (previousObjectUrl is not null)
        {
            await objectUrls.Revoke(previousObjectUrl);
        }

        previousObjectUrl = await objectUrls.Create(content, contentType, track: false);

        // Both values are the app's own - a blob: URL it just created, and a file name from its own server.
        await anchor.SetAttribute("href", previousObjectUrl);
        await anchor.SetAttribute("download", fileName);

        await anchor.Click();
    }
}
