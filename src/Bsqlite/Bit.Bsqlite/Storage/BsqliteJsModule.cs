namespace Bit.Bsqlite.Storage;

/// <summary>
/// Locates the JavaScript half of a storage backend: an ES module the worker imports dynamically,
/// exporting a function that registers the VFS with the engine.
/// </summary>
/// <remarks>
/// A backend is a two-sided artifact. The .NET side declares capabilities and answers probes; the
/// VFS itself is JavaScript, served from the provider package's own static assets, which is what
/// keeps VFS code out of the core.
/// </remarks>
public sealed record BsqliteJsModule
{
    /// <param name="moduleUrl">
    /// URL of the ES module, normally an RCL static asset path such as
    /// <c>./_content/Bit.Bsqlite.Storage.Opfs/bit-bsqlite-opfs-vfs.js</c>.
    /// </param>
    /// <param name="registerExport">Name of the exported registration function.</param>
    public BsqliteJsModule(string moduleUrl, string registerExport = "register")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(registerExport);

        ModuleUrl = moduleUrl;
        RegisterExport = registerExport;
    }

    /// <summary>URL the worker imports.</summary>
    public string ModuleUrl { get; }

    /// <summary>The module export that registers the VFS.</summary>
    public string RegisterExport { get; }
}
