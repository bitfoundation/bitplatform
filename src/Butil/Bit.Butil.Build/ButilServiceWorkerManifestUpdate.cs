namespace Bit.Butil.Build;

/// <summary>What <see cref="ButilServiceWorkerManifest.Update"/> made of a service worker assets manifest.</summary>
public sealed class ButilServiceWorkerManifestUpdate(string text, string previousVersion, string version, int dropped, int rehashed)
{
    /// <summary>The manifest to publish: the input itself, unchanged, when there was nothing to change.</summary>
    public string Text { get; } = text;

    /// <summary>
    /// The manifest's version as it was read: the one the service worker script still names, and so the one
    /// <see cref="ButilServiceWorkerManifest.UpdateVersion"/> looks for in it.
    /// </summary>
    public string PreviousVersion { get; } = previousVersion;

    /// <summary>
    /// The version of the manifest in <see cref="Text"/> - the manifest's, not the package's: a hash over what it
    /// lists, in the shape of the SDK's own, or <see cref="PreviousVersion"/> when there was nothing to change.
    /// </summary>
    public string Version { get; } = version;

    /// <summary>Entries removed because the publish no longer serves their file.</summary>
    public int Dropped { get; } = dropped;

    /// <summary>Entries whose hash was replaced with that of the file the publish serves instead.</summary>
    public int Rehashed { get; } = rehashed;

    /// <summary>
    /// Whether anything was dropped or rehashed. When not, <see cref="Text"/> is the input itself and neither the
    /// manifest nor the service worker script needs publishing anew.
    /// </summary>
    public bool Changed => Dropped > 0 || Rehashed > 0;
}
