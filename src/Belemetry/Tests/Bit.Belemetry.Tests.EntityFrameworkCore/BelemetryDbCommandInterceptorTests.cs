using Bit.Belemetry.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.EntityFrameworkCore;

[TestClass]
public class BelemetryDbCommandInterceptorTests
{
    private sealed class Blog
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private sealed class BlogContext(DbContextOptions<BlogContext> options) : DbContext(options)
    {
        public DbSet<Blog> Blogs => Set<Blog>();
    }

    /// <summary>Belemetry with an in-memory exporter, started the way a host starts it (by building the logger factory).</summary>
    private sealed class Host : IAsyncDisposable
    {
        private readonly ServiceProvider _services;

        public Host(Action<BelemetryOptions>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddBitBelemetryServices(o =>
            {
                o.ServiceName = "efcore-tests";
                o.UseEnvironmentVariables = false;
                o.Consent.Default = BelemetryFeatures.All;
                o.Consent.Persist = false;
                o.Persistence.Enabled = false;
                o.Batch.FlushInterval = TimeSpan.FromHours(1);
                o.Metrics.ExportInterval = TimeSpan.FromHours(1);
                o.Metrics.Meters.Clear();
                o.AddExporter(Memory);
                configure?.Invoke(o);
            });
            _services = services.BuildServiceProvider();
            _ = _services.GetRequiredService<ILoggerFactory>();
        }

        public BelemetryInMemoryExporter Memory { get; } = new();

        public async Task<List<BelemetrySpan>> FlushAsync()
        {
            await using var scope = _services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IBelemetry>().FlushAsync();
            return Memory.Spans.Where(s => s.Scope.Name == BelemetryDbCommandInterceptor.SourceName).ToList();
        }

        /// <summary>Forgets the spans so far - the ones of EnsureCreated, which are queued, not just exported.</summary>
        public async Task ResetAsync()
        {
            await FlushAsync();
            Memory.Clear();
        }

        public ValueTask DisposeAsync() => _services.DisposeAsync();
    }

    /// <summary>
    /// A second interceptor: EF Core then calls both through a composite whose async callbacks are state machines,
    /// which is exactly the case where Activity.Current is no longer the span by the time the command has executed.
    /// </summary>
    private sealed class NoopInterceptor : DbCommandInterceptor;

    /// <summary>A context on an open in-memory connection, which keeps the database alive for the test.</summary>
    private sealed class Database : IDisposable
    {
        public Database(Action<BelemetryEntityFrameworkCoreOptions>? configure = null, bool secondInterceptor = false)
        {
            Connection = new SqliteConnection("Data Source=:memory:");
            Connection.Open();
            var builder = new DbContextOptionsBuilder<BlogContext>().UseSqlite(Connection).UseBitBelemetry(configure);
            if (secondInterceptor) builder.AddInterceptors(new NoopInterceptor());
            Context = new BlogContext(builder.Options);
            Context.Database.EnsureCreated();
        }

        public SqliteConnection Connection { get; }

        public BlogContext Context { get; }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }

    [TestMethod]
    public async Task Commands_BecomeClientSpans_WithTheDatabaseConventions()
    {
        await using var host = new Host(o => o.Traces.Sources.Add("Tests.EfCore"));
        using var db = new Database();
        var context = db.Context;
        await host.ResetAsync();

        using var source = new ActivitySource("Tests.EfCore");
        string? parentSpanId, traceId;
        using (var parent = source.StartActivity("Request"))
        {
            Assert.IsNotNull(parent);
            parentSpanId = parent.SpanId.ToHexString();
            traceId = parent.TraceId.ToHexString();
            context.Blogs.Add(new Blog { Title = "first" });
            context.SaveChanges();
            Assert.HasCount(1, context.Blogs.Where(b => b.Title == "first").ToList());
            Assert.AreSame(parent, Activity.Current, "Every command span was stopped and the parent restored.");
        }

        var spans = await host.FlushAsync();
        Assert.IsGreaterThanOrEqualTo(2, spans.Count, "SaveChanges and the query, at least.");
        foreach (var span in spans)
        {
            Assert.AreEqual(BelemetrySpanKind.Client, span.Kind);
            Assert.AreEqual("main", span.Name, "Named after the database, as the OpenTelemetry EF Core instrumentation does.");
            Assert.AreEqual("sqlite", span.Attributes.GetString("db.system.name"));
            Assert.AreEqual("main", span.Attributes.GetString("db.namespace"));
            Assert.IsFalse(span.Attributes.Contains("db.query.text"), "Off by default.");
            Assert.IsFalse(span.Attributes.Contains("server.address"), "A SQLite data source is a file, not a server.");
            Assert.AreEqual(BelemetryStatusCode.Unset, span.Status);
            Assert.AreEqual(parentSpanId, span.ParentSpanId);
            Assert.AreEqual(traceId, span.TraceId);
            Assert.IsNotNull(span.Scope.Version);
        }
        CollectionAssert.Contains(spans.Select(s => s.Attributes.GetString("efcore.command.source")).ToArray(), "SaveChanges");
        CollectionAssert.Contains(spans.Select(s => s.Attributes.GetString("efcore.command.source")).ToArray(), "LinqQuery");
    }

    [TestMethod]
    public async Task AsyncCommands_AreSpansToo()
    {
        await using var host = new Host();
        using var db = new Database();
        var context = db.Context;
        await host.ResetAsync();

        context.Blogs.Add(new Blog { Title = "async" });
        await context.SaveChangesAsync();
        _ = await context.Blogs.CountAsync();
        _ = await context.Blogs.ToListAsync();
        Assert.IsNull(Activity.Current);

        var spans = await host.FlushAsync();
        Assert.IsGreaterThanOrEqualTo(3, spans.Count, "The non-query, the scalar and the reader.");
    }

