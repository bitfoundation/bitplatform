using Bit.CodeAnalyzers.SystemAnalyzers;
using static Bit.CodeAnalyzers.Tests.AnalyzerTestHarness;

namespace Bit.CodeAnalyzers.Tests;

[TestClass]
public class NonAsyncEFCoreMethodsUsageAnalyzerTests
{
    private const string Model = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Microsoft.EntityFrameworkCore;

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public DateTimeOffset ExpiresOn { get; set; }
            public User? Manager { get; set; }
            public List<Order> Orders { get; set; } = [];
        }

        public class Order
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public decimal Total { get; set; }
        }

        public class AppDbContext : DbContext
        {
            public DbSet<User> Users => Set<User>();
            public DbSet<Order> Orders => Set<Order>();
        }
        """;

    private static Task<string[]> Analyze(string source)
    {
        return GetDiagnostics(new NonAsyncEFCoreMethodsUsageAnalyzer(), [("Model.cs", Model), ("Test.cs", source)]);
    }

    private static string[] Reported(params string[] methodNames)
    {
        return [.. methodNames.Select(methodName => $"NonAsyncEFCoreMethodsUsageAnalyzer: {methodName}")];
    }

    [TestMethod]
    public async Task ReportsSyncQueryMethodsOnTheMethodName()
    {
        var diagnostics = await Analyze("""
            public class Service(AppDbContext db)
            {
                public void Run(User user)
                {
                    db.Users.ToList();
                    db.Users.Where(u => u.Id > 1)
                        .Select(u => u.Name)
                        .ToArray();
                    db.Users.ToDictionary(u => u.Id);
                    db.Users.Any();
                    db.Users.All(u => u.Id > 0);
                    db.Users.Max(u => u.Id);
                    db.Users.Min(u => u.Id);
                    db.Users.Average(u => u.Id);
                    db.Users.Sum(u => u.Id);
                    db.Users.Contains(user);
                    db.Users.LongCount();
                    db.Users.Count();
                    db.Users.FirstOrDefault();
                    db.Users.First();
                    db.Users.Single();
                    db.Users.SingleOrDefault();
                    db.Users.Last();
                    db.Users.LastOrDefault();
                    db.Users.ElementAt(0);
                    db.Users.ElementAtOrDefault(0);
                    db.Users.Load();
                    db.Users.ExecuteDelete();
                    db.Users.ExecuteUpdate(setters => setters.SetProperty(u => u.Name, "name"));
                    db.Users.Add(user);
                    db.Users.AddRange(user);
                    db.Users.Find(1);
                }
            }
            """);

        AssertDiagnostics(diagnostics, Reported("ToList", "ToArray", "ToDictionary", "Any", "All", "Max", "Min", "Average", "Sum", "Contains",
            "LongCount", "Count", "FirstOrDefault", "First", "Single", "SingleOrDefault", "Last", "LastOrDefault", "ElementAt", "ElementAtOrDefault",
            "Load", "ExecuteDelete", "ExecuteUpdate", "Add", "AddRange", "Find"));
    }

    [TestMethod]
    public async Task ReportsSyncMethodsOfTheContextDatabaseEntriesAndFactory()
    {
        var diagnostics = await Analyze("""
            public class Service(AppDbContext db, IDbContextFactory<AppDbContext> factory)
            {
                public void Run(User user)
                {
                    db.SaveChanges();
                    db.Find<User>(1);
                    db.Add(user);
                    db.AddRange(user);
                    db.Database.EnsureCreated();
                    db.Database.EnsureDeleted();
                    db.Database.CanConnect();
                    db.Database.BeginTransaction();
                    db.Database.CommitTransaction();
                    db.Database.RollbackTransaction();
                    db.Entry(user).GetDatabaseValues();
                    db.Entry(user).Reload();
                    db.Entry(user).Reference(u => u.Manager).Load();
                    db.Entry(user).Collection(u => u.Orders).Load();
                    factory.CreateDbContext();
                }
            }
            """);

        AssertDiagnostics(diagnostics, Reported("SaveChanges", "Find<User>", "Add", "AddRange", "EnsureCreated", "EnsureDeleted", "CanConnect",
            "BeginTransaction", "CommitTransaction", "RollbackTransaction", "GetDatabaseValues", "Reload", "Load", "Load", "CreateDbContext"));
    }

    [TestMethod]
    public async Task ReportsOrderedAndIncludableQueries()
    {
        var diagnostics = await Analyze("""
            public class Service(AppDbContext db)
            {
                public void Run()
                {
                    db.Users.OrderBy(u => u.Name).ToList();
                    db.Users.Include(u => u.Orders).ToList();
                    db.Users.Include(u => u.Orders).First();
                }
            }
            """);

        AssertDiagnostics(diagnostics, Reported("ToList", "ToList", "First"));
    }

    [TestMethod]
    public async Task ReportsCallsWithoutAnExplicitReceiver()
    {
        var diagnostics = await Analyze("""
            public class AuditDbContext : AppDbContext
            {
                public void Log(Order order)
                {
                    Add(order);
                    SaveChanges();
                }
            }

            public class Service(AppDbContext? db)
            {
                public void Run() => db?.SaveChanges();
            }

            public class Repository<TContext>(TContext db)
                where TContext : DbContext
            {
                public void Save() => db.SaveChanges();
            }
            """);

        AssertDiagnostics(diagnostics, Reported("Add", "SaveChanges", "SaveChanges", "SaveChanges"));
    }

    [TestMethod]
    public async Task ReportsInsideLambdasThatAreNotExpressionTrees()
    {
        var diagnostics = await Analyze("""
            public class Service(AppDbContext db)
            {
                public int[] Run(int[] ids)
                {
                    Func<AppDbContext, int> count = context => context.Users.Count();
                    return ids.Select(id => db.Orders.Count(o => o.UserId == id)).ToArray();
                }
            }
            """);

        AssertDiagnostics(diagnostics, Reported("Count", "Count"));
    }

    [TestMethod]
    public async Task IgnoresQueriesTranslatedInsideExpressionTrees()
    {
        var diagnostics = await Analyze("""
            public class Service(AppDbContext db)
            {
                public async Task Run()
                {
                    await db.Users.Where(u => db.Orders.Any(o => o.UserId == u.Id)).ToListAsync();
                    await db.Users.Select(u => new { u.Id, Count = db.Orders.Count(o => o.UserId == u.Id) }).ToListAsync();
                    await db.Users.Where(u => u.Orders.Any(o => db.Orders.Count() > 1)).ToListAsync();
                    await (from u in db.Users where db.Orders.Any(o => o.UserId == u.Id) select u).ToListAsync();
                    await db.Users.ExecuteUpdateAsync(setters => setters.SetProperty(u => u.Name, u => db.Users.First().Name));
                }
            }
            """);

        AssertDiagnostics(diagnostics);
    }

    [TestMethod]
    public async Task IgnoresSameNamedMethodsInTheArgumentsOfAsyncCalls()
    {
        var diagnostics = await Analyze("""
            public class Service(AppDbContext db, TimeProvider timeProvider)
            {
                public async Task Run(List<int> list, int pageSize)
                {
                    var now = timeProvider.GetUtcNow();

                    await db.Users.AddAsync(new User { ExpiresOn = now.Add(TimeSpan.FromDays(1)) });
                    await db.Users.Skip(list.Count).Take(Math.Max(1, pageSize)).ToListAsync();
                    await db.Users.Where(u => list.Contains(u.Id)).FirstOrDefaultAsync();
                }
            }
            """);

        AssertDiagnostics(diagnostics);
    }

    [TestMethod]
    public async Task IgnoresAsyncMethodsAndInMemoryCollections()
    {
        var diagnostics = await Analyze("""
            public class Service(AppDbContext db, IDbContextFactory<AppDbContext> factory)
            {
                public async Task Run(User user, CancellationToken cancellationToken)
                {
                    await db.Users.ToListAsync(cancellationToken);
                    await db.Users.CountAsync(cancellationToken);
                    await db.Users.AnyAsync(cancellationToken);
                    await db.Users.FindAsync([1], cancellationToken);
                    await db.AddAsync(user, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                    await db.Database.EnsureCreatedAsync(cancellationToken);
                    await db.Entry(user).ReloadAsync(cancellationToken);
                    await using var context = await factory.CreateDbContextAsync(cancellationToken);

                    var users = await db.Users.ToListAsync(cancellationToken);
                    users.ToList();
                    users.Count();
                    users.Add(user);
                    users.Find(u => u.Id == 1);
                    users.First().Orders.Sum(o => o.Total);
                    new Dictionary<int, User>().Add(1, user);
                }
            }
            """);

        AssertDiagnostics(diagnostics);
    }

    [TestMethod]
    public async Task IgnoresProjectsWithoutEntityFrameworkCore()
    {
        var diagnostics = await GetDiagnostics(new NonAsyncEFCoreMethodsUsageAnalyzer(), """
            using System.Linq;

            public class Service
            {
                public int[] Run(int[] items) => items.AsQueryable().Where(item => item > 1).ToArray();
            }
            """, referenceEntityFrameworkCore: false);

        AssertDiagnostics(diagnostics);
    }

    [TestMethod]
    public async Task ReportsInGeneratedCodeWhereRazorPutsCodeBlocks()
    {
        var diagnostics = await Analyze("""
            // <auto-generated/>
            public partial class UsersPage(AppDbContext db)
            {
                private List<User> users = [];

                protected void OnInitialized()
                {
                    users = db.Users.ToList();
                }
            }
            """);

        AssertDiagnostics(diagnostics, Reported("ToList"));
    }
}
