using Bunit;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>What a draggable renders and what it tells the script; the drag itself is in <see cref="BdndDragLifecycleTests"/>.</summary>
[TestClass]
public class BdndDraggableTests : BdndComponentTestBase
{
    private IRenderedComponent<BdndDraggable<string>> RenderDraggable(
        Action<ComponentParameterCollectionBuilder<BdndDraggable<string>>>? parameters = null, StateLog<BdndDragState>? log = null)
        => Render<BdndDraggable<string>>(p =>
        {
            p.AddCascadingValue(Manager);
            p.Add(x => x.Type, "box");
            p.Add(x => x.Item, "crate");
            if (log is not null) p.Add(x => x.ChildContent, log.Content());
            parameters?.Invoke(p);
        });

    // ---- markup ----

    [TestMethod]
    public void RendersADraggableDivAroundItsContent()
    {
        var cut = RenderDraggable(p => p.Add(x => x.ChildContent, _ => "<span class=\"card\">Crate</span>"));

        var host = cut.Find("div");
        Assert.AreEqual("true", host.GetAttribute("draggable"));
        Assert.AreEqual("Crate", host.QuerySelector(".card")!.TextContent);
    }

    [TestMethod]
    public void TheContentReceivesTheCollectedState()
    {
        var log = new StateLog<BdndDragState>();

        RenderDraggable(log: log);

        Assert.IsFalse(log.Last.IsDragging);
        Assert.IsTrue(log.Last.CanDrag);
        Assert.AreSame(Manager, log.Last.Monitor);
    }

    [TestMethod]
    public void ADraggableThatCannotDragRendersDraggableFalse()
    {
        var log = new StateLog<BdndDragState>();

        var cut = RenderDraggable(p => p.Add(x => x.CanDrag, false), log);

        Assert.AreEqual("false", cut.Find("div").GetAttribute("draggable"));
        Assert.IsFalse(log.Last.CanDrag);
    }

    [TestMethod]
    [DataRow(false, true, "true")]
    [DataRow(true, false, "false")]
    public void CanDragPredicateOverridesCanDrag(bool canDrag, bool predicate, string expected)
    {
        var cut = RenderDraggable(p => p.Add(x => x.CanDrag, canDrag).Add(x => x.CanDragPredicate, () => predicate));

        Assert.AreEqual(expected, cut.Find("div").GetAttribute("draggable"));
    }

    [TestMethod]
    public void CanDragPredicateIsEvaluatedOnEveryRender()
    {
        var allowed = true;
        var cut = RenderDraggable(p => p.Add(x => x.CanDragPredicate, () => allowed));

        allowed = false;
        cut.Render();

        Assert.AreEqual("false", cut.Find("div").GetAttribute("draggable"));
    }

    [TestMethod]
    public void WithAHandleTheHostItselfIsNotDraggable()
    {
        var cut = RenderDraggable(p => p.Add(x => x.UseHandle, true));

        Assert.AreEqual("false", cut.Find("div").GetAttribute("draggable"));
    }

    [TestMethod]
    public void TagAndOtherAttributesGoOntoTheHostElement()
    {
        var cut = RenderDraggable(p => p
            .Add(x => x.Tag, "li")
            .AddUnmatched("id", "card-1")
            .AddUnmatched("class", "card")
            .AddUnmatched("style", "display:flex")
            .AddUnmatched("data-index", "3"));

        var host = cut.Find("li");
        Assert.AreEqual("card-1", host.Id);
        Assert.AreEqual("card", host.ClassName);
        Assert.AreEqual("display:flex", host.GetAttribute("style"));
        Assert.AreEqual("3", host.GetAttribute("data-index"));
        Assert.AreEqual("true", host.GetAttribute("draggable"));
    }

    // ---- registration with the script ----

