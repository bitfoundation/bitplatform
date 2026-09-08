namespace Bit.Butil;

/// <summary>
/// One app the browser found installed alongside this web app - an entry from
/// <see cref="Navigator.GetInstalledRelatedApps"/>.
/// </summary>
/// <remarks>
/// Only apps listed in the manifest's <c>related_applications</c> are ever reported, so this cannot
/// be used to enumerate what a user has installed.
/// </remarks>
public class RelatedApp
{
    /// <summary>The platform-specific id - a package name on Android, an app id on Windows. Empty for a web app.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The platform the app is installed on: <c>"play"</c>, <c>"windows"</c>, <c>"webapp"</c>, and so on.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>The app's URL. Set for a <c>"webapp"</c> entry, which points at its manifest.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>The installed version, when the platform reports one.</summary>
    public string Version { get; set; } = string.Empty;
}
