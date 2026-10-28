using Microsoft.JSInterop;

namespace Bit.Bheadless.Tests.Core;

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

[TestClass]
public class CollectionTests
{
    private sealed class Item(string id) : IBitBhCollectionItem
    {
        public string ItemId => id;
        public bool IsItemDisabled => false;
        public string? ItemText => null;
    }

    /// <summary>A JS runtime whose module answers <c>collection.order</c> with what the test put on the "page".</summary>
    private sealed class Page : IJSRuntime, IJSObjectReference
    {
        public string[][] Order { get; set; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => identifier switch
            {
                "import" => ValueTask.FromResult((TValue)(object)this),
                "collection.order" => ValueTask.FromResult((TValue)(object)Order),
                _ => ValueTask.FromResult(default(TValue)!),
            };

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [TestMethod]
    public async Task Rendered_texts_of_items_that_left_are_dropped()
    {
        var page = new Page();
        var interop = new BitBhInterop(page);
        var collection = new BitBhCollection<Item>("options");
        Item a = new("a"), b = new("b"), c = new("c");

        collection.Register(a);
        collection.Register(b);
        page.Order = [["a", "Apple"], ["b", "Banana"]];
        await collection.EnsureOrderAsync(interop);
        Assert.AreEqual("Apple", collection.GetRenderedText("a"));

        // A combobox re-filtered on every keystroke replaces its options: the texts of the old ones go with them.
        collection.Unregister(a);
        Assert.IsNull(collection.GetRenderedText("a"));

        collection.Register(c);
        page.Order = [["b", "Banana"], ["c", "Cherry"]];
        await collection.EnsureOrderAsync(interop);
        Assert.AreEqual("Banana", collection.GetRenderedText("b"));
        Assert.AreEqual("Cherry", collection.GetRenderedText("c"));
    }

    [TestMethod]
    public async Task A_sync_that_finds_order_and_texts_as_they_were_raises_no_change()
    {
        var page = new Page();
        var interop = new BitBhInterop(page);
        var collection = new BitBhCollection<Item>("options");
        var changes = 0;
        collection.Changed += () => changes++;

        collection.Register(new("a"));
        collection.Register(new("b"));
        page.Order = [["a", "Apple"], ["b", "Banana"]];
        await collection.EnsureOrderAsync(interop);
        Assert.AreEqual(3, changes, "two registrations and the texts the sync read");

        // A member the page does not show yet: the sync finds the order and the texts unchanged.
        collection.Register(new("c"));
        await collection.EnsureOrderAsync(interop);
        Assert.AreEqual(4, changes, "only the registration");

        // A new order is a change.
        page.Order = [["b", "Banana"], ["a", "Apple"], ["c", "Cherry"]];
        collection.Register(new("d"));
        await collection.EnsureOrderAsync(interop);
        Assert.AreEqual(6, changes, "the registration and the new order");
        Assert.AreSequenceEqual(new[] { "b", "a", "c", "d" }, collection.Items.Select(i => i.ItemId).ToArray());
    }
}
