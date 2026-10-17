using Bit.BlazeDb.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

[TestClass]
public class SaveChangesTests
{
    [TestMethod]
    public void Adding_An_Entity_Puts_A_Row_In_The_Table()
    {
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();

        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        var saved = context.SaveChanges();

        Assert.AreEqual(1, saved);
        Assert.AreEqual("Ada", test.People.Get(1)!.Name);
    }

    [TestMethod]
    public void A_Saved_Entity_Becomes_Unchanged()
    {
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();

        var person = new Person { Id = 1, Name = "Ada", City = "London", Age = 36 };
        context.People.Add(person);
        context.SaveChanges();

        Assert.AreEqual(EntityState.Unchanged, context.Entry(person).State);
    }

    [TestMethod]
    public void A_Save_Inside_An_Engine_Transaction_Joins_It()
    {
        // The engine's transactions are ambient, so a save made inside one belongs to it rather
        // than failing as a nested transaction - which is what mixing the two APIs would hit.
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();

        using (var transaction = test.Database.BeginTransaction())
        {
            test.Orders.Insert(new Order { Id = 1, PersonId = 1, Total = 5m });
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            Assert.AreEqual(1, context.SaveChanges());
            transaction.Commit();
        }

        Assert.AreEqual("Ada", test.People.Get(1)!.Name);
        Assert.AreEqual(1, test.Orders.Count);
    }

    [TestMethod]
    public void Rolling_Back_The_Engine_Transaction_Undoes_The_Save_Inside_It()
    {
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();

        using (var transaction = test.Database.BeginTransaction())
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
            Assert.AreEqual(1, test.People.Count);
            transaction.Rollback();
        }

