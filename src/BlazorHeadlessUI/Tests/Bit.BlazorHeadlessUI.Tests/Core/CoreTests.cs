namespace Bit.BlazorHeadlessUI.Tests.Core;

[TestClass]
public class FilterTests
{
    [TestMethod]
    [DataRow("Banana", "nan", true)]
    [DataRow("Banana", "NAN", true)]
    [DataRow("Banana", "x", false)]
    [DataRow("Banana", "", true)]
    [DataRow(null, "a", false)]
    public void Contains_ignores_case(string? text, string query, bool expected)
        => Assert.AreEqual(expected, BitBhFilter.Contains(text, query));

    [TestMethod]
    public void StartsWith_and_AllWords()
    {
        Assert.IsTrue(BitBhFilter.StartsWith("Apple pie", "app"));
        Assert.IsFalse(BitBhFilter.StartsWith("Apple pie", "pie"));
        Assert.IsTrue(BitBhFilter.AllWords("Apple pie with cream", "cream apple"));
        Assert.IsFalse(BitBhFilter.AllWords("Apple pie", "apple tart"));
    }

    [TestMethod]
    public void Score_ranks_exact_then_prefix_then_fuzzy()
    {
        var exact = BitBhFilter.Score("Settings", "settings");
        var prefix = BitBhFilter.Score("Settings", "set");
        var inside = BitBhFilter.Score("Reset settings", "set");
        var fuzzy = BitBhFilter.Score("Open settings", "ost");
        var none = BitBhFilter.Score("Profile", "xyz");

        Assert.AreEqual(1, exact);
        Assert.IsTrue(prefix > inside, $"{prefix} > {inside}");
        Assert.IsTrue(inside > 0);
        Assert.IsTrue(fuzzy > 0);
        Assert.AreEqual(0, none);
        Assert.AreEqual(1, BitBhFilter.Score("anything", " "));
    }

    [TestMethod]
    public void Keywords_match_with_a_small_penalty()
    {
        var byKeyword = BitBhFilter.Score("Search emoji", "smiley", ["smiley"]);
        Assert.IsTrue(byKeyword is > 0 and < 1);
    }

    [TestMethod]
    public void Fuzzy_filters_and_sorts()
    {
        var result = BitBhFilter.Fuzzy(["Profile", "Billing", "Billing history", "Bill Murray"], "bill", s => s).ToArray();
        Assert.AreEqual("Billing", result[0]);
        Assert.DoesNotContain("Profile", result);
    }
}

[TestClass]
public class TypeaheadTests
{
    private static readonly string[] Items = ["Apple", "Apricot", "Banana", "Blueberry", "Cherry"];

    [TestMethod]
    public void Single_characters_cycle_through_matches()
    {
        var t = new BitBhTypeahead();
        var disabled = (int _) => false;
        Assert.AreEqual(2, t.SearchIndex(Items.Length, "b", 0, i => Items[i], disabled));
        t.Reset();
        Assert.AreEqual(3, t.SearchIndex(Items.Length, "b", 2, i => Items[i], disabled));
        // "bb" cycles like pressing "b" twice.
        Assert.AreEqual(2, t.SearchIndex(Items.Length, "b", 3, i => Items[i], disabled));
    }

    [TestMethod]
    public void Multiple_characters_match_a_prefix_and_skip_disabled_items()
    {
        var t = new BitBhTypeahead();
        // Apple is disabled: "a" lands on Apricot.
        Assert.AreEqual(1, t.SearchIndex(Items.Length, "a", -1, i => Items[i], i => i == 0));
        t.Reset();
        Assert.AreEqual(0, t.SearchIndex(Items.Length, "a", -1, i => Items[i], _ => false));
        // A longer search may stay on the current item, then moves on as it narrows.
        Assert.AreEqual(0, t.SearchIndex(Items.Length, "p", 0, i => Items[i], _ => false));
        Assert.AreEqual(1, t.SearchIndex(Items.Length, "r", 0, i => Items[i], _ => false));
        Assert.AreEqual("apr", t.Buffer);
        Assert.IsTrue(t.IsActive);
    }

    [TestMethod]
    public void Buffer_expires()
    {
        var t = new BitBhTypeahead { Timeout = TimeSpan.Zero };
        t.SearchIndex(Items.Length, "c", -1, i => Items[i], _ => false);
        Thread.Sleep(5);
        Assert.AreEqual(string.Empty, t.Buffer);
        Assert.IsFalse(t.IsActive);
    }

    [TestMethod]
    [DataRow("a", false, false, true)]
    [DataRow(" ", false, false, false)] // Space only extends an active search (handled by the caller)
    [DataRow("ArrowDown", false, false, false)]
    [DataRow("a", true, false, false)]
    [DataRow("a", false, true, false)]
    public void Typeahead_keys(string key, bool ctrl, bool alt, bool expected)
        => Assert.AreEqual(expected, BitBhTypeahead.IsTypeaheadKey(key, ctrl, alt));
}