    [TestMethod]
    public async Task WithASecondInterceptor_AsyncCommandsStillEndTheirSpans()
    {
        await using var host = new Host(o => o.Traces.Sources.Add("Tests.EfCore.Composite"));
        using var db = new Database(secondInterceptor: true);
        var context = db.Context;
        await host.ResetAsync();

        using var source = new ActivitySource("Tests.EfCore.Composite");
        string? parentSpanId;
        using (var parent = source.StartActivity("Request"))
        {
            Assert.IsNotNull(parent);
            parentSpanId = parent.SpanId.ToHexString();
            context.Blogs.Add(new Blog { Title = "composite" });
            await context.SaveChangesAsync();
            _ = await context.Blogs.ToListAsync();
            await Assert.ThrowsExactlyAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync("SELECT * FROM missing"));
            _ = context.Blogs.Count();
            Assert.AreSame(parent, Activity.Current, "No span is left current after the commands.");
        }

        var spans = await host.FlushAsync();
        Assert.IsGreaterThanOrEqualTo(4, spans.Count, "Every command ended its span, async ones included.");
        Assert.IsTrue(spans.All(s => s.ParentSpanId == parentSpanId), "Every span is parented to the request.");
        Assert.AreEqual(1, spans.Count(s => s.Status == BelemetryStatusCode.Error));
    }

    [TestMethod]
    public async Task CaptureQueryText_AddsTheCommandText()
    {
        await using var host = new Host();
        using var db = new Database(o => o.CaptureQueryText = true);
        var context = db.Context;
        await host.ResetAsync();

        _ = context.Blogs.Where(b => b.Id > 0).ToList();

        var span = (await host.FlushAsync()).Single();
        StringAssert.Contains(span.Attributes.GetString("db.query.text"), "SELECT");
    }

    [TestMethod]
    public async Task Filter_SkipsCommands()
    {
        await using var host = new Host();
        using var db = new Database(o => o.Filter = (command, data) => data.CommandSource != Microsoft.EntityFrameworkCore.Diagnostics.CommandSource.SaveChanges);
        var context = db.Context;
        await host.ResetAsync();

        context.Blogs.Add(new Blog { Title = "filtered" });
        context.SaveChanges();
        _ = context.Blogs.ToList();

        var spans = await host.FlushAsync();
        Assert.HasCount(1, spans);
        Assert.AreEqual("LinqQuery", spans[0].Attributes.GetString("efcore.command.source"));
    }

    [TestMethod]
    public async Task AFailedCommand_IsAnErrorSpan()
    {
        await using var host = new Host();
        using var db = new Database();
        var context = db.Context;
        await host.ResetAsync();

        Assert.ThrowsExactly<SqliteException>(() => context.Database.ExecuteSqlRaw("SELECT * FROM missing"));
        await Assert.ThrowsExactlyAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync("SELECT * FROM missing"));
        Assert.IsNull(Activity.Current, "The span is stopped even when the command fails.");

        var spans = await host.FlushAsync();
        Assert.HasCount(2, spans);
        foreach (var span in spans)
        {
            Assert.AreEqual(BelemetryStatusCode.Error, span.Status);
            Assert.AreEqual("Microsoft.Data.Sqlite.SqliteException", span.Attributes.GetString("error.type"));
            StringAssert.Contains(span.StatusMessage, "missing");
            Assert.AreEqual("ExecuteSqlRaw", span.Attributes.GetString("efcore.command.source"));
        }
    }

    [TestMethod]
    public void WithoutAListener_NothingIsStarted()
    {
        Assert.IsFalse(BelemetryDbCommandInterceptor.HasListeners);
        using var db = new Database(o => o.CaptureQueryText = true);
        var context = db.Context;

        context.Blogs.Add(new Blog { Title = "quiet" });
        context.SaveChanges();
        _ = context.Blogs.ToList();
        Assert.IsNull(Activity.Current);
    }

    [TestMethod]
    public void DbSystem_FollowsTheConnectionType()
    {
        Assert.AreEqual("sqlite", BelemetryDbCommandInterceptor.GetDbSystem(new SqliteConnection()));
        Assert.AreEqual("other_sql", BelemetryDbCommandInterceptor.GetDbSystem(null));
    }

    [TestMethod]
    public void ServerAddress_IsParsedFromTheDataSource()
    {
        Assert.AreEqual(("sql.shop.test", 1433), BelemetryDbCommandInterceptor.ParseServer("tcp:sql.shop.test,1433"));
        Assert.AreEqual(("sql.shop.test", 1433), BelemetryDbCommandInterceptor.ParseServer("sql.shop.test:1433"));
        Assert.AreEqual(("sql.shop.test", (int?)null), BelemetryDbCommandInterceptor.ParseServer("sql.shop.test"));
        Assert.AreEqual(("sql.shop.test", (int?)null), BelemetryDbCommandInterceptor.ParseServer("sql.shop.test\\SQLEXPRESS"));
        Assert.AreEqual(("localhost", 5432), BelemetryDbCommandInterceptor.ParseServer("tcp://localhost:5432"));
        Assert.AreEqual(((string?)null, (int?)null), BelemetryDbCommandInterceptor.ParseServer("np:\\\\.\\pipe\\sql\\query"));
        Assert.AreEqual(((string?)null, (int?)null), BelemetryDbCommandInterceptor.ParseServer("/var/lib/app/app.db"));
        Assert.AreEqual(((string?)null, (int?)null), BelemetryDbCommandInterceptor.ParseServer(""));
    }
}
