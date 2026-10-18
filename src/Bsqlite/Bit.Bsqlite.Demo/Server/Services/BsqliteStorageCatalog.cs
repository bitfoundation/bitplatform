using System.Text;
using System.Reflection;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.Opfs;
using Bit.Bsqlite.Demo.Server.Dtos;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.CacheStorage;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// The four first-party storage backends, read out of the library itself: every name, module URL and
/// capability comes from an instance of the provider type, every capability's meaning from the XML
/// documentation of the property that declares it, and every refused statement from running the
/// library's own guards under the limits the session factory derives from those capabilities.
/// <para>
/// Nothing here is a table someone keeps up to date. The capability rows are the properties of
/// <see cref="BsqliteStorageCapabilities"/>, so a capability added to the contract shows up the moment
/// it is declared; and a backend whose declaration changes is reported as it now is - which is the
/// point: "OPFS cannot ATTACH, IndexedDB serializes readers" is exactly the kind of fact an agent half
/// remembers from another version.
/// </para>
/// <para>
/// The providers are built without an <c>IJSRuntime</c>, which every first-party constructor
/// accepts: there is no browser here, and nothing on this server ever probes one. Name, capabilities
/// and modules are declarations, not discoveries, so they do not need one.
/// </para>
/// </summary>
public static class BsqliteStorageCatalog
{
    /// <summary>
    /// The statements the guards exist for, one per rule of <see cref="BsqliteFeatureGuards"/>: the
    /// product-wide WAL ban, and the two capability-gated ones. Every verdict about them is computed.
    /// </summary>
    public static readonly string[] GuardedStatements =
    [
        "PRAGMA journal_mode = WAL",
        "ATTACH DATABASE 'other.db' AS other",
        "PRAGMA page_size = 8192",
    ];

    private static readonly Lazy<IBsqliteStorageProvider[]> _instances = new(() =>
    [
        new BsqliteOpfsStorageProvider(),
        new BsqliteIndexedDbStorageProvider(),
        new BsqliteCacheStorageProvider(),
        new BsqliteInMemoryStorageProvider(),
    ]);

    private static readonly Lazy<BsqliteStorageProviderDto[]> _providers = new(() => [.. _instances.Value.Select(Describe)]);

    /// <summary>The four backends, in the order the README recommends them: fastest first, volatile last.</summary>
    public static BsqliteStorageProviderDto[] Providers => _providers.Value;