        Assert.AreEqual(0, test.People.Count);
        Assert.IsEmpty(test.People.Lookup(Person.Indexes.City, "London"));
    }

    [TestMethod]
    public void A_Tracked_Entity_Is_The_Row_The_Table_Holds()
    {
        using var test = TestDatabase.Open();
        test.People.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });

        using var context = test.CreateContext();
        var person = context.People.Single();

        Assert.AreSame(test.People.Get(1), person);
    }

    [TestMethod]
    public void Mutating_A_Tracked_Entity_Changes_Memory_Before_SaveChanges()
    {
        using var test = TestDatabase.Open();
        test.People.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });

        using var context = test.CreateContext();
        var person = context.People.Single();
        person.Name = "Ada Lovelace";

        // The row is the entity, so memory has already moved; SaveChanges is what journals it.
        Assert.AreEqual("Ada Lovelace", test.People.Get(1)!.Name);
        Assert.AreEqual(EntityState.Modified, context.Entry(person).State);

        Assert.AreEqual(1, context.SaveChanges());
        Assert.AreEqual(EntityState.Unchanged, context.Entry(person).State);
    }

    [TestMethod]
    public void Saving_A_Modified_Entity_Moves_Its_Index_Entries()
    {
        using var test = TestDatabase.Open();
        test.People.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });

        using var context = test.CreateContext();
        context.People.Single().City = "Paris";
        context.SaveChanges();

        // The row was mutated in place, so without the original values the old index entry would
        // still claim London and the new one would be missing.
        Assert.IsEmpty(test.People.Lookup(Person.Indexes.City, "London"));
        Assert.ContainsSingle(test.People.Lookup(Person.Indexes.City, "Paris"));
    }

    [TestMethod]
    public void Deleting_An_Entity_Removes_The_Row_And_Its_Index_Entries()
    {
        using var test = TestDatabase.Open();
        test.People.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });

        using var context = test.CreateContext();
        context.People.Remove(context.People.Single());
        context.SaveChanges();

        Assert.IsNull(test.People.Get(1));
        Assert.IsEmpty(test.People.Lookup(Person.Indexes.City, "London"));
    }

    [TestMethod]
    public void A_Deleted_Entity_Stops_Being_Tracked()
    {
        using var test = TestDatabase.Open();
        test.People.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });

        using var context = test.CreateContext();
        var person = context.People.Single();
        context.People.Remove(person);
        context.SaveChanges();

        Assert.AreEqual(EntityState.Detached, context.Entry(person).State);
    }

    [TestMethod]
    public void Mutating_And_Deleting_In_One_Save_Still_Clears_The_Old_Index_Entry()
    {
        using var test = TestDatabase.Open();
        test.People.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });

        using var context = test.CreateContext();
        var person = context.People.Single();
        person.City = "Paris";
        context.People.Remove(person);
        context.SaveChanges();

        Assert.IsNull(test.People.Get(1));
        Assert.IsEmpty(test.People.Lookup(Person.Indexes.City, "London"));
        Assert.IsEmpty(test.People.Lookup(Person.Indexes.City, "Paris"));
    }

    [TestMethod]
    public void A_Save_Spanning_Two_Tables_Is_One_Transaction()
    {
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();

        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        context.Orders.Add(new Order { Id = 10, PersonId = 1, Total = 42m });

        Assert.AreEqual(2, context.SaveChanges());
        Assert.IsNotNull(test.People.Get(1));
        Assert.IsNotNull(test.Orders.Get(10));
    }

    [TestMethod]
    public void A_Failing_Save_Leaves_Every_Table_Untouched()
    {
        using var test = TestDatabase.Open();
        test.People.Insert(new Person { Id = 1, Name = "Ada", City = "London", Age = 36, Email = "ada@example.com" });

        using var context = test.CreateContext();
        context.Orders.Add(new Order { Id = 10, PersonId = 1, Total = 42m });
        context.People.Add(new Person { Id = 2, Name = "Grace", City = "NY", Age = 45, Email = "ada@example.com" });

        // Reported the way EF callers expect, with the engine's own exception underneath.
        var ex = Assert.ThrowsExactly<DbUpdateException>(() => context.SaveChanges());
        Assert.IsExactInstanceOfType<BlazeDbUniqueConstraintViolationException>(ex.InnerException);

        // The order was applied before the conflicting person, so the rollback has to undo it.
        Assert.IsNull(test.Orders.Get(10));
        Assert.IsNull(test.People.Get(2));
    }

    [TestMethod]
    public void Saving_Nothing_Costs_Nothing()
    {
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();

        Assert.AreEqual(0, context.SaveChanges());
    }

    [TestMethod]
    public async Task SaveChangesAsync_Applies_The_Same_Changes()
    {
        using var test = TestDatabase.Open();
        await using var context = test.CreateContext();

        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        Assert.AreEqual(1, await context.SaveChangesAsync());
        Assert.IsNotNull(test.People.Get(1));
    }

    [TestMethod]
    public async Task Changes_Survive_A_Reopen()
    {
        var storage = new Bit.BlazeDb.Storage.BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>()
            .UseBlazeDb(storage, o => o.FlushInterval = TimeSpan.FromHours(1))
            .Options;

        using (var context = new PeopleContext(options))
        {
            context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
            context.SaveChanges();
            await context.Database.GetBlazeDb().FlushAsync();
            await context.Database.GetService<BlazeDbEngineCache>().ReleaseAsync(storage);
        }

        using var fresh = new PeopleContext(options);
        Assert.AreEqual("Ada", fresh.People.Single().Name);
    }

    [TestMethod]
    public void Collections_And_Blobs_Round_Trip_Through_The_Context()
    {
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();

        context.Notes.Add(new Note
        {
            Id = 1,
            Bucket = null,
            Tags = ["a", "b"],
            Scores = [3, 1, 2],
            Payload = [0xDE, 0xAD],
            DueAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)),
        });
        context.SaveChanges();

        var note = context.Notes.Single(n => n.Bucket == null);
        note.Tags.Add("c");
        note.Scores = [9];
        note.Bucket = 4;
        Assert.AreEqual(1, context.SaveChanges());

        using var fresh = test.CreateContext();
        var reloaded = fresh.Notes.Single(n => n.Bucket == 4);
        Assert.AreSequenceEqual(["a", "b", "c"], reloaded.Tags);
        Assert.AreSequenceEqual([9], reloaded.Scores);
        Assert.AreSequenceEqual(new byte[] { 0xDE, 0xAD }, reloaded.Payload);
        Assert.AreEqual(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)), reloaded.DueAt);
        Assert.IsEmpty(fresh.Notes.Where(n => n.Bucket == null).ToList());
    }
}
