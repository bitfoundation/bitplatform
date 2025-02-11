namespace Boilerplate.Shared.Services;

public static class UriUtils
{
    public static string? Escape(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        return Uri.EscapeDataString(Uri.UnescapeDataString(url));
    }
}
