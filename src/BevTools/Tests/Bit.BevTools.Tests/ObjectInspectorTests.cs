
namespace Bit.BevTools.Tests;

[TestClass]
public class ObjectInspectorTests
{
    private static BevObjectInspector Create(Action<BitBevToolsOptions>? configure = null)
    {
        var options = new BitBevToolsOptions();
        configure?.Invoke(options);
        return new BevObjectInspector(options, new BevRedactor(options.SensitiveNamePatterns));
    }

    private sealed class Node
    {
        public string Name { get; set; } = "root";

        public Node? Next { get; set; }

        public string Password { get; set; } = "hunter2";

        [BevToolsSensitive]
        public string Handle { get; set; } = "secret-handle";

        public List<int> Numbers { get; } = [1, 2, 3];

        public Dictionary<string, string> Map { get; } = new() { ["a"] = "x" };

        public string Throws => throw new InvalidOperationException("nope");

        public Action Callback { get; set; } = () => { };
    }

    [TestMethod]
    public void Redacts_by_name_and_by_attribute()
    {
        var inspector = Create();
        var children = inspector.Children(new Node(), "");
        Assert.AreEqual(BevRedactor.RedactedValue, children.Single(c => c.Name == "Password").Display);
        Assert.AreEqual(BevNodeKind.Redacted, children.Single(c => c.Name == "Handle").Kind);
        Assert.AreEqual("\"root\"", children.Single(c => c.Name == "Name").Display);
    }

    [TestMethod]
    public void Detects_cycles_along_the_expansion_path()
    {
        var inspector = Create();
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b", Next = a };
        a.Next = b;

        var level1 = inspector.Children(a, "Next");
        var next = level1.Single(c => c.Name == "Next");
        Assert.AreEqual(BevNodeKind.Circular, next.Kind);
        Assert.IsFalse(next.IsExpandable);
    }

    [TestMethod]
    public void Throwing_getters_are_reported_not_propagated()
    {
        var inspector = Create();
        var throws = inspector.Children(new Node(), "").Single(c => c.Name == "Throws");
        Assert.AreEqual(BevNodeKind.Error, throws.Kind);
        Assert.Contains("InvalidOperationException", throws.Display);
    }

    [TestMethod]
    public void Collections_and_dictionaries_are_lazy_and_capped()
    {
        var inspector = Create(o => o.MaxCollectionItems = 2);
        var root = inspector.Describe("numbers", new List<int> { 1, 2, 3, 4 });
        Assert.AreEqual(BevNodeKind.Collection, root.Kind);
        Assert.AreEqual(4, root.Count);
        var children = inspector.Children(new List<int> { 1, 2, 3, 4 }, "");
        Assert.AreEqual(3, children.Count); // 2 items + "…"
        Assert.AreEqual("[1]", children[1].Name);

        var dict = inspector.Children(new Node(), "Map");
        Assert.ContainsSingle(dict);
        Assert.AreEqual("a", dict[0].Name);
        Assert.AreEqual("Map[a]", dict[0].Path);
    }

    [TestMethod]
    public void Special_values_are_summarized_not_traversed()
    {
        var inspector = Create();
        var children = inspector.Children(new Node(), "");
        var callback = children.Single(c => c.Name == "Callback");
        Assert.AreEqual(BevNodeKind.Special, callback.Kind);
        Assert.StartsWith("delegate", callback.Display);
        Assert.IsFalse(callback.IsExpandable);
    }

    [TestMethod]
    public void Long_strings_are_truncated()
    {
        var inspector = Create(o => o.MaxStringLength = 20);
        var node = inspector.Describe("s", new string('x', 100));
        Assert.Contains("+80 chars", node.Display);
    }

    [TestMethod]
    public void Flatten_and_diff_report_changed_added_and_removed_paths()
    {
        var inspector = Create();
        var before = new Node { Name = "one" };
        var flatBefore = inspector.Flatten(before);
        before.Name = "two";
        before.Numbers.Add(4);
        var flatAfter = inspector.Flatten(before);

        var diff = BevObjectInspector.Diff(flatBefore, flatAfter);
        Assert.Contains(d => d.Path == "Name" && d.Before == "\"one\"" && d.After == "\"two\"", diff);
        Assert.Contains(d => d.Path == "Numbers[3]" && d.Kind == "added", diff);
        Assert.DoesNotContain(d => d.Path == "Numbers", diff); // parent collapsed in favour of the specific child change
        Assert.AreEqual(BevRedactor.RedactedValue, flatAfter["Password"]);
    }

    /// <summary>
    /// Parameter capture walks every parameter of a component on every captured render, so the walks share one
    /// budget. A spent budget still writes each value's own entry: a parameter missing from the snapshot would be
    /// reported as removed and then added, instead of simply compared at the top level.
    /// </summary>
    [TestMethod]
    public void Named_flatten_shares_the_budget_and_always_writes_the_root_entry()
    {
        var inspector = Create();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        inspector.FlattenNamed(result, "First", new Node(), member: null, maxDepth: 2, maxItems: 25, maxEntries: 150, BevObjectInspector.FlattenDeadline());
        Assert.AreEqual("\"root\"", result["First.Name"]);
        Assert.AreEqual(BevRedactor.RedactedValue, result["First.Password"]);
        Assert.Contains("First.Numbers[2]", result.Keys);
        var walked = result.Count;

        // Spent wall-clock budget: the root entry is written, nothing below it is read (Throws would be).
        inspector.FlattenNamed(result, "Second", new Node(), member: null, maxDepth: 2, maxItems: 25, maxEntries: 150, deadline: 0);
        Assert.AreEqual(walked + 1, result.Count);
        Assert.Contains("Second", result.Keys);

        // The entry cap counts the whole dictionary, so it is the caller's running total that bounds the walk.
        inspector.FlattenNamed(result, "Third", new Node(), member: null, maxDepth: 2, maxItems: 25, maxEntries: result.Count + 2, BevObjectInspector.FlattenDeadline());
        Assert.AreEqual(walked + 1 + 2, result.Count);
    }

    [TestMethod]
    public void Max_depth_is_enforced()
    {
        var inspector = Create(o => o.MaxInspectionDepth = 2);
        var chain = new Node { Next = new Node { Next = new Node { Next = new Node() } } };
        var deep = inspector.Children(chain, "Next.Next");
        Assert.AreEqual(BevNodeKind.Special, deep.Single(c => c.Name == "Next").Kind);
    }
}
