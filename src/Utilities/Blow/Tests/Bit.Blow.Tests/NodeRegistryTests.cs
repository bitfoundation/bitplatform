namespace Bit.Blow.Tests;

/// <summary>
/// Node definitions and their registry: creating nodes, the catalog search, dynamic ports and the edges they leave,
/// validation, deprecated kinds and their upgrade, and running nodes through their definitions.
/// </summary>
[TestClass]
public class NodeRegistryTests
{
    private sealed class SwitchData
    {
        public List<string> Cases { get; set; } = ["a", "b"];
    }

    private static BlowNodeRegistry Registry() => new BlowNodeRegistry().Register(
        new BlowNodeDefinition
        {
            Type = "http",
            Label = "HTTP request",
            Category = "Integrations",
            Tags = ["fetch", "api", "rest"],
            Description = "Calls a web service.",
            Width = 180,
            SourcePosition = BlowPosition.Right,
            TargetPosition = BlowPosition.Left,
            Ports =
            [
                new BlowPort { Type = BlowHandleType.Target, Id = "in", DataType = "request", MinConnections = 1 },
                new BlowPort { Type = BlowHandleType.Source, Id = "out", DataType = "response" },
            ],
            CreateData = () => new Dictionary<string, string> { ["method"] = "GET" },
        },
        new BlowNodeDefinition
        {
            Type = "switch",
            Label = "Switch",
            Category = "Logic",
            Ports = [new BlowPort { Type = BlowHandleType.Target }],
            CreateData = () => new SwitchData(),
            DynamicPorts = n => ((SwitchData)n.Data!).Cases.Select(c => new BlowPort { Type = BlowHandleType.Source, Id = c, Label = c }),
            Validate = n => ((SwitchData)n.Data!).Cases.Count == 0
                ? [new BlowValidationIssue("no-cases", BlowValidationSeverity.Error, "A switch needs a case.")]
                : [],
        },
        new BlowNodeDefinition { Type = "old-http", Label = "HTTP (old)", Category = "Integrations", Deprecated = "Use HTTP request.", ReplacedBy = "http",
            Migrate = n => n.Label = "HTTP request (upgraded)" },
        new BlowNodeDefinition { Type = "internal", Label = "Internal", Hidden = true });

    [TestMethod]
    public void ANodeIsCreatedFromItsDefinition()
    {
        var registry = Registry();

        var node = registry.Create("http", new BlowXYPosition(10, 20), "n1");

        Assert.AreEqual("n1", node.Id);
        Assert.AreEqual("http", node.Type);
        Assert.AreEqual("HTTP request", node.Label);
        Assert.AreEqual(new BlowXYPosition(10, 20), node.Position);
        Assert.AreEqual(180, node.Width);
        Assert.AreEqual(BlowPosition.Right, node.SourcePosition);
        Assert.AreEqual(2, node.Ports!.Count);
        Assert.AreEqual("GET", ((Dictionary<string, string>)node.Data!)["method"]);
    }

    [TestMethod]
    public void NodesDoNotShareTheirPortsOrData()
    {
        var registry = Registry();

        var a = registry.Create("http");
        var b = registry.Create("http");
        a.Ports![0].Label = "changed";

        Assert.AreNotEqual(a.Id, b.Id);
        Assert.AreNotSame(a.Data, b.Data);
        Assert.IsNull(b.Ports![0].Label);
        Assert.IsNull(registry.Find("http")!.Ports[0].Label);
    }

    [TestMethod]
    public void CreatingAnUnknownTypeThrows()
        => Assert.ThrowsExactly<KeyNotFoundException>(() => Registry().Create("nope"));

    [TestMethod]
    public void RegisteringATypeAgainReplacesItInPlace()
    {
        var registry = Registry();
        var changes = 0;
        registry.Changed += _ => changes++;

        registry.Register(new BlowNodeDefinition { Type = "http", Label = "Web call" });

        Assert.AreEqual("Web call", registry.Definitions[0].Label);
        Assert.AreEqual(4, registry.Definitions.Count);
        Assert.IsTrue(registry.Unregister("switch"));
        Assert.IsFalse(registry.Unregister("switch"));
        Assert.AreEqual(2, changes);
    }

    [TestMethod]
    public void TheCatalogFindsKindsByWhatTheyAreCalledAndTagged()
    {
        var registry = Registry();

        CollectionAssert.AreEqual(new[] { "http" }, registry.Search("api").Select(d => d.Type).ToArray());
        CollectionAssert.AreEqual(new[] { "http" }, registry.Search("web service").Select(d => d.Type).ToArray());
        CollectionAssert.AreEqual(new[] { "switch" }, registry.Search("SWITCH").Select(d => d.Type).ToArray());
        // Hidden and deprecated kinds are not offered, and an empty query lists the rest.
        CollectionAssert.AreEqual(new[] { "http", "switch" }, registry.Search("").Select(d => d.Type).ToArray());
        CollectionAssert.AreEqual(new[] { "switch" }, registry.Search(null, category: "Logic").Select(d => d.Type).ToArray());
        CollectionAssert.AreEqual(new[] { "Integrations", "Logic" }, registry.Categories.ToArray());
    }

