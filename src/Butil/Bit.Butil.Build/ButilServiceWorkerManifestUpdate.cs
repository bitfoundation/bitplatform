namespace Bit.Butil.Build;

/// <summary>What <see cref="ButilServiceWorkerManifest.Update"/> made of a service worker assets manifest.</summary>
public sealed class ButilServiceWorkerManifestUpdate(string text, string previousVersion, string version, int dropped, int rehashed)
{
    /// <summary>The manifest to publish: the input itself, unchanged, when there was nothing to change.</summary>
    public string Text { get; } = text;

    public string PreviousVersion { get; } = previousVersion;

    public string Version { get; } = version;

    /// <summary>Entries removed because the publish no longer serves their file.</summary>
    public int Dropped { get; } = dropped;

    /// <summary>Entries whose hash was replaced with that of the file the publish serves instead.</summary>
    public int Rehashed { get; } = rehashed;

    public bool Changed => Dropped > 0 || Rehashed > 0;
}
