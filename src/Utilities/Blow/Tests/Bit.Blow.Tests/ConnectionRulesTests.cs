namespace Bit.Blow.Tests;

/// <summary>
/// The connection rules on their own, with no canvas: every rejection reason, the order they are checked in, typed
/// ports, limits, cycles and reconnection.
/// </summary>
[TestClass]
public class ConnectionRulesTests
{
    private static BlowNode Node(string id, params BlowPort[] ports)
        => new() { Id = id, Ports = ports.Length == 0 ? null : [.. ports] };

    private static BlowPort In(string? id = null, string? type = null, int? max = null)
        => new() { Id = id, Type = BlowHandleType.Target, DataType = type, MaxConnections = max };

    private static BlowPort Out(string? id = null, string? type = null, int? max = null)
        => new() { Id = id, Type = BlowHandleType.Source, DataType = type, MaxConnections = max };

    private static BlowConnectionRejection Check(List<BlowNode> nodes, List<BlowEdge> edges, string source, string target,
        BlowConnectionPolicy? policy = null, string? sourceHandle = null, string? targetHandle = null)
        => BlowConnectionRules.Check(nodes, edges, new BlowConnection(source, target, sourceHandle, targetHandle), policy).Rejection;

    [TestMethod]
    public void AnOrdinaryConnectionIsAllowed()
    {
        Assert.AreEqual(BlowConnectionRejection.None, Check([Node("a"), Node("b")], [], "a", "b"));
    }

    [TestMethod]
    public void AConnectionToANodeNotInTheGraphIsRefused()
    {
        Assert.AreEqual(BlowConnectionRejection.MissingNode, Check([Node("a")], [], "a", "ghost"));
    }

    [TestMethod]
    public void ANodeThatDoesNotTakeConnectionsRefuses()
    {
        var nodes = new List<BlowNode> { Node("a"), new() { Id = "b", Connectable = false } };
        Assert.AreEqual(BlowConnectionRejection.NotConnectable, Check(nodes, [], "a", "b"));

        var hidden = new List<BlowNode> { Node("a"), new() { Id = "b", Hidden = true } };
        Assert.AreEqual(BlowConnectionRejection.NotConnectable, Check(hidden, [], "a", "b"));
    }

    [TestMethod]
    public void StrictModeRefusesTwoHandlesOfTheSameType()
    {
        var graph = new BlowGraphIndex([Node("a"), Node("b")], []);
        var session = BlowConnectionRules.Begin(graph, "a", null, BlowHandleType.Source);

        Assert.AreEqual(BlowConnectionRejection.SameHandleType, session.Check("b", null, BlowHandleType.Source).Rejection);
        Assert.IsTrue(session.Check("b", null, BlowHandleType.Target).IsValid);

        var loose = BlowConnectionRules.Begin(graph, "a", null, BlowHandleType.Source, new BlowConnectionPolicy { Mode = BlowConnectionMode.Loose });
        Assert.IsTrue(loose.Check("b", null, BlowHandleType.Source).IsValid);
    }

    [TestMethod]
    public void SelfConnectionsNeedOptingInAndTwoDifferentHandles()
    {
        List<BlowNode> nodes = [Node("a")];
        Assert.AreEqual(BlowConnectionRejection.SelfConnection, Check(nodes, [], "a", "a"));

        var allowed = new BlowConnectionPolicy { AllowSelfConnections = true };
        Assert.AreEqual(BlowConnectionRejection.None, Check(nodes, [], "a", "a", allowed));

        var loose = new BlowConnectionPolicy { AllowSelfConnections = true, Mode = BlowConnectionMode.Loose };
        var session = BlowConnectionRules.Begin(new BlowGraphIndex(nodes, []), "a", "x", BlowHandleType.Source, loose);
        Assert.AreEqual(BlowConnectionRejection.SelfConnection, session.Check("a", "x", BlowHandleType.Source).Rejection);
    }

    [TestMethod]
    public void ADuplicateIsRefusedUnlessDuplicatesAreAllowed()
    {
        List<BlowNode> nodes = [Node("a"), Node("b")];
        List<BlowEdge> edges = [new() { Id = "e", Source = "a", Target = "b" }];

        Assert.AreEqual(BlowConnectionRejection.Duplicate, Check(nodes, edges, "a", "b"));
        Assert.AreEqual(BlowConnectionRejection.None, Check(nodes, edges, "a", "b", new BlowConnectionPolicy { PreventDuplicates = false }));
        // A different handle is a different connection.
        Assert.AreEqual(BlowConnectionRejection.None, Check(nodes, edges, "a", "b", targetHandle: "other"));
    }

