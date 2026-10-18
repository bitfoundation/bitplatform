using Bit.BlazeDb.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazeDb.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Provider configuration for a context. The engine itself is opened from the EF model on first
/// use - tables come from entity types, not from <see cref="BlazeDbDatabaseOptions.AddTable"/> -
/// and is shared by every context with the same store identity.
/// </summary>
public sealed class BlazeDbOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public BlazeDbOptionsExtension()
    {
    }

    private BlazeDbOptionsExtension(BlazeDbOptionsExtension copyFrom)
    {
        ExistingDatabase = copyFrom.ExistingDatabase;
        Storage = copyFrom.Storage;
        DatabaseName = copyFrom.DatabaseName;
        Configure = copyFrom.Configure;
    }

    /// <summary>An already-open engine, analogous to passing an existing ADO.NET connection.</summary>
    public BlazeDbDatabase? ExistingDatabase { get; private set; }

    /// <summary>Persistent backend. When set, it is the store identity.</summary>
    public IBlazeDbStorage? Storage { get; private set; }

    /// <summary>
    /// Identity of an in-memory store. Contexts that share a name (or share the same options
    /// instance) share one engine; a unique name isolates a store.
    /// </summary>
    public string? DatabaseName { get; private set; }

    /// <summary>Engine settings (flush interval, quota, …). Tables are still taken from the model.</summary>
    public Action<BlazeDbDatabaseOptions>? Configure { get; private set; }

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public BlazeDbOptionsExtension WithDatabase(BlazeDbDatabase database)
    {
        var clone = Clone();
        clone.ExistingDatabase = database;
        return clone;
    }

    public BlazeDbOptionsExtension WithStorage(IBlazeDbStorage storage)
    {
        var clone = Clone();
        clone.Storage = storage;
        return clone;
    }

    public BlazeDbOptionsExtension WithDatabaseName(string name)
    {
        var clone = Clone();
        clone.DatabaseName = name;
        return clone;
    }

    public BlazeDbOptionsExtension WithConfigure(Action<BlazeDbDatabaseOptions> configure)
    {
        var clone = Clone();
        clone.Configure = configure;
        return clone;
    }

    internal object StoreKey
    {
        get
        {
            if (ExistingDatabase is not null)
            {
                return ExistingDatabase;
            }
            if (Storage is not null)
            {
                return Storage;
            }
            return DatabaseName
                   ?? throw new InvalidOperationException(
                       "No BlazeDb store was configured. Call optionsBuilder.UseBlazeDb().");
        }
    }

    internal bool OwnsEngine => ExistingDatabase is null;

    internal BlazeDbDatabaseOptions CreateEngineOptions(IEnumerable<BlazeDbTableDescriptor> tables)
    {
        var options = new BlazeDbDatabaseOptions { Storage = Storage };
        Configure?.Invoke(options);
        if (Storage is not null)
        {
            options.Storage = Storage;
        }
        options.Tables.Clear();
        foreach (var table in tables)
        {
            options.AddTable(table);
        }
        return options;
    }

    public void ApplyServices(IServiceCollection services) => services.AddEntityFrameworkBlazeDb();

    public void Validate(IDbContextOptions options)
    {
        if (ExistingDatabase is null && Storage is null && string.IsNullOrEmpty(DatabaseName))
        {
            throw new InvalidOperationException(
                "No BlazeDb store was configured. Call optionsBuilder.UseBlazeDb(), " +
                "UseBlazeDb(storage), or UseBlazeDb(name).");
        }
    }

    private BlazeDbOptionsExtension Clone() => new(this);

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        private new BlazeDbOptionsExtension Extension => (BlazeDbOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => true;

        public override string LogFragment => "using Bit.BlazeDb ";

        // Which store a context talks to is not part of the service graph - the provider reads
        // it from the options per context - so every BlazeDb context can share one service
        // provider. Keying on the store instead would build a new one per database and trip
        // EF's "too many service providers" guard.
        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) =>
            debugInfo["BlazeDb:Store"] = Extension.StoreKey.GetHashCode().ToString();
    }
}