    [TestMethod]
    public void DynamicPortsFollowTheNodesSettings()
    {
        var registry = Registry();
        var node = registry.Create("switch", id: "s");
        var edges = new List<BlowEdge>
        {
            new() { Id = "to-a", Source = "s", SourceHandle = "a", Target = "x" },
            new() { Id = "to-b", Source = "s", SourceHandle = "b", Target = "y" },
            new() { Id = "in", Source = "z", Target = "s" },
        };
        CollectionAssert.AreEqual(new[] { null, "a", "b" }, node.Ports!.Select(p => p.Id).ToArray());

        ((SwitchData)node.Data!).Cases = ["a", "c"];
        var orphaned = registry.RefreshPorts(node, edges);

        CollectionAssert.AreEqual(new[] { null, "a", "c" }, node.Ports!.Select(p => p.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "to-b" }, orphaned.Select(e => e.Id).ToArray());
    }

    [TestMethod]
    public void ANodeWithoutPortsLeavesNoEdgeWithoutOne()
    {
        var registry = new BlowNodeRegistry().Register(new BlowNodeDefinition { Type = "plain" });
        var node = registry.Create("plain", id: "p");

        var orphaned = registry.RefreshPorts(node, [new BlowEdge { Id = "e", Source = "p", SourceHandle = "anything", Target = "q" }]);

        Assert.IsNull(node.Ports);
        Assert.AreEqual(0, orphaned.Count);
    }

    [TestMethod]
    public void ValidationReportsUnknownAndDeprecatedKindsAndWhatDefinitionsCheck()
    {
        var registry = Registry();
        var sw = registry.Create("switch", id: "s");
        ((SwitchData)sw.Data!).Cases.Clear();
        var nodes = new List<BlowNode> { sw, new() { Id = "x", Type = "mystery" }, new() { Id = "o", Type = "old-http" } };

        var result = new BlowGraphValidator(registry.ValidationRule()).Validate(nodes, []);

        Assert.AreEqual("no-cases", result.ForNode("s").Single().Code);
        Assert.AreEqual(BlowValidationCodes.UnknownNodeType, result.ForNode("x").Single().Code);
        Assert.AreEqual(BlowValidationSeverity.Warning, result.ForNode("o").Single().Severity);
        Assert.AreEqual(BlowValidationCodes.DeprecatedNodeType, result.ForNode("o").Single().Code);

        var lenient = new BlowGraphValidator(registry.ValidationRule(unknownType: null)).Validate(nodes, []);
        Assert.AreEqual(0, lenient.ForNode("x").Count);
    }

    [TestMethod]
    public void UpgradeReplacesDeprecatedKinds()
    {
        var registry = Registry();
        var nodes = new List<BlowNode> { new() { Id = "o", Type = "old-http" }, new() { Id = "s", Type = "switch" } };

        var changed = registry.Upgrade(nodes);

        Assert.AreEqual(1, changed.Count);
        Assert.AreEqual("http", nodes[0].Type);
        Assert.AreEqual("HTTP request (upgraded)", nodes[0].Label);
        Assert.AreEqual(2, nodes[0].Ports!.Count);
    }

    [TestMethod]
    public void UpgradeStopsAtALoopOfReplacements()
    {
        var registry = new BlowNodeRegistry().Register(
            new BlowNodeDefinition { Type = "a", Deprecated = "x", ReplacedBy = "b" },
            new BlowNodeDefinition { Type = "b", Deprecated = "x", ReplacedBy = "a" });
        var nodes = new List<BlowNode> { new() { Id = "n", Type = "a" } };

        registry.Upgrade(nodes);

        Assert.IsTrue(nodes[0].Type is "a" or "b");
    }

    [TestMethod]
    public async Task ARunnerRunsNodesThroughTheirDefinitions()
    {
        var registry = new BlowNodeRegistry().Register(
            new BlowNodeDefinition { Type = "one", Run = _ => Task.FromResult(BlowNodeResult.Done(1)) },
            new BlowNodeDefinition
            {
                Type = "add",
                Run = ctx => Task.FromResult(BlowNodeResult.Done(ctx.GetInputs<int>().Sum())),
                RunOptions = new BlowNodeRunOptions { Join = BlowJoinMode.All },
            });
        var runner = new BlowRunner().Use(registry);
        var nodes = new List<BlowNode> { registry.Create("one", id: "x"), registry.Create("one", id: "y"), registry.Create("add", id: "sum") };
        var edges = new List<BlowEdge> { new() { Id = "1", Source = "x", Target = "sum" }, new() { Id = "2", Source = "y", Target = "sum" } };

        var result = await runner.RunAsync(nodes, edges);

        Assert.AreEqual(2, result.ValueOf<int>("sum"));
        // A handler registered on the runner wins over the definition's.
        runner.Handle("one", _ => 10);
        Assert.AreEqual(20, (await runner.RunAsync(nodes, edges)).ValueOf<int>("sum"));
    }
}
