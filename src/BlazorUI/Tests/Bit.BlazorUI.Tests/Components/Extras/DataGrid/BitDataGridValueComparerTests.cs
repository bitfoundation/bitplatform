using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

/// <summary>
/// Pins the comparer a sort of BitDataGrid picks for a column's keys (<c>BitDataGridValueComparer.For</c>): a direct one
/// for keys of one trusted type, which spares the type checks the general comparer makes on every call, and the general
/// one for anything else - and that the direct one orders every set of keys it is picked for exactly as the general one
/// does, nulls first. The comparer is compile-linked from the Extras source (see the test project).
/// </summary>
[TestClass]
public class BitDataGridValueComparerTests
{
    private enum Level { Low, Medium, High }

    private sealed class Opaque
    {
    }

    private sealed class Custom(int value) : IComparable
    {
        public int CompareTo(object? obj) => value.CompareTo(((Custom)obj!).Value);

        public int Value => value;
    }

    private static readonly IComparer<object?> General = BitDataGridValueComparer.Instance;

    // 400 keys of one trusted type, every ninth one null, with repeats among them; the same ones on every run.
    private static object?[] KeysOf(string type)
    {
        var random = new Random(15_191);
        Func<object> next = type switch
        {
            "decimal" => () => (decimal)random.Next(-500, 500) / 100,
            "int" => () => random.Next(-50, 50),
            "long" => () => (long)random.Next(-50, 50) * 1_000_000_000L,
            "double" => () => random.Next(0, 6) switch { 0 => double.NaN, 1 => double.NegativeInfinity, 2 => -0.0, _ => random.NextDouble() * 10 - 5 },
            "bool" => () => random.Next(2) == 0,
            "DateTime" => () => new DateTime(2026, 1, 1).AddMinutes(random.Next(0, 5_000)),
            "DateTimeOffset" => () => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(random.Next(-3, 4))).AddMinutes(random.Next(0, 500)),
            "DateOnly" => () => new DateOnly(2026, 1, 1).AddDays(random.Next(0, 60)),
            "TimeSpan" => () => TimeSpan.FromSeconds(random.Next(-100, 100)),
            "Guid" => () => new Guid(random.Next(0, 20), 0, 0, new byte[8]),
            "enum" => () => (Level)random.Next(0, 3),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
        return Enumerable.Range(0, 400).Select(i => i % 9 == 0 ? null : next()).ToArray();
    }

    [TestMethod]
    [DataRow("decimal")]
    [DataRow("int")]
    [DataRow("long")]
    [DataRow("double")]
    [DataRow("bool")]
    [DataRow("DateTime")]
    [DataRow("DateTimeOffset")]
    [DataRow("DateOnly")]
    [DataRow("TimeSpan")]
    [DataRow("Guid")]
    [DataRow("enum")]
    public void TheComparerPickedForKeysOfOneTrustedTypeOrdersThemAsTheGeneralOneDoes(string type)
    {
        var keys = KeysOf(type);
        var picked = BitDataGridValueComparer.For(keys);
        Assert.AreNotSame(General, picked, $"{type}: keys of one trusted type get the direct comparer");

        // Sorted stably by both, the keys come out in the same order, nulls first.
        var byPicked = keys.Select((key, index) => (key, index)).OrderBy(e => e.key, picked).Select(e => e.index).ToArray();
        var byGeneral = keys.Select((key, index) => (key, index)).OrderBy(e => e.key, General).Select(e => e.index).ToArray();
        CollectionAssert.AreEqual(byGeneral, byPicked, type);
        Assert.IsNull(keys[byPicked[0]], $"{type}: a null key comes first");

        // And every pair compares the same way, not just the order a sort happens to visit them in.
        foreach (var x in keys.Take(60))
        {
            foreach (var y in keys.Take(60))
            {
                Assert.AreEqual(Math.Sign(General.Compare(x, y)), Math.Sign(picked.Compare(x, y)), $"{type}: {x ?? "null"} vs {y ?? "null"}");
            }
        }
    }

    [TestMethod]
    public void AnyOtherSetOfKeysGetsTheGeneralComparer()
    {
        // Strings: the general comparer orders them ordinally ignoring case, which a string's own CompareTo would not.
        Assert.AreSame(General, BitDataGridValueComparer.For(["b", null, "A"]));
        // Two trusted types side by side: the general comparer orders the types apart first.
        Assert.AreSame(General, BitDataGridValueComparer.For([1, 2L]));
        Assert.AreSame(General, BitDataGridValueComparer.For([1, "1"]));
        // A comparable the comparer does not trust, and a type with no order of its own.
        Assert.AreSame(General, BitDataGridValueComparer.For([new Custom(2), new Custom(1)]));
        Assert.AreSame(General, BitDataGridValueComparer.For([new Opaque(), new Opaque()]));
        // Nothing to tell a type by.
        Assert.AreSame(General, BitDataGridValueComparer.For([null, null]));
        Assert.AreSame(General, BitDataGridValueComparer.For([]));
    }

    [TestMethod]
    public void TheTypeOfTheFirstKeyDoesNotDecideAlone()
    {
        // A column whose keys turn to another type after the first ones still gets the general comparer.
        var keys = Enumerable.Range(0, 100).Select(i => (object?)i).Append(100L).ToArray();

        Assert.AreSame(General, BitDataGridValueComparer.For(keys));
    }
}
