using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

public sealed class ModelShapesContext(DbContextOptions<ModelShapesContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Measurement> Measurements => Set<Measurement>();
}

/// <summary>
/// The provider over the table shapes the generator builds through a constructor - positional
/// records and get-only properties - and over set-typed properties, whose in-place changes the change
/// tracker has to notice.
/// </summary>
[TestClass]
public class ModelShapeTests
{
    private static DbContextOptions<ModelShapesContext> Options() =>
        new DbContextOptionsBuilder<ModelShapesContext>().UseBlazeDb("shapes-" + Guid.NewGuid().ToString("N")).Options;

    [TestMethod]
    public void A_Positional_Record_Is_Added_Queried_And_Updated()
    {
        var options = Options();
        using (var context = new ModelShapesContext(options))
        {
            context.Contacts.Add(new Contact(1, "Ada", new DateOnly(1815, 12, 10)) { Nickname = "Countess" });
            context.Contacts.Add(new Contact(2, "Alan", new DateOnly(1912, 6, 23)));
            context.SaveChanges();
        }

        using (var context = new ModelShapesContext(options))
        {
            var ada = context.Contacts.Single(c => c.Birthday < new DateOnly(1900, 1, 1));
            Assert.AreEqual("Countess", ada.Nickname);

            // A record's init-only property is changed the way EF does it: through the entry.
            context.Entry(ada).Property(c => c.Name).CurrentValue = "Augusta";
            context.SaveChanges();
        }

        using (var context = new ModelShapesContext(options))
        {
            var engine = context.Database.GetBlazeDb().GetTable(Contact.Table);
            Assert.AreEqual("Augusta", engine.Get(1)!.Name);
            CollectionAssert.AreEqual(new[] { 1 }, engine.Lookup(Contact.Indexes.Initial, "A").Where(c => c.Id == 1).Select(c => c.Id).ToArray());
            Assert.AreEqual(0, context.Database.GetBlazeDb().VerifyIntegrity().Count);
            context.Database.EnsureDeleted();
        }
    }

    [TestMethod]
    public void A_Set_Changed_In_Place_Is_Saved()
    {
        var options = Options();
        var id = Guid.NewGuid();
        using (var context = new ModelShapesContext(options))
        {
            context.Measurements.Add(new Measurement(id, new TimeOnly(9, 0), (Half)1) { Bins = [1, 2] });
            context.SaveChanges();
        }

        using (var context = new ModelShapesContext(options))
        {
            var measurement = context.Measurements.Single(m => m.Id == id);
            measurement.Bins.Add(3);
            Assert.AreEqual(1, context.SaveChanges(), "Adding to the set is a modification.");
        }

        using (var context = new ModelShapesContext(options))
        {
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, context.Measurements.Single(m => m.Id == id).Bins.ToArray());
            context.Database.EnsureDeleted();
        }
    }
}
