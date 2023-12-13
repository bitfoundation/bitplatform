using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Bit.Besql;

public class BesqlDbContextFactory<TContext> : DbContextFactory<TContext>
    where TContext : DbContext
{
    private static readonly IDictionary<Type, string> FileNames = new Dictionary<Type, string>();

    private readonly IBesqlStorage cache;
    private Task<int>? startupTask = null;
    private int lastStatus = -2;

    public BesqlDbContextFactory(
        IBesqlStorage cache,
        IServiceProvider serviceProvider,
        DbContextOptions<TContext> options,
        IDbContextFactorySource<TContext> factorySource)
        : base(serviceProvider, options, factorySource)
    {
        this.cache = cache;
        startupTask = RestoreAsync();
    }

    private static string Filename => FileNames[typeof(TContext)];

    private static string BackupFile => $"{BesqlDbContextFactory<TContext>.Filename}_bak";

    public static void Reset() => FileNames.Clear();

    public static string? GetFilenameForType() =>
        FileNames.ContainsKey(typeof(TContext)) ? FileNames[typeof(TContext)] : null;

    public override async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        await CheckForStartupTaskAsync();

        var ctx = await base.CreateDbContextAsync(cancellationToken);

        ctx.SavedChanges += SyncDbToCacheAsync;

        return ctx;
    }

    private async Task DoSwap(string source, string target)
    {
        await using var src = new SqliteConnection($"Data Source={source}");
        await using var tgt = new SqliteConnection($"Data Source={target}");

        await src.OpenAsync();
        await tgt.OpenAsync();

        src.BackupDatabase(tgt);

        await tgt.CloseAsync();
        await src.CloseAsync();
    }

    private async Task<string> GetFilename()
    {
        await using var ctx = await base.CreateDbContextAsync();
        var filename = "filenotfound.db";
        var type = ctx.GetType();
        if (FileNames.TryGetValue(type, out var value))
        {
            return value;
        }

        var cs = ctx.Database.GetConnectionString();

        if (cs != null)
        {
            var file = cs.Split(';').Select(s => s.Split('='))
                .Select(split => new
                {
                    key = split[0].ToLowerInvariant(),
                    value = split[1],
                })
                .Where(kv => kv.key.Contains("data source") ||
                    kv.key.Contains("datasource") ||
                    kv.key.Contains("filename"))
                .Select(kv => kv.value)
                .FirstOrDefault();
            if (file != null)
            {
                filename = file;
            }
        }

        FileNames.Add(type, filename);
        return filename;
    }

    private async Task CheckForStartupTaskAsync()
    {
        if (startupTask != null)
        {
            lastStatus = await startupTask;
            startupTask?.Dispose();
            startupTask = null;
        }
    }

    private async void SyncDbToCacheAsync(object sender, SavedChangesEventArgs e)
    {
        var ctx = (TContext)sender;
        await ctx.Database.CloseConnectionAsync();
        await CheckForStartupTaskAsync();
        if (e.EntitiesSavedCount > 0)
        {
            // unique to avoid conflicts. Is deleted after caching.
            var backupName = $"{BesqlDbContextFactory<TContext>.BackupFile}-{Guid.NewGuid().ToString().Split('-')[0]}";
            await DoSwap(BesqlDbContextFactory<TContext>.Filename, backupName);
            lastStatus = await cache.SyncDb(backupName);
        }
    }

    private async Task<int> RestoreAsync()
    {
        var filename = $"{await GetFilename()}_bak";
        lastStatus = await cache.SyncDb(filename);
        if (lastStatus == 0)
        {
            await DoSwap(filename, FileNames[typeof(TContext)]);
        }

        return lastStatus;
    }
}
