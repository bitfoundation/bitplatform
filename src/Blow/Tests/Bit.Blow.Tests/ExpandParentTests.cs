using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for <c>ExpandParent</c>: dragging a child past its parent's edge grows the parent to
/// keep it enclosed. Growing past the right or bottom edge only changes the parent's size, but
/// growing past the left or top has to move the parent's origin - and moving a container's
/// origin renumbers every coordinate inside it, which is where this gets interesting.
/// </summary>
[TestClass]
public class ExpandParentTests : BunitContext
{
    public ExpandParentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<BlowNode> ParentAndChild() =>
    [
        new()
        {
            Id = "group",
            Position = new(100, 100),
            Width = 300,
            Height = 200,
            Type = "group",
        },
        new()
        {
            Id = "child",
            ParentId = "group",
            Position = new(50, 50),
            Width = 80,
            Height = 40,
            ExpandParent = true,
        },
    ];

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, new List<BlowEdge>());
        });

    /// <summary>Where the node actually is, in absolute flow space.</summary>
    private static BlowXYPosition Absolute(List<BlowNode> nodes, string id)
    {
        var node = nodes.Single(n => n.Id == id);
        if (node.ParentId is not { } parentId) return node.Position;

        var parent = nodes.Single(n => n.Id == parentId);
        return new BlowXYPosition(parent.Position.X + node.Position.X, parent.Position.Y + node.Position.Y);
    }

    private static void Press(IRenderedComponent<BlowCanvas> cut, int nodeIndex, double x, double y)
        => cut.FindAll(".bit-blow-node")[nodeIndex]
            .PointerDown(new PointerEventArgs { Button = 0, ClientX = x, ClientY = y });

    private static void MoveTo(IRenderedComponent<BlowCanvas> cut, double x, double y)
        => cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = x, ClientY = y });

    [TestMethod]
    public void DraggingAChildLeftOutOfItsParentMovesItExactlyAsFarAsThePointer()
    {
        // The child starts at absolute (150, 150). Every pointer move re-derives the child's
        // position from where the drag began, so a parent shift that renumbers the child's
        // coordinates has to be reflected there too - otherwise the second move undoes the first
        // shift and applies a new one, and the parent runs away from the pointer.
        var nodes = ParentAndChild();
        var cut = Render(nodes);

        Press(cut, 1, 0, 0);
        MoveTo(cut, -60, 0);
        MoveTo(cut, -120, 0);
        MoveTo(cut, -180, 0);

        Assert.AreEqual(new BlowXYPosition(150 - 180, 150), Absolute(nodes, "child"));
    }

    [TestMethod]
    public void TheParentFollowsTheChildRatherThanOutrunningIt()
    {
        var nodes = ParentAndChild();
        var cut = Render(nodes);

        Press(cut, 1, 0, 0);
        MoveTo(cut, -60, 0);
        MoveTo(cut, -120, 0);

        // The child left through the parent's left edge by 70, so the parent's left edge follows
        // it there and no further.
        var group = nodes.Single(n => n.Id == "group");
        Assert.AreEqual(150 - 120, group.Position.X, 1e-3);
        Assert.AreEqual(100, group.Position.Y, 1e-3);
    }

    [TestMethod]
    public void TheParentGrowsByExactlyWhatTheChildNeeded()
    {
        var nodes = ParentAndChild();
        var cut = Render(nodes);

        Press(cut, 1, 0, 0);
        MoveTo(cut, -120, 0);

        // Dragging 120 left from an inset of 50 pokes out by 70, so the parent widens by 70.
        var group = nodes.Single(n => n.Id == "group");
        Assert.AreEqual(370, group.Width);
        Assert.AreEqual(200, group.Height);
    }

    [TestMethod]
    public void AnUnmovedSiblingStaysWhereItIsOnScreen()
    {
        var nodes = ParentAndChild();
        nodes.Add(new BlowNode
        {
            Id = "sibling",
            ParentId = "group",
            Position = new(200, 100),
            Width = 60,
            Height = 30,
        });

        var cut = Render(nodes);
        var before = Absolute(nodes, "sibling");

        Press(cut, 1, 0, 0);
        MoveTo(cut, -120, -80);

        // The parent moved, so the sibling's parent-relative coordinates changed - but it must
        // not have budged on screen.
        Assert.AreEqual(before, Absolute(nodes, "sibling"));
    }

    [TestMethod]
    public void DraggingRightOnlyGrowsTheParentAndLeavesItsOriginAlone()
    {
        var nodes = ParentAndChild();
        var cut = Render(nodes);

        Press(cut, 1, 0, 0);
        MoveTo(cut, 260, 0);

        var group = nodes.Single(n => n.Id == "group");
        Assert.AreEqual(new BlowXYPosition(100, 100), group.Position);
        Assert.AreEqual(390, group.Width);
        Assert.AreEqual(new BlowXYPosition(150 + 260, 150), Absolute(nodes, "child"));
    }

    [TestMethod]
    public void DraggingBackInsideDoesNotShrinkTheParentAgain()
    {
        // Growing is one-way: a container that snapped back would fight the user.
        var nodes = ParentAndChild();
        var cut = Render(nodes);

        Press(cut, 1, 0, 0);
        MoveTo(cut, -120, 0);
        var widened = nodes.Single(n => n.Id == "group").Width;

        MoveTo(cut, 0, 0);

        Assert.AreEqual(widened, nodes.Single(n => n.Id == "group").Width);
        Assert.AreEqual(new BlowXYPosition(150, 150), Absolute(nodes, "child"));
    }

    [TestMethod]
    public void AChildPinnedInsideItsParentNeverMovesTheParentAtAll()
    {
        var nodes = ParentAndChild();
        var child = nodes.Single(n => n.Id == "child");
        child.ExtentParent = true;

        var cut = Render(nodes);

        Press(cut, 1, 0, 0);
        MoveTo(cut, -400, -400);

        var group = nodes.Single(n => n.Id == "group");
        Assert.AreEqual(new BlowXYPosition(100, 100), group.Position);
        Assert.AreEqual(300, group.Width);
        Assert.AreEqual(new BlowXYPosition(0, 0), child.Position);
    }

    [TestMethod]
    public void DraggingOutThroughTheTopBehavesTheSameWayAsTheLeft()
    {
        var nodes = ParentAndChild();
        var cut = Render(nodes);

        Press(cut, 1, 0, 0);
        MoveTo(cut, 0, -70);
        MoveTo(cut, 0, -140);

        Assert.AreEqual(new BlowXYPosition(150, 150 - 140), Absolute(nodes, "child"));
        Assert.AreEqual(150 - 140, nodes.Single(n => n.Id == "group").Position.Y, 1e-3);
    }

    [TestMethod]
    public void ArrowKeysGrowTheParentJustAsDraggingDoes()
    {
        // A node moved by the keyboard is in exactly the same place as one moved by the pointer,
        // so it must not be left hanging outside a container that would have grown for a drag.
        var nodes = ParentAndChild();
        var child = nodes.Single(n => n.Id == "child");
        child.Selected = true;

        var cut = Render(nodes);
        var pane = cut.Find(".bit-blow");

        for (var i = 0; i < 15; i++)
            pane.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", ShiftKey = true });

        // 15 shift-steps of 25 is 375 left, from an inset of 50: well outside the parent.
        Assert.AreEqual(new BlowXYPosition(150 - 375, 150), Absolute(nodes, "child"));

        var group = nodes.Single(n => n.Id == "group");
        Assert.IsTrue(group.Position.X <= 150 - 375, $"the parent did not grow to keep the child: left edge at {group.Position.X}");
    }
}