    [TestMethod]
    public void PortsWithIncompatibleDataTypesRefuse()
    {
        List<BlowNode> nodes = [Node("a", Out(type: "number")), Node("b", In(type: "text")), Node("c", In(type: "Number"))];

        Assert.AreEqual(BlowConnectionRejection.IncompatibleTypes, Check(nodes, [], "a", "b"));
        Assert.AreEqual(BlowConnectionRejection.None, Check(nodes, [], "a", "c"));
    }

    [TestMethod]
    [DataRow(null, "text", true)]
    [DataRow("*", "text", true)]
    [DataRow("", "text", true)]
    [DataRow("int,float", "float", true)]
    [DataRow("int, float", "FLOAT", true)]
    [DataRow("int", "float", false)]
    [DataRow("image", "*", true)]
    public void TheBuiltInTypeRuleAcceptsWildcardsListsAndAnyCase(string? source, string? target, bool expected)
    {
        Assert.AreEqual(expected, BlowConnectionRules.AreTypesCompatible(source, target));
    }

    [TestMethod]
    public void ACustomTypeRuleReplacesTheBuiltInOne()
    {
        List<BlowNode> nodes = [Node("a", Out(type: "int")), Node("b", In(type: "float"))];
        var widening = new BlowConnectionPolicy { TypeCompatibility = (s, t) => s == t || (s == "int" && t == "float") };

        Assert.AreEqual(BlowConnectionRejection.None, Check(nodes, [], "a", "b", widening));
    }

    [TestMethod]
    public void APortAtItsLimitRefusesMore()
    {
        List<BlowNode> nodes = [Node("a", Out(max: 1)), Node("b", In(max: 1)), Node("c"), Node("d")];
        List<BlowEdge> edges = [new() { Id = "e", Source = "a", Target = "c" }, new() { Id = "f", Source = "d", Target = "b" }];

        Assert.AreEqual(BlowConnectionRejection.SourceFull, Check(nodes, edges, "a", "b"));
        Assert.AreEqual(BlowConnectionRejection.TargetFull, Check(nodes, edges, "c", "b"));
    }

    [TestMethod]
    public void AFullPortCannotStartAConnection()
    {
        List<BlowNode> nodes = [Node("a", Out(max: 1)), Node("c")];
        List<BlowEdge> edges = [new() { Id = "e", Source = "a", Target = "c" }];

        Assert.IsFalse(BlowConnectionRules.Begin(new BlowGraphIndex(nodes, edges), "a", null, BlowHandleType.Source).CanStart);
        Assert.IsTrue(BlowConnectionRules.Begin(new BlowGraphIndex(nodes, []), "a", null, BlowHandleType.Source).CanStart);
    }

    [TestMethod]
    public void ConnectionsThatWouldCloseACycleAreRefusedWhenCyclesArePrevented()
    {
        List<BlowNode> nodes = [Node("a"), Node("b"), Node("c")];
        List<BlowEdge> edges = [new() { Id = "1", Source = "a", Target = "b" }, new() { Id = "2", Source = "b", Target = "c" }];
        var acyclic = new BlowConnectionPolicy { PreventCycles = true };

        Assert.AreEqual(BlowConnectionRejection.Cycle, Check(nodes, edges, "c", "a", acyclic));
        Assert.AreEqual(BlowConnectionRejection.None, Check(nodes, edges, "a", "c", acyclic));
        Assert.AreEqual(BlowConnectionRejection.None, Check(nodes, edges, "c", "a"));
    }

    [TestMethod]
    public void ACycleIsFoundFromWhicheverEndTheDragStarted()
    {
        List<BlowNode> nodes = [Node("a"), Node("b"), Node("c")];
        List<BlowEdge> edges = [new() { Id = "1", Source = "a", Target = "b" }, new() { Id = "2", Source = "b", Target = "c" }];
        var acyclic = new BlowConnectionPolicy { PreventCycles = true };

        // Dragging out of a's target handle onto c's source handle would make c -> a.
        var session = BlowConnectionRules.Begin(new BlowGraphIndex(nodes, edges), "a", null, BlowHandleType.Target, acyclic);
        Assert.AreEqual(BlowConnectionRejection.Cycle, session.Check("c", null, BlowHandleType.Source).Rejection);
    }

    [TestMethod]
    public void ASelfConnectionIsACycleWhenCyclesArePrevented()
    {
        var policy = new BlowConnectionPolicy { PreventCycles = true, AllowSelfConnections = true };
        Assert.AreEqual(BlowConnectionRejection.Cycle, Check([Node("a")], [], "a", "a", policy));
    }

