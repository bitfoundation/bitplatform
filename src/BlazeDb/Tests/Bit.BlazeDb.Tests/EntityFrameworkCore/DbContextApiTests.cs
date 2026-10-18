using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

[TestClass]
public class DbContextApiTests
{
    private static DbContextOptions<PeopleContext> IsolatedOptions() =>
        new DbContextOptionsBuilder<PeopleContext>()
            .UseBlazeDb("test-" + Guid.NewGuid().ToString("N"))
            .Options;

    [TestMethod]
    public void Engine_Settings_Keep_A_Store_Name_Chosen_Before_Them()
    {
        static string? NameOf(DbContextOptions options) =>
            options.FindExtension<Bit.BlazeDb.EntityFrameworkCore.Infrastructure.BlazeDbOptionsExtension>()!.DatabaseName;

        var named = new DbContextOptionsBuilder<PeopleContext>()
            .UseBlazeDb("shared")
            .UseBlazeDb(o => o.FlushInterval = TimeSpan.FromSeconds(1))
            .Options;
        Assert.AreEqual("shared", NameOf(named));

        var defaulted = new DbContextOptionsBuilder<PeopleContext>()
            .UseBlazeDb(o => o.FlushInterval = TimeSpan.FromSeconds(1))
            .Options;
        Assert.AreEqual(typeof(PeopleContext).FullName, NameOf(defaulted));
    }