    [TestMethod]
    public void TheHostElementIsRegisteredAsADragSource()
    {
        var cut = RenderDraggable();

        var call = SourceRegistrationCall(cut.Instance);
        var options = (BdndDragSourceOptions)call.Arguments[2]!;
        Assert.IsInstanceOfType<Microsoft.AspNetCore.Components.ElementReference>(call.Arguments[0]);
        Assert.AreSame(cut.Instance, ((DotNetObjectReference<BdndDraggable<string>>)call.Arguments[1]!).Value);
        Assert.AreEqual(32, options.Id.Length);
        Assert.IsTrue(options.CanDrag);
        Assert.IsFalse(options.UseHandle);
        Assert.IsFalse(options.UseEmptyPreview);
        Assert.AreEqual("move", options.EffectAllowed);
        Assert.IsFalse(options.TrackPointer, "no drag layer follows the pointer");
    }

    [TestMethod]
    public void TheOptionsCarryTheParameters()
    {
        var cut = RenderDraggable(p => p
            .Add(x => x.CanDrag, false)
            .Add(x => x.UseHandle, true)
            .Add(x => x.UseCustomDragLayer, true)
            .Add(x => x.DropEffect, "copyMove"));

        var options = Assert.ContainsSingle(SourceRegistrations);
        Assert.AreEqual(new BdndDragSourceOptions(SourceIdOf(cut.Instance), false, true, true, "copyMove", false), options);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void ABlankDropEffectFallsBackToMove(string dropEffect)
    {
        RenderDraggable(p => p.Add(x => x.DropEffect, dropEffect));

        Assert.AreEqual("move", Assert.ContainsSingle(SourceRegistrations).EffectAllowed);
    }

    [TestMethod]
    public void EveryDraggableRegistersWithItsOwnIdThroughOneModuleImport()
    {
        var cut = Render<BdndProvider>(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box"))
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box")));

        var draggables = cut.FindComponents<BdndDraggable<string>>();

        Assert.HasCount(2, SourceRegistrations);
        Assert.AreNotEqual(SourceIdOf(draggables[0].Instance), SourceIdOf(draggables[1].Instance));
        Assert.ContainsSingle(JSInterop.Invocations["import"]);
    }

    [TestMethod]
    public void ChangedOptionsArePushedToTheExistingRegistration()
    {
        var cut = RenderDraggable();
        var id = SourceIdOf(cut.Instance);

        cut.Render(p => p.Add(x => x.CanDrag, false));
        cut.Render(p => p.Add(x => x.DropEffect, "copy"));

        Assert.ContainsSingle(SourceRegistrations);
        Assert.AreSequenceEqual(
            new[] { new BdndDragSourceOptions(id, false, false, false, "move", false), new BdndDragSourceOptions(id, false, false, false, "copy", false) },
            SourceUpdates);
    }

    [TestMethod]
    public void UnchangedOptionsAreNotPushedAgain()
    {
        var cut = RenderDraggable();

        cut.Render();
        cut.Render(p => p.Add(x => x.Item, "another crate"));

        Assert.IsEmpty(SourceUpdates);
    }

    [TestMethod]
    public void ARecreatedHostElementIsRegisteredAgain()
    {
        // a new Tag makes Blazor replace the element: the old registration listens on a detached node
        var cut = RenderDraggable();
        var id = SourceIdOf(cut.Instance);

        cut.Render(p => p.Add(x => x.Tag, "li"));

        Assert.HasCount(2, SourceRegistrations);
        Assert.AreEqual(id, SourceRegistrations[1].Id, "the handler id survives, so drags stay attributed to this source");
        Assert.ContainsSingle(Sources.Invocations["dispose"]);
        Assert.IsEmpty(SourceUpdates);
    }

    [TestMethod]
    public void RemovingAnIdleDraggableReleasesItsRegistrationAndCallbackObject()
    {
        var cut = Render<BdndProvider>(p => p.AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box")));
        var dotNetRef = (DotNetObjectReference<BdndDraggable<string>>)SourceRegistrationCall(cut.FindComponent<BdndDraggable<string>>().Instance).Arguments[1]!;

        cut.Render(p => p.AddChildContent("<p>gone</p>"));

        Assert.ContainsSingle(Sources.Invocations["dispose"]);
        Assert.ThrowsExactly<ObjectDisposedException>(() => dotNetRef.Value);
    }
}
