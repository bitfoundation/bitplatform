namespace Bit.Blow.Tests;

/// <summary>Graph validation with the built-in and custom rules, with no UI.</summary>
[TestClass]
public class GraphValidatorTests
{
    private static BlowValidationResult Validate(List<BlowNode> nodes, List<BlowEdge> edges, BlowGraphValidator? validator = null)
        => (validator ?? BlowGraphValidator.CreateDefault()).Validate(nodes, edges);

    private static string[] Codes(BlowValidationResult result) => result.Issues.Select(i => i.Code).ToArray();

    [TestMethod]
    public void AWellFormedGraphIsValid()
    {
        var result = Validate(
            [new() { Id = "a" }, new() { Id = "b" }],
            [new() { Id = "e", Source = "a", Target = "b" }]);

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Issues);
    }

    [TestMethod]
    public void TheStructureRuleReportsDuplicateIdsAndMissingReferences()
    {
        var result = Validate(
            [new() { Id = "a" }, new() { Id = "a" }, new() { Id = "child", ParentId = "nobody" }],
            [new() { Id = "e", Source = "a", Target = "ghost" }, new() { Id = "e", Source = "a", Target = "a" }]);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.AreEquivalent(
            new[] { BlowValidationCodes.DuplicateNodeId, BlowValidationCodes.DuplicateEdgeId, BlowValidationCodes.MissingEdgeEndpoint, BlowValidationCodes.MissingParentNode },
            Codes(result));
        Assert.AreEqual("child", result.Issues.Single(i => i.Code == BlowValidationCodes.MissingParentNode).NodeId);
    }

    [TestMethod]
    public void EveryNodeOnAParentCycleIsReported()
    {
        var result = Validate(
            [new() { Id = "a", ParentId = "b" }, new() { Id = "b", ParentId = "c" }, new() { Id = "c", ParentId = "a" }, new() { Id = "d", ParentId = "a" }, new() { Id = "self", ParentId = "self" }],
            []);

        var cyclic = result.Issues.Where(i => i.Code == BlowValidationCodes.ParentCycle).Select(i => i.NodeId).ToArray();
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c", "self" }, cyclic);
    }

    [TestMethod]
    public void AnEdgeToAPortTheNodeDoesNotDeclareIsReported()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Ports = [new BlowPort { Id = "out", Type = BlowHandleType.Source }] },
            new() { Id = "b" },
        };

        var result = Validate(nodes, [new() { Id = "e", Source = "a", SourceHandle = "typo", Target = "b" }]);

        var issue = result.Issues.Single();
        Assert.AreEqual(BlowValidationCodes.UnknownPort, issue.Code);
        Assert.AreEqual("a", issue.NodeId);
        Assert.AreEqual("e", issue.EdgeId);
        Assert.AreEqual("typo", issue.PortId);
        Assert.AreEqual(BlowHandleType.Source, issue.PortType);
    }

    [TestMethod]
    public void ARequiredPortLeftUnconnectedIsAnError()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "sum", Ports = [new BlowPort { Id = "x", Type = BlowHandleType.Target, MinConnections = 1, Label = "X" }] },
        };

        var result = Validate(nodes, []);

        var issue = result.Issues.Single();
        Assert.AreEqual(BlowValidationCodes.TooFewConnections, issue.Code);
        Assert.AreEqual(BlowValidationSeverity.Error, issue.Severity);
        Assert.AreEqual("x", issue.PortId);
        StringAssert.Contains(issue.Message, "'X'");
    }

    [TestMethod]
    public void APortOverItsLimitAndAnEdgeBetweenIncompatibleTypesAreErrors()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Ports = [new BlowPort { Type = BlowHandleType.Source, DataType = "number", MaxConnections = 1 }] },
            new() { Id = "b", Ports = [new BlowPort { Type = BlowHandleType.Target, DataType = "text" }] },
            new() { Id = "c" },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "1", Source = "a", Target = "b" },
            new() { Id = "2", Source = "a", Target = "c" },
        };

        var result = Validate(nodes, edges);

        CollectionAssert.AreEquivalent(new[] { BlowValidationCodes.TooManyConnections, BlowValidationCodes.IncompatibleTypes }, Codes(result));
        Assert.AreEqual("1", result.Issues.Single(i => i.Code == BlowValidationCodes.IncompatibleTypes).EdgeId);
    }

    [TestMethod]
    public void OptionalRulesForCyclesIsolationAndReachability()
    {
        var nodes = new List<BlowNode> { new() { Id = "start", Type = "input" }, new() { Id = "a" }, new() { Id = "b" }, new() { Id = "lonely" } };
        var edges = new List<BlowEdge> { new() { Id = "1", Source = "a", Target = "b" }, new() { Id = "2", Source = "b", Target = "a" } };
        var validator = new BlowGraphValidator(
            BlowValidationRules.NoCycles(),
            BlowValidationRules.NoIsolatedNodes(),
            BlowValidationRules.AllReachableFrom(n => n.Type == "input"));

        var result = validator.Validate(nodes, edges);

        CollectionAssert.AreEquivalent(new[] { "a", "b" },
            result.Issues.Where(i => i.Code == BlowValidationCodes.Cycle).Select(i => i.NodeId).ToArray());
        CollectionAssert.AreEquivalent(new[] { "start", "lonely" },
            result.Issues.Where(i => i.Code == BlowValidationCodes.IsolatedNode).Select(i => i.NodeId).ToArray());
        CollectionAssert.AreEquivalent(new[] { "a", "b", "lonely" },
            result.Issues.Where(i => i.Code == BlowValidationCodes.UnreachableNode).Select(i => i.NodeId).ToArray());
        Assert.IsFalse(result.IsValid, "a cycle is an error by default");
    }

    [TestMethod]
    public void CustomNodeAndEdgeRules()
    {
        var nodes = new List<BlowNode> { new() { Id = "task", Type = "task" }, new() { Id = "done", Type = "task", Label = "Done" } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "task", Target = "done" } };
        var validator = new BlowGraphValidator()
            .Add(BlowValidationRules.ForNodes("unnamed", n => n.Label is null ? $"{n.Id} has no name." : null, BlowValidationSeverity.Warning))
            .Add(BlowValidationRules.ForEdges("into-done", (e, s, t) => t.Label == "Done" && s.Label is null ? "Only named steps may finish." : null));

        var result = validator.Validate(nodes, edges);

        Assert.AreEqual(BlowValidationSeverity.Warning, result.SeverityOf(nodes[0]));
        Assert.AreEqual(BlowValidationSeverity.Error, result.SeverityOf(edges[0]));
        Assert.IsNull(result.SeverityOf(nodes[1]));
        Assert.HasCount(1, result.ForNode("task"));
        Assert.HasCount(1, result.ForEdge("e"));
    }

    [TestMethod]
    public void ARuleWrittenAsAFunctionSeesTheGraph()
    {
        var validator = new BlowGraphValidator().Add(ctx => ctx.Graph.Nodes.Count > 1
            ? [new BlowValidationIssue("too-big", BlowValidationSeverity.Info, "Keep it small.")]
            : []);

        var result = validator.Validate([new() { Id = "a" }, new() { Id = "b" }], []);

        Assert.IsTrue(result.IsValid, "an info issue is not an error");
        Assert.AreEqual("too-big", result.Issues.Single().Code);
    }

    [TestMethod]
    public void TheReportIsBoundedForAHopelesslyBrokenGraph()
    {
        var nodes = Enumerable.Range(0, 5000).Select(_ => new BlowNode { Id = "same" }).ToList();
        var validator = BlowGraphValidator.CreateDefault();
        validator.MaxIssues = 100;

        var result = validator.Validate(nodes, []);

        Assert.HasCount(100, result.Issues);
        Assert.IsTrue(result.IsTruncated);
    }

    [TestMethod]
    public void ValidationCanBeCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(() =>
            BlowGraphValidator.CreateDefault().Validate([new() { Id = "a" }], [], cts.Token));
    }
}
