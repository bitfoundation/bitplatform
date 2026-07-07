using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

[TestClass]
public class BitDataGridPropertyAccessorTests
{
    private enum Fruit { Apple, Banana }

    private class Address
    {
        public string? City { get; set; }
    }

    private struct Money
    {
        public decimal Amount { get; set; }
    }

    private class Model
    {
        public int Count { get; set; }
        public int? MaybeCount { get; set; }
        public string? Name { get; set; }
        public Address? Address { get; set; }
        public Money Price { get; set; }
        public Fruit Fruit { get; set; }
        public Guid Key { get; set; }
        public DateOnly Day { get; set; }
        public int ReadOnly => 42;
    }

    [TestMethod]
    public void GetsSimpleProperty()
    {
        var accessor = BitDataGridPropertyAccessor<Model>.For("Name");
        Assert.AreEqual("x", accessor.GetValue(new Model { Name = "x" }));
    }

    [TestMethod]
    public void GetsNestedPropertyAndReturnsNullForNullIntermediate()
    {
        var accessor = BitDataGridPropertyAccessor<Model>.For("Address.City");
        Assert.AreEqual("Tehran", accessor.GetValue(new Model { Address = new Address { City = "Tehran" } }));
        Assert.IsNull(accessor.GetValue(new Model()));
    }

    [TestMethod]
    public void PathResolutionIsCaseInsensitive()
    {
        var accessor = BitDataGridPropertyAccessor<Model>.For("address.city");
        Assert.AreEqual("Oslo", accessor.GetValue(new Model { Address = new Address { City = "Oslo" } }));
    }

    [TestMethod]
    public void SetValueConvertsStringToPropertyType()
    {
        var accessor = BitDataGridPropertyAccessor<Model>.For("Count");
        var model = new Model();
        accessor.SetValue(model, "17");
        Assert.AreEqual(17, model.Count);
    }

    [TestMethod]
    public void SetValueRejectsUnparseableInputWithoutOverwriting()
    {
        var accessor = BitDataGridPropertyAccessor<Model>.For("Count");
        var model = new Model { Count = 5 };
        accessor.SetValue(model, "not-a-number");
        Assert.AreEqual(5, model.Count, "invalid input must not silently overwrite the value");
    }

    [TestMethod]
    public void TryConvertValueMapsEmptyStringToNullOnlyForNullableTargets()
    {
        var nullable = BitDataGridPropertyAccessor<Model>.For("MaybeCount");
        Assert.IsTrue(nullable.TryConvertValue(string.Empty, out var converted));
        Assert.IsNull(converted);

        var nonNullable = BitDataGridPropertyAccessor<Model>.For("Count");
        Assert.IsFalse(nonNullable.TryConvertValue(string.Empty, out _));
    }

    [TestMethod]
    public void TryConvertValueParsesEnumGuidAndDateOnly()
    {
        var fruit = BitDataGridPropertyAccessor<Model>.For("Fruit");
        Assert.IsTrue(fruit.TryConvertValue("banana", out var f));
        Assert.AreEqual(Fruit.Banana, f);

        var key = BitDataGridPropertyAccessor<Model>.For("Key");
        var guid = Guid.NewGuid();
        Assert.IsTrue(key.TryConvertValue(guid.ToString(), out var g));
        Assert.AreEqual(guid, g);

        var day = BitDataGridPropertyAccessor<Model>.For("Day");
        Assert.IsTrue(day.TryConvertValue("2026-07-05", out var d));
        Assert.AreEqual(new DateOnly(2026, 7, 5), d);
    }

    [TestMethod]
    public void NumericConversionUsesInvariantCulture()
    {
        // Run under a comma-decimal locale where "." is a group separator: CurrentCulture parsing
        // would read "10.5" as 105, so this only passes while TryConvertValue stays invariant.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var accessor = BitDataGridPropertyAccessor<Model>.For("MaybeCount");
            Assert.IsTrue(accessor.TryConvertValue("1000", out var v));
            Assert.AreEqual(1000, v);

            var amount = BitDataGridPropertyAccessor<Model>.For("Price.Amount");
            Assert.IsTrue(amount.TryConvertValue("10.5", out var a));
            Assert.AreEqual(10.5m, a);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void StructIntermediatePathIsReadOnly()
    {
        // Price is a struct: a compiled setter would write to a copy and silently drop the value,
        // so the accessor must report itself as read-only instead.
        var accessor = BitDataGridPropertyAccessor<Model>.For("Price.Amount");
        Assert.IsFalse(accessor.CanWrite);

        var model = new Model { Price = new Money { Amount = 3 } };
        Assert.AreEqual(3m, accessor.GetValue(model));
    }

    [TestMethod]
    public void ReadOnlyPropertyReportsCanWriteFalse()
    {
        var accessor = BitDataGridPropertyAccessor<Model>.For("ReadOnly");
        Assert.IsFalse(accessor.CanWrite);
    }

    [TestMethod]
    public void UnknownPathThrows()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BitDataGridPropertyAccessor<Model>.For("Nope"));
    }

    [TestMethod]
    public void PropertyLambdaIsNullSafeInQueryableOverNestedPath()
    {
        // PropertyLambda promises the getter's null handling (conditional yielding null on a null
        // intermediate), so composing it into a LINQ query — as the queryable processor does for
        // sorts/filters — must not throw for rows whose intermediate is null.
        var accessor = BitDataGridPropertyAccessor<Model>.For("Address.City");
        var lambda = (Expression<Func<Model, string?>>)accessor.PropertyLambda;

        var items = new[]
        {
            new Model { Name = "b", Address = new Address { City = "Oslo" } },
            new Model { Name = "a", Address = null },
            new Model { Name = "c", Address = new Address { City = "Bergen" } },
        }.AsQueryable();

        var sorted = items.OrderBy(lambda).Select(x => x.Name).ToList();
        CollectionAssert.AreEqual(new[] { "a", "c", "b" }, sorted, "null intermediate must sort as a null key, not throw");

        var getCity = lambda.Compile();
        Assert.AreEqual(2, items.AsEnumerable().Count(x => getCity(x) is not null));
    }

    [TestMethod]
    public void ResolvePathExtractsSimpleAndBoxedMembers()
    {
        // Name is a reference type; Count is boxed by the object-typed lambda (Convert node to peel).
        Assert.AreEqual("Name", BitDataGridPropertyAccessor<Model>.ResolvePath(x => x.Name));
        Assert.AreEqual("Count", BitDataGridPropertyAccessor<Model>.ResolvePath(x => x.Count));
    }

    [TestMethod]
    public void ResolvePathExtractsNestedChains()
    {
        Assert.AreEqual("Address.City", BitDataGridPropertyAccessor<Model>.ResolvePath(x => x.Address!.City));
        Assert.AreEqual("Price.Amount", BitDataGridPropertyAccessor<Model>.ResolvePath(x => x.Price.Amount));
    }

    [TestMethod]
    public void ResolvePathSkipsNullableValueUnwrap()
    {
        // ".Value" is Nullable<T> plumbing, not a data property; it must resolve to the same
        // path (and thus the same cached accessor) as the bare member.
        Assert.AreEqual("MaybeCount", BitDataGridPropertyAccessor<Model>.ResolvePath(x => x.MaybeCount!.Value));
    }

    [TestMethod]
    public void ResolvePathRejectsNonMemberChains()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BitDataGridPropertyAccessor<Model>.ResolvePath(x => x.Name!.ToString()));
        Assert.ThrowsExactly<ArgumentException>(() => BitDataGridPropertyAccessor<Model>.ResolvePath(x => "constant"));
    }
}