    /// <summary>
    /// A first-party provider instance by its name, for the storage lab to borrow the real
    /// capabilities and modules from. Null for a name no first-party backend uses.
    /// </summary>
    public static IBsqliteStorageProvider? Instance(string? name)
        => _instances.Value.FirstOrDefault(provider => string.Equals(provider.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>One backend by its name ("opfs"), its type name ("BsqliteOpfsStorageProvider") or its package, or null.</summary>
    public static BsqliteStorageProviderDto? Find(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var text = key.Trim();
        var squashed = Squash(text);

        return Providers.FirstOrDefault(provider => string.Equals(provider.Name, text, StringComparison.OrdinalIgnoreCase))
            ?? Providers.FirstOrDefault(provider => string.Equals(provider.TypeName, text, StringComparison.OrdinalIgnoreCase))
            ?? Providers.FirstOrDefault(provider => string.Equals(provider.Package, text, StringComparison.OrdinalIgnoreCase))
            // "IndexedDB", "Cache Storage", "inmemory": the backend as a person spells it.
            ?? Providers.FirstOrDefault(provider => Squash(provider.Name) == squashed)
            ?? Providers.FirstOrDefault(provider => Squash(provider.TypeName) == $"bsqlite{squashed}storageprovider");
    }

    /// <summary>The backends as one Markdown table, as GetBsqliteStorageProviders answers when it is called without a name.</summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();
        var capabilities = CapabilityProperties();

        builder.AppendLine("# Bit.Bsqlite storage backends").AppendLine();
        builder.AppendLine("The four first-party backends, each with the capabilities its provider type really declares - what selection, ")
               .AppendLine("the SQL guards and the migrator act on. Call `GetBsqliteStorageProviders(provider: \"...\")` for one of them in full, ")
               .AppendLine("with its package, its modules and what each capability means.")
               .AppendLine();

        builder.Append("| Name | Package |");
        foreach (var property in capabilities) builder.Append($" {property.Name} |");
        builder.AppendLine();

        builder.Append("| --- | --- |");
        foreach (var _ in capabilities) builder.Append(" --- |");
        builder.AppendLine();

        foreach (var provider in Providers)
        {
            builder.Append($"| `{provider.Name}` | `{provider.Package}` |");
            foreach (var capability in provider.Capabilities) builder.Append($" {capability.Value} |");
            builder.AppendLine();
        }

        builder.AppendLine()
               .AppendLine("Refused by the guards (`BsqliteFeatureGuards`), per backend:")
               .AppendLine();

        foreach (var provider in Providers)
        {
            var refused = provider.Guarded.Where(verdict => verdict.Accepted is false).Select(verdict => $"`{verdict.Name}`").ToArray();

            builder.AppendLine($"- `{provider.Name}`: {(refused.Length == 0 ? "nothing" : string.Join(", ", refused))}");
        }

        builder.AppendLine()
               .AppendLine("Register a backend with `AddStorageProvider<T>()` and rank it with `UseStorage(s => s.Prefer(...).Fallback(...))`. ")
               .AppendLine("Selection prefers that order, but a database already bound to a backend stays there; falling back to a backend ")
               .AppendLine("that does not persist needs `AllowNonPersistentFallback()`. `PlanBsqliteStorageSelection` runs that selection on ")
               .AppendLine("a browser you describe, and `GetBsqliteSetupGuide(scenario: \"storage\")` walks through choosing.");

        return builder.ToString();
    }

    /// <summary>The full description of one backend as Markdown, or null when no first-party backend goes by that name.</summary>
    public static string? Render(string key)
    {
        var provider = Find(key);
        if (provider is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# {provider.TypeName} (`{provider.Name}`)").AppendLine();
        builder.AppendLine($"Package `{provider.Package}` - `using {provider.Namespace};`").AppendLine();

        if (provider.Summary is not null) builder.AppendLine(provider.Summary).AppendLine();
        if (provider.Remarks is not null) builder.AppendLine(provider.Remarks).AppendLine();

        builder.AppendLine("## Capabilities").AppendLine()
               .AppendLine($"Read off `new {provider.TypeName}().Capabilities`. The core enforces every one of them generically - it never tests a provider's name.")
               .AppendLine();

        foreach (var capability in provider.Capabilities)
        {
            builder.Append($"- **{capability.Name}** = `{capability.Value}`");
            if (capability.Summary is not null) builder.Append($" - {capability.Summary}");
            builder.AppendLine();
        }

        builder.AppendLine().AppendLine("## What the guards refuse here").AppendLine()
               .AppendLine("Each statement run through `BsqliteFeatureGuards.EnsureSupported` under the `BsqliteRuntimeLimits` a session on this backend gets:")
               .AppendLine();

        foreach (var verdict in provider.Guarded)
        {
            builder.AppendLine(verdict.Accepted
                ? $"- `{verdict.Name}` - accepted"
                : $"- `{verdict.Name}` - refused: {verdict.Error}");
        }

        builder.AppendLine().AppendLine("## Modules").AppendLine();

        builder.AppendLine(provider.VfsModule is null
            ? "- VFS: none - the storage is the engine's own memory, so there is no JavaScript to load."
            : $"- VFS module: `{provider.VfsModule}`{(provider.VfsName is null ? null : $" (registers the `{provider.VfsName}` VFS)")}");

        if (provider.AdminModule is not null) builder.AppendLine($"- Probe and admin module, imported on the main thread: `{provider.AdminModule}`");

        builder.AppendLine()
               .AppendLine("Both are static web assets of the package, named relative to the document so an app served under a sub-path still finds them; ")
               .AppendLine("nothing needs adding to the host page.")
               .AppendLine()
               .AppendLine("## Registration").AppendLine()
               .AppendLine("```csharp")
               .AppendLine($"using {provider.Namespace};")
               .AppendLine()
               .AppendLine("builder.Services.AddBitBsqliteServices(sqlite => sqlite")
               .AppendLine($"    .AddStorageProvider<{provider.TypeName}>()")
               .AppendLine($"    .UseStorage(s => s.Prefer({provider.TypeName}.ProviderName))")
               .AppendLine("    .AddDatabase(\"app.db\"));")
               .AppendLine("```");

        var readme = BsqliteSourceCatalog.ResolveDocumentName(provider.Package);
        if (readme is not null) builder.AppendLine().AppendLine($"The package's own notes: `GetBsqliteDocument(name: \"{readme}\")`.");

        return builder.ToString();
    }

    /// <summary>
    /// The limits a session on a backend runs under - built exactly as the session factory builds them
    /// from the backend's capabilities, so the guards here judge a statement the way the connection will.
    /// </summary>
    public static BsqliteRuntimeLimits LimitsOf(BsqliteStorageCapabilities capabilities) => new()
    {
        SupportsMultiDatabaseTransactions = capabilities.SupportsMultiDatabaseTransactions,
        CanChangePageSize = capabilities.CanChangePageSize,
    };

    /// <summary>What the library's guards make of one statement under one backend's limits. Nothing is executed.</summary>
    public static BsqliteSqlVerdictDto Judge(string name, string sql, BsqliteRuntimeLimits limits)
    {
        try
        {
            BsqliteFeatureGuards.EnsureSupported(sql, limits);

            return new BsqliteSqlVerdictDto { Name = name, Accepted = true };
        }
        catch (BsqliteException exception)
        {
            return new BsqliteSqlVerdictDto { Name = name, Accepted = false, Error = exception.Message };
        }
    }

    /// <summary>A capability's value as C# prints it, with a [Flags] combination spelled out.</summary>
    public static string FormatCapability(object? value) => value switch
    {
        null => "null",
        bool flag => flag ? "true" : "false",
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>The capability properties of the contract, in declaration order - the rows of every table here.</summary>
    public static PropertyInfo[] CapabilityProperties()
        => [.. typeof(BsqliteStorageCapabilities).GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(property => property.Name != "EqualityContract")
               .OrderBy(property => property.MetadataToken)];

    private static BsqliteStorageProviderDto Describe(IBsqliteStorageProvider provider)
    {
        var type = provider.GetType();
        var limits = LimitsOf(provider.Capabilities);

        return new BsqliteStorageProviderDto
        {
            Name = provider.Name,
            TypeName = type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Package = BsqliteApiCatalog.PackageOf(type),
            Summary = BsqliteXmlDocs.GetSummary($"T:{type.FullName}"),
            Remarks = BsqliteXmlDocs.GetRemarks($"T:{type.FullName}"),
            VfsName = Constant(type, "VfsName"),
            VfsModule = provider.VfsModule?.ModuleUrl,
            AdminModule = Constant(type, "AdminModuleUrl"),
            Capabilities = [.. CapabilityProperties().Select(property => new BsqliteCapabilityDto
            {
                Name = property.Name,
                Value = FormatCapability(property.GetValue(provider.Capabilities)),
                Summary = BsqliteXmlDocs.GetSummary($"P:{typeof(BsqliteStorageCapabilities).FullName}.{property.Name}")
            })],
            Guarded = [.. GuardedStatements.Select(sql => Judge(sql, sql, limits))]
        };
    }

    /// <summary>A public string constant a provider type declares, such as its VFS name - read, not copied.</summary>
    private static string? Constant(Type type, string name)
        => type.GetField(name, BindingFlags.Public | BindingFlags.Static) is { IsLiteral: true } field ? field.GetRawConstantValue() as string : null;

    private static string Squash(string text) => new([.. text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
}