    [TestMethod]
    public void TheApplicationRuleIsConsultedLastAndOnlyForOtherwiseValidConnections()
    {
        var asked = new List<BlowConnection>();
        var policy = new BlowConnectionPolicy
        {
            IsValidConnection = c =>
            {
                asked.Add(c);
                return c.Target != "b";
            },
        };
        List<BlowNode> nodes = [Node("a"), Node("b"), Node("c")];
        List<BlowEdge> edges = [new() { Id = "e", Source = "a", Target = "c" }];

        Assert.AreEqual(BlowConnectionRejection.RejectedByRule, Check(nodes, edges, "a", "b", policy));
        Assert.AreEqual(BlowConnectionRejection.Duplicate, Check(nodes, edges, "a", "c", policy));
        Assert.HasCount(1, asked, "the rule was asked about a connection the built-in rules had already refused");
    }

    [TestMethod]
    public void ReconnectingAnEdgeIgnoresItForDuplicatesLimitsAndCycles()
    {
        List<BlowNode> nodes = [Node("a", Out(max: 1)), Node("b"), Node("c")];
        var moving = new BlowEdge { Id = "e", Source = "a", Target = "b" };
        List<BlowEdge> edges = [moving, new() { Id = "back", Source = "b", Target = "c" }];
        var graph = new BlowGraphIndex(nodes, edges);
        var policy = new BlowConnectionPolicy { PreventCycles = true };

        // Moving the target end of a -> b: a's single output is the one being moved, so it is not full, and
        // dropping it back on b is not a duplicate of itself.
        var session = BlowConnectionRules.Begin(graph, "a", null, BlowHandleType.Source, policy, reconnecting: moving);
        Assert.IsTrue(session.Check("b", null, BlowHandleType.Target).IsValid);
        Assert.IsTrue(session.Check("c", null, BlowHandleType.Target).IsValid);
    }

    [TestMethod]
    public void TheChecksRunInTheDocumentedOrder()
    {
        // Everything is wrong at once: hidden, same type... the most fundamental reason wins.
        List<BlowNode> nodes = [Node("a", Out(type: "x", max: 0)), new() { Id = "b", Hidden = true, Ports = [In(type: "y", max: 0)] }];
        var policy = new BlowConnectionPolicy { IsValidConnection = _ => false, PreventCycles = true };

        Assert.AreEqual(BlowConnectionRejection.NotConnectable, Check(nodes, [], "a", "b", policy));
        nodes[1].Hidden = false;
        Assert.AreEqual(BlowConnectionRejection.IncompatibleTypes, Check(nodes, [], "a", "b", policy));
        nodes[1].Ports![0].DataType = "x";
        Assert.AreEqual(BlowConnectionRejection.SourceFull, Check(nodes, [], "a", "b", policy));
        nodes[0].Ports![0].MaxConnections = null;
        Assert.AreEqual(BlowConnectionRejection.TargetFull, Check(nodes, [], "a", "b", policy));
        nodes[1].Ports![0].MaxConnections = null;
        Assert.AreEqual(BlowConnectionRejection.RejectedByRule, Check(nodes, [], "a", "b", policy));
    }

    [TestMethod]
    public void DescribeNormalisesTheConnectionSourceFirst()
    {
        var graph = new BlowGraphIndex([Node("a"), Node("b")], []);
        var fromTarget = BlowConnectionRules.Begin(graph, "b", "in", BlowHandleType.Target);

        Assert.AreEqual(new BlowConnection("a", "b", "out", "in"), fromTarget.Describe("a", "out", BlowHandleType.Source));
    }

    [TestMethod]
    public void APortMatchesItsHandleTreatingNullAndEmptyIdsAlike()
    {
        var node = new BlowNode { Id = "n", Ports = [new BlowPort { Type = BlowHandleType.Target }, new BlowPort { Id = "x", Type = BlowHandleType.Source }] };

        Assert.IsNotNull(node.FindPort(BlowHandleType.Target, null));
        Assert.IsNotNull(node.FindPort(BlowHandleType.Target, ""));
        Assert.IsNull(node.FindPort(BlowHandleType.Source, null));
        Assert.IsNotNull(node.FindPort(BlowHandleType.Source, "x"));
    }

    [TestMethod]
    public void CloningANodeCopiesItsPorts()
    {
        var node = new BlowNode { Id = "n", Ports = [Out("o", "number", 2)] };

        var clone = node.Clone();
        clone.Ports![0].DataType = "text";

        Assert.AreEqual("number", node.Ports[0].DataType);
        Assert.AreEqual(2, clone.Ports[0].MaxConnections);
    }
}