    [TestMethod]
    public void AddDbContext_Resolves_A_Working_Context()
    {
        var name = "di-" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddDbContext<PeopleContext>(options => options.UseBlazeDb(name));
        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PeopleContext>();
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            Assert.AreEqual(1, context.SaveChanges());
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PeopleContext>();
            Assert.AreEqual("Ada", context.People.Single().Name);
        }
    }

    [TestMethod]
    public void OnConfiguring_Is_Enough_To_Construct_A_Context()
    {
        var name = "on-config-" + Guid.NewGuid().ToString("N");
        using (var context = new OnConfiguringContext(name))
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
        }

        using var fresh = new OnConfiguringContext(name);
        Assert.AreEqual("Ada", fresh.People.Single().Name);
    }

    [TestMethod]
    public void Contexts_From_The_Same_Options_Share_The_Store()
    {
        var options = IsolatedOptions();

        using (var context = new PeopleContext(options))
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
        }

        using var next = new PeopleContext(options);
        Assert.AreEqual(1, next.People.Count());
    }

    [TestMethod]
    public void Separate_Store_Names_Isolate_Data()
    {
        using var first = new PeopleContext(IsolatedOptions());
        using var second = new PeopleContext(IsolatedOptions());

        first.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        first.SaveChanges();

        Assert.IsEmpty(second.People);
    }

    [TestMethod]
    public void A_Named_Store_Is_Shared_Across_Builders()
    {
        var name = "named-" + Guid.NewGuid().ToString("N");
        using var first = new PeopleContext(new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(name).Options);
        using var second = new PeopleContext(new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(name).Options);

        first.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        first.SaveChanges();

        Assert.AreEqual("Ada", second.People.Single().Name);
    }

    [TestMethod]
    public void BeginTransaction_Spans_Several_Saves()
    {
        using var context = new PeopleContext(IsolatedOptions());

        using (var transaction = context.Database.BeginTransaction())
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
            context.Orders.Add(new Order { Id = 10, PersonId = 1, Total = 4m });
            context.SaveChanges();
            transaction.Rollback();
        }

        Assert.IsEmpty(context.People);
        Assert.IsEmpty(context.Orders);
    }

    [TestMethod]
    public void Tables_Come_From_The_Model_Not_AddTable()
    {
        using var context = new PeopleContext(IsolatedOptions());

        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        context.Tags.Add(new Tag { Slug = "wal", Kind = TagKind.Topic, Weight = 3 });
        context.SaveChanges();

        Assert.AreEqual("Ada", context.People.Find(1)!.Name);
        Assert.AreEqual("wal", context.Tags.Find("wal")!.Slug);
    }

    [TestMethod]
    public void AddDbContextFactory_Creates_Contexts_Over_The_Same_Store()
    {
        var name = "factory-" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddDbContextFactory<PeopleContext>(options => options.UseBlazeDb(name));
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<PeopleContext>>();

        using (var context = factory.CreateDbContext())
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
        }

        using var next = factory.CreateDbContext();
        Assert.AreEqual("Ada", next.People.Single().Name);
    }

    [TestMethod]
    public void Parameterless_UseBlazeDb_Shares_The_Context_Type_Store()
    {
        var options = new DbContextOptionsBuilder<DefaultStoreContext>().UseBlazeDb().Options;

        using (var context = new DefaultStoreContext(options))
        {
            context.Database.EnsureDeleted();
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
        }

        using var next = new DefaultStoreContext(options);
        Assert.AreEqual("Ada", next.People.Single().Name);
        next.Database.EnsureDeleted();
    }

    [TestMethod]
    public void Storage_Instance_Is_The_Store_Identity()
    {
        var storage = new Bit.BlazeDb.Storage.BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;

        using (var context = new PeopleContext(options))
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
        }

        using var next = new PeopleContext(options);
        Assert.AreEqual("Ada", next.People.Single().Name);
        next.Database.EnsureDeleted();
    }

    [TestMethod]
    public void EnsureDeleted_Clears_The_Store_For_Later_Contexts()
    {
        var options = IsolatedOptions();

        using (var context = new PeopleContext(options))
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
            Assert.IsTrue(context.Database.EnsureDeleted());
        }

        using var fresh = new PeopleContext(options);
        Assert.IsEmpty(fresh.People);
        Assert.IsTrue(fresh.Database.CanConnect());
    }

    [TestMethod]
    public void GetBlazeDb_Returns_The_Engine_The_Context_Is_Using()
    {
        using var context = new PeopleContext(IsolatedOptions());
        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        context.SaveChanges();

        var engine = context.Database.GetBlazeDb();
        Assert.AreEqual("Ada", engine.GetTable(Person.Table).Get(1)!.Name);
    }

    [TestMethod]
    public async Task An_Existing_Engine_Can_Be_Passed_In()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions());
        using var context = new PeopleContext(
            new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(db).Options);

        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        context.SaveChanges();

        Assert.AreEqual("Ada", db.GetTable(Person.Table).Get(1)!.Name);
        Assert.IsFalse(context.Database.EnsureDeleted());
    }

    private sealed class OnConfiguringContext : DbContext
    {
        private readonly string _name;

        public OnConfiguringContext(string name) => _name = name;

        public DbSet<Person> People => Set<Person>();

        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseBlazeDb(_name);
    }

    private sealed class DefaultStoreContext : DbContext
    {
        public DefaultStoreContext(DbContextOptions<DefaultStoreContext> options) : base(options)
        {
        }

        public DbSet<Person> People => Set<Person>();
    }


    [TestMethod]
    public void The_Default_Store_Is_Named_After_The_Context_Type_Through_Every_Builder()
    {
        // AddDbContext and OnConfiguring hand out the non-generic builder, and AddDbContext builds
        // its options once per scope: the name has to come from the options' context type, not
        // from the overload or from a value made up per call.
        var services = new ServiceCollection();
        services.AddDbContext<DefaultStoreContext>(options => options.UseBlazeDb());
        services.AddDbContext<OtherDefaultStoreContext>(options => options.UseBlazeDb(engine => engine.FlushInterval = TimeSpan.FromHours(1)));
        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DefaultStoreContext>().Database.EnsureDeleted();
            scope.ServiceProvider.GetRequiredService<OtherDefaultStoreContext>().Database.EnsureDeleted();
        }

        using (var scope = provider.CreateScope())
        {
            var first = scope.ServiceProvider.GetRequiredService<DefaultStoreContext>();
            first.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            first.SaveChanges();
            var other = scope.ServiceProvider.GetRequiredService<OtherDefaultStoreContext>();
            other.People.Add(new Person { Id = 2, Name = "Grace", City = "NYC", Age = 45 });
            other.SaveChanges();
        }

        using (var scope = provider.CreateScope())
        {
            // The same type, a later scope: the same store. The generic builder agrees on the name.
            Assert.AreEqual("Ada", scope.ServiceProvider.GetRequiredService<DefaultStoreContext>().People.Single().Name);
            using var generic = new DefaultStoreContext(new DbContextOptionsBuilder<DefaultStoreContext>().UseBlazeDb().Options);
            Assert.AreEqual("Ada", generic.People.Single().Name);

            // A different type with the same default is a different store.
            var other = scope.ServiceProvider.GetRequiredService<OtherDefaultStoreContext>();
            Assert.AreEqual("Grace", other.People.Single().Name);
            other.Database.EnsureDeleted();
            generic.Database.EnsureDeleted();
        }
    }

    private sealed class OtherDefaultStoreContext : DbContext
    {
        public OtherDefaultStoreContext(DbContextOptions<OtherDefaultStoreContext> options) : base(options)
        {
        }

        public DbSet<Person> People => Set<Person>();
    }
}
