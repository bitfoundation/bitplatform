using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

[TestClass]
public class QueryTests
{
    private static TestDatabase Seeded()
    {
        var test = TestDatabase.Open();
        var people = test.People;
        people.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36, Email = "ada@x.com" });
        people.Insert(new Person { Id = 2, Name = "Grace", City = "New York", Age = 45, Email = "grace@x.com" });
        people.Insert(new Person { Id = 3, Name = "Alan", City = "London", Age = 41, Email = "alan@x.com" });
        people.Insert(new Person { Id = 4, Name = "Edsger", City = "Austin", Age = 72, Email = "edsger@x.com" });
        people.Insert(new Person { Id = 5, Name = "Barbara", City = "London", Age = 81, Email = "barbara@x.com" });
        return test;
    }

    [TestMethod]
    public void A_Bare_Set_Returns_Every_Row()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        Assert.AreEqual(5, context.People.ToList().Count);
    }

    [TestMethod]
    public void Where_Filters()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var names = context.People.Where(p => p.City == "London").Select(p => p.Name).Order().ToList();

        Assert.AreSequenceEqual(["Ada", "Alan", "Barbara"], names);
    }

    [TestMethod]
    public void Several_Wheres_Are_Combined()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var names = context.People
            .Where(p => p.City == "London")
            .Where(p => p.Age > 40)
            .Select(p => p.Name)
            .Order()
            .ToList();

        Assert.AreSequenceEqual(["Alan", "Barbara"], names);
    }

    [TestMethod]
    public void A_Captured_Variable_Is_Read_As_A_Value()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var city = "London";
        Assert.AreEqual(3, context.People.Count(p => p.City == city));
    }

    [TestMethod]
    public void One_Query_Shape_Run_With_Different_Captured_Values_Answers_Each_Correctly()
    {
        // Residual predicates and orderings are compiled once per query shape and re-bound to the
        // values captured by each execution; a stale binding would answer the first call's question.
        using var test = Seeded();
        using var context = test.CreateContext();

        var expected = new Dictionary<string, string[]>
        {
            ["A"] = ["Ada", "Alan"],
            ["B"] = ["Barbara"],
            ["G"] = ["Grace"],
            ["Z"] = [],
        };
        foreach (var (prefix, names) in expected)
        {
            var minimumAge = prefix == "A" ? 30 : 0;
            var descending = prefix == "A";
            var query = context.People.Where(p => p.Name.StartsWith(prefix) && p.Age >= minimumAge);
            query = descending ? query.OrderByDescending(p => p.Age) : query.OrderBy(p => p.Age);
            var actual = query.Select(p => p.Name).ToList();
            Assert.AreSequenceEqual(names.Order().ToList(), actual.Order().ToList());
            if (descending)
            {
                Assert.AreSequenceEqual(["Alan", "Ada"], actual);
            }
        }
    }

    [TestMethod]
    public void Compiled_Delegates_Are_Shared_Between_Executions_Of_One_Shape()
    {
        var value = 1;
        System.Linq.Expressions.Expression<Func<Person, bool>> first = p => p.Age > value;
        value = 100;
        System.Linq.Expressions.Expression<Func<Person, bool>> second = p => p.Age > value;

        var one = (Func<Person, bool>)Bit.BlazeDb.EntityFrameworkCore.Query.BlazeDbDelegateCache.Compile(first);
        var two = (Func<Person, bool>)Bit.BlazeDb.EntityFrameworkCore.Query.BlazeDbDelegateCache.Compile(second);
        var person = new Person { Age = 50 };

        // Both lambdas read the same closure, so both see 100 - as the originals would.
        Assert.IsFalse(one(person));
        Assert.IsFalse(two(person));
        value = 10;
        Assert.IsTrue(one(person));

        // The same compiled body serves both: one method, two bindings.
        Assert.AreEqual(one.Method, two.Method);
    }

    [TestMethod]
    public void OrderBy_Then_Skip_And_Take_Pages()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var names = context.People.OrderBy(p => p.Age).Skip(1).Take(2).Select(p => p.Name).ToList();

        Assert.AreSequenceEqual(["Alan", "Grace"], names);
    }

    [TestMethod]
    public void OrderByDescending_Reverses()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        Assert.AreEqual("Barbara", context.People.OrderByDescending(p => p.Age).First().Name);
    }

    [TestMethod]
    public void ThenBy_Breaks_Ties()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var names = context.People.OrderBy(p => p.City).ThenByDescending(p => p.Age).Select(p => p.Name).ToList();

        Assert.AreSequenceEqual(["Edsger", "Barbara", "Alan", "Ada", "Grace"], names);
    }

    [TestMethod]
    public void First_And_Single_Pick_One_Row()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        Assert.AreEqual("Grace", context.People.Single(p => p.Id == 2).Name);
        Assert.AreEqual("Ada", context.People.First(p => p.City == "London").Name);
        Assert.IsNull(context.People.FirstOrDefault(p => p.City == "Paris"));
    }

    [TestMethod]
    public void Count_And_Any_Answer_Without_Materializing()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        Assert.AreEqual(3, context.People.Count(p => p.City == "London"));
        Assert.IsTrue(context.People.Any(p => p.Age > 80));
        Assert.IsFalse(context.People.Any(p => p.Age > 100));
    }

    [TestMethod]
    public void Projections_Fall_Through_To_Linq()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var summaries = context.People
            .Where(p => p.City == "London")
            .Select(p => new { p.Name, Decade = p.Age / 10 })
            .OrderBy(x => x.Name)
            .ToList();

        Assert.AreEqual(3, summaries.Count);
        Assert.AreEqual("Ada", summaries[0].Name);
        Assert.AreEqual(3, summaries[0].Decade);
    }

    [TestMethod]
    public void Operators_The_Plan_Cannot_Absorb_Still_Run()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        // GroupBy has no plan equivalent, so it runs as LINQ to Objects over the rows the plan
        // produced - which is free, because those rows are already objects in memory.
        var byCity = context.People
            .GroupBy(p => p.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderBy(x => x.City)
            .ToList();

        Assert.AreSequenceEqual(["Austin", "London", "New York"], byCity.Select(x => x.City));
        Assert.AreSequenceEqual([1, 3, 1], byCity.Select(x => x.Count));
    }

    [TestMethod]
    public void Aggregates_Work()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        Assert.AreEqual(81, context.People.Max(p => p.Age));
        Assert.AreEqual(275, context.People.Sum(p => p.Age));
    }

    [TestMethod]
    public void Take_Before_Where_Keeps_Linq_Semantics()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        // Take then Where means "of the first two rows, the London ones" - the plan must not
        // reorder that into "the first two London rows".
        var expected = test.People.Scan().Take(2).Count(p => p.City == "London");

        Assert.AreEqual(expected, context.People.Take(2).Count(p => p.City == "London"));
    }

    [TestMethod]
    public void Queried_Entities_Are_Tracked()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var person = context.People.First(p => p.Id == 1);

        Assert.AreEqual(EntityState.Unchanged, context.Entry(person).State);
    }

    [TestMethod]
    public void AsNoTracking_Leaves_The_Change_Tracker_Empty()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var people = context.People.AsNoTracking().ToList();

        Assert.AreEqual(5, people.Count);
        Assert.IsEmpty(context.ChangeTracker.Entries());
    }

    [TestMethod]
    public void A_Context_Wide_No_Tracking_Setting_Is_Honored()
    {
        using var test = Seeded();
        using var context = test.CreateContext();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;

        Assert.AreEqual(5, context.People.ToList().Count);
        Assert.IsEmpty(context.ChangeTracker.Entries());

        // An explicit AsTracking on the query still wins over the context default.
        Assert.AreEqual(5, context.People.AsTracking().ToList().Count);
        Assert.AreEqual(5, context.ChangeTracker.Entries().Count());
    }

    [TestMethod]
    public void Counting_Does_Not_Track_The_Rows_It_Reads()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        _ = context.People.Count();

        Assert.IsEmpty(context.ChangeTracker.Entries());
    }

    [TestMethod]
    public void The_Same_Row_Queried_Twice_Is_One_Tracked_Entity()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var first = context.People.Single(p => p.Id == 1);
        var second = context.People.Single(p => p.Id == 1);

        Assert.AreSame(first, second);
        Assert.ContainsSingle(e => e.Entity.Id == 1, context.ChangeTracker.Entries<Person>());
    }

    [TestMethod]
    public async Task Queries_Run_Asynchronously()
    {
        using var test = Seeded();
        await using var context = test.CreateContext();

        Assert.AreEqual(3, (await context.People.Where(p => p.City == "London").ToListAsync()).Count);
        Assert.AreEqual("Grace", (await context.People.FirstOrDefaultAsync(p => p.Id == 2))!.Name);
        Assert.AreEqual(5, await context.People.CountAsync());
    }

    [TestMethod]
    public void Include_Is_Refused_With_An_Explanation()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var error = Assert.ThrowsExactly<NotSupportedException>(() => context.People.Include("Orders").ToList());

        Assert.Contains("no navigations", error.Message);
    }


    [TestMethod]
    public void A_Subquery_Over_Another_Set_Runs_Over_That_Tables_Rows()
    {
        using var test = Seeded();
        test.Orders.Insert(new Order { Id = 10, PersonId = 1, Total = 5m });
        test.Orders.Insert(new Order { Id = 11, PersonId = 3, Total = 7m });
        using var context = test.CreateContext();

        // The inner set has no plan of its own; it is bound to its table's rows and evaluated by
        // LINQ to Objects inside the predicate, which is what a relational provider would turn
        // into a correlated subquery.
        var buyers = context.People
            .Where(p => context.Orders.Any(o => o.PersonId == p.Id))
            .Select(p => p.Name)
            .Order()
            .ToList();

        Assert.AreSequenceEqual(["Ada", "Alan"], buyers);
        Assert.AreEqual(12m, context.People.Where(p => p.City == "London").Sum(p => context.Orders.Where(o => o.PersonId == p.Id).Sum(o => o.Total)));
    }

    [TestMethod]
    public void A_Join_Between_Two_Sets_Works()
    {
        using var test = Seeded();
        test.Orders.Insert(new Order { Id = 10, PersonId = 1, Total = 5m });
        test.Orders.Insert(new Order { Id = 11, PersonId = 1, Total = 6m });
        test.Orders.Insert(new Order { Id = 12, PersonId = 2, Total = 7m });
        using var context = test.CreateContext();

        var totals = (
            from p in context.People
            join o in context.Orders on p.Id equals o.PersonId
            group o by p.Name into g
            orderby g.Key
            select new { Name = g.Key, Total = g.Sum(o => o.Total) }).ToList();

        Assert.AreSequenceEqual(["Ada", "Grace"], totals.Select(t => t.Name));
        Assert.AreSequenceEqual([11m, 7m], totals.Select(t => t.Total));

        // The query is still rooted at - and planned for - the first set.
        var alan = context.People.Where(p => p.Id == 3).SelectMany(p => context.Orders.Where(o => o.PersonId == p.Id)).ToList();
        Assert.IsEmpty(alan);
    }

    [TestMethod]
    public void A_Query_Not_Rooted_At_A_Set_Is_Refused_With_An_Explanation()
    {
        using var test = Seeded();
        using var context = test.CreateContext();

        var local = new[] { 1, 2 }.AsQueryable();
        var error = Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IQueryable)context.People).Provider.Execute<int>(local.Select(x => x + 1).Expression));

        Assert.Contains("DbSet", error.Message);
    }

    private static readonly Func<PeopleContext, int, Person?> PersonById =
        EF.CompileQuery((PeopleContext context, int id) => context.People.SingleOrDefault(p => p.Id == id));

    private static readonly Func<PeopleContext, string, IEnumerable<Person>> PeopleIn =
        EF.CompileQuery((PeopleContext context, string city) => context.People.Where(p => p.City == city).OrderBy(p => p.Id));

    private static readonly Func<PeopleContext, int, Task<Person?>> PersonByIdAsync =
        EF.CompileAsyncQuery((PeopleContext context, int id) => context.People.SingleOrDefault(p => p.Id == id));

    private static readonly Func<PeopleContext, int, IAsyncEnumerable<Person>> PeopleOlderThan =
        EF.CompileAsyncQuery((PeopleContext context, int age) => context.People.Where(p => p.Age > age));

    private static readonly Func<PeopleContext, int, int> PeopleWithOrdersAbove =
        EF.CompileQuery((PeopleContext context, int total) =>
            context.People.Count(p => context.Orders.Any(o => o.PersonId == p.Id && o.Total > total)));

    [TestMethod]
    public async Task Compiled_Queries_Bind_Each_Calls_Arguments_And_Context()
    {
        using var test = Seeded();
        test.Orders.Insert(new Order { Id = 1, PersonId = 2, Total = 50 });
        test.Orders.Insert(new Order { Id = 2, PersonId = 3, Total = 5 });
        using var context = test.CreateContext();

        Assert.AreEqual("Ada", PersonById(context, 1)!.Name);
        Assert.AreEqual("Grace", PersonById(context, 2)!.Name);
        Assert.IsNull(PersonById(context, 99));
        Assert.AreSequenceEqual([1, 3, 5], PeopleIn(context, "London").Select(p => p.Id).ToList());
        Assert.AreSequenceEqual([4], PeopleIn(context, "Austin").Select(p => p.Id).ToList());
        Assert.AreEqual("Alan", (await PersonByIdAsync(context, 3))!.Name);

        var older = new List<int>();
        await foreach (var person in PeopleOlderThan(context, 44))
        {
            older.Add(person.Id);
        }
        Assert.AreSequenceEqual([2, 4, 5], older.Order());
        Assert.AreEqual(1, PeopleWithOrdersAbove(context, 10));
        Assert.AreEqual(2, PeopleWithOrdersAbove(context, 1));

        // The delegate is reused for every context with this model; each call runs on its own.
        using var other = TestDatabase.Open();
        other.People.Insert(new Person { Id = 1, Name = "Someone else", City = "Oslo", Age = 30 });
        using var otherContext = other.CreateContext();
        Assert.AreEqual("Someone else", PersonById(otherContext, 1)!.Name);
        Assert.AreSame(otherContext.People.Local.Single(), PersonById(otherContext, 1));
        Assert.AreEqual("Ada", PersonById(context, 1)!.Name);
    }
}
