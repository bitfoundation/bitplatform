using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs;

/// <summary>
/// Pins the members every text input inherits from BitTextInputBase, through BitTextField: the change event commits
/// the value, the input event does so only under Immediate, a debounce defers that commit to the handler that carries
/// the wait, a throttle lets the first keystroke of a burst through at once, and a field that cannot be edited takes
/// neither event.
/// </summary>
[TestClass]
public class BitTextInputBaseTests : BunitTestContext
{
    [TestMethod]
    public void TheChangeEventShouldCommitTheValueAndTheInputEventShouldNotWithoutImmediate()
    {
        var component = RenderComponent<BitTextField>();

        var input = component.Find("input");

        input.Input("typed");

        Assert.IsNull(component.Instance.Value);

        input.Change("typed");

        Assert.AreEqual("typed", component.Instance.Value);
    }

    [TestMethod]
    public void ImmediateShouldCommitTheValueOnEveryInputEvent()
    {
        var values = new List<string?>();

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ValueChanged, (string? v) => values.Add(v));
        });

        var input = component.Find("input");

        input.Input("t");
        input.Input("ty");

        CollectionAssert.AreEqual(new[] { "t", "ty" }, values);
    }

    [TestMethod]
    public async Task DebounceTimeShouldDeferTheCommitToTheHandlerThatCarriesTheWait()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DebounceTime, 10);
        });

        // The handler of the keystroke is what waits the debounce out, so the value is not there with the keystroke
        // itself and is once the handler has run to its end. The keystroke is raised and the value read in one turn
        // of the renderer's dispatcher, which is where the wait resumes: read from the test thread afterwards, a slow
        // machine could let the 10 ms run out - and the value be committed - before the read.
        Task typing = Task.CompletedTask;
        await component.InvokeAsync(() =>
        {
            typing = component.Find("input").InputAsync(new ChangeEventArgs { Value = "typed" });

            Assert.IsNull(component.Instance.Value);
        });

        await typing;

        Assert.AreEqual("typed", component.Instance.Value);
    }

    [TestMethod]
    public async Task DebounceTimeShouldKeepOnlyTheLastOfABurst()
    {
        var values = new List<string?>();

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DebounceTime, 10);
            parameters.Add(p => p.ValueChanged, (string? v) => values.Add(v));
        });

        var input = component.Find("input");

        // Both keystrokes are raised in one turn of the renderer's dispatcher, which is where the wait of each resumes:
        // the first one's 10 ms cannot run out - and commit "t" - before the second one is raised, however slow the
        // machine. Raised one after the other from the test, the first one's wait could run out in between.
        Task first = Task.CompletedTask;
        Task second = Task.CompletedTask;
        await component.InvokeAsync(() =>
        {
            first = input.InputAsync(new ChangeEventArgs { Value = "t" });
            second = input.InputAsync(new ChangeEventArgs { Value = "ty" });
        });

        await first;
        await second;

        // The first keystroke's wait was cancelled by the second one, so only the text the burst ended on is committed.
        CollectionAssert.AreEqual(new[] { "ty" }, values);
    }

    [TestMethod]
    public async Task ThrottleTimeShouldCommitTheFirstKeystrokeAtOnce()
    {
        var values = new List<string?>();

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ThrottleTime, 60_000);
            parameters.Add(p => p.ValueChanged, (string? v) => values.Add(v));
        });

        var input = component.Find("input");

        await input.InputAsync(new ChangeEventArgs { Value = "t" });

        // The ones inside the interval wait for the input to go quiet, which this test never lets happen: the dispose
        // of the field drops the wait.
        _ = input.InputAsync(new ChangeEventArgs { Value = "ty" });

        CollectionAssert.AreEqual(new[] { "t" }, values);
    }

    [TestMethod]
    public void DebounceTimeShouldWinOverThrottleTime()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DebounceTime, 60_000);
            parameters.Add(p => p.ThrottleTime, 1);
        });

        // Under a throttle alone the first keystroke would commit at once; the debounce beside it holds it back.
        _ = component.Find("input").InputAsync(new ChangeEventArgs { Value = "typed" });

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod,
        DataRow(true, false),
        DataRow(false, true)
    ]
    public void AFieldThatCannotBeEditedShouldTakeNeitherEvent(bool readOnly, bool disabled)
    {
        var changes = 0;

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.Disabled, disabled);
            parameters.Add(p => p.DefaultValue, "kept");
            parameters.Add(p => p.OnChange, (string? _) => changes++);
        });

        var input = component.Find("input");

        input.Input("typed");
        input.Change("typed");

        Assert.AreEqual("kept", component.Instance.Value);
        Assert.AreEqual(0, changes);
    }
}
