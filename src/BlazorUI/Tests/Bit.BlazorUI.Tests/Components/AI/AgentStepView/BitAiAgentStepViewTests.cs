using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.AgentStepView;

[TestClass]
public class BitAiAgentStepViewTests : BitAiTestBase
{
    private static int Subscribers(BitAiAgentStep step) =>
        ((Delegate?)typeof(BitAiAgentStep)
            .GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(step))?.GetInvocationList().Length ?? 0;



    [TestMethod]
    public void BitAiAgentStepViewShouldRenderTheStep()
    {
        var step = new BitAiAgentStep("search", "Search the docs") { Detail = "Looking for the setup guide" };

        var component = RenderComponent<BitAiAgentStepView>(p => p.Add(c => c.Step, step));

        var root = component.Find(".bit-aiasv");

        Assert.AreEqual("DIV", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("bit-aiasv-pnd"));
        Assert.AreEqual("Search the docs", component.Find(".bit-aiasv-ttl > span").TextContent);
        Assert.AreEqual("Looking for the setup guide", component.Find(".bit-aiasv-dtl").TextContent);
        Assert.AreEqual("true", component.Find(".bit-aiasv-mrk").GetAttribute("aria-hidden"));
        Assert.IsEmpty(component.FindAll(".bit-aiasv-mic"));
        Assert.IsEmpty(component.FindAll(".bit-aiasv-tls"));
    }

    [TestMethod]
    public void TheStatusClassFollowsTheStep()
    {
        var step = new BitAiAgentStep("search", "Search");

        var component = RenderComponent<BitAiAgentStepView>(p => p.Add(c => c.Step, step));
        Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains("bit-aiasv-pnd"));

        step.Start();
        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains("bit-aiasv-run")));
        Assert.IsNotNull(component.Find(".bit-aiasv-dur"));

        step.Complete(summary: "Found 3 pages");
        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains("bit-aiasv-cmp")));
        Assert.IsFalse(component.Find(".bit-aiasv").ClassList.Contains("bit-aiasv-run"));
        Assert.AreEqual("Found 3 pages", component.Find(".bit-aiasv-smr").TextContent);
        // A completed step is marked with a check.
        Assert.IsTrue(component.Find(".bit-aiasv-mrk .bit-aiasv-mic").ClassList.Contains("bit-icon--Accept"));
    }

    [TestMethod,
        DataRow(BitAiAgentStepStatus.Failed, "bit-aiasv-fld"),
        DataRow(BitAiAgentStepStatus.Skipped, "bit-aiasv-skp")]
    public void AFinishedStepSaysHowItFinished(BitAiAgentStepStatus status, string expectedClass)
    {
        var step = new BitAiAgentStep("search", "Search");
        step.Start();
        step.Complete(status, error: status is BitAiAgentStepStatus.Failed ? new BitAiError("No access") : null);

        var component = RenderComponent<BitAiAgentStepView>(p => p.Add(c => c.Step, step));

        Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains(expectedClass));

        if (status is BitAiAgentStepStatus.Failed)
        {
            Assert.AreEqual("No access", component.Find(".bit-aiasv-err").TextContent);
        }
    }

    [TestMethod]
    public void ABlockedStepIsMarkedAsBlocked()
    {
        var step = new BitAiAgentStep("ask", "Ask");
        step.Start();
        step.Block();

        var component = RenderComponent<BitAiAgentStepView>(p => p.Add(c => c.Step, step));

        Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains("bit-aiasv-blk"));
    }

    [TestMethod]
    public void AStepListsTheToolCallsItMadeByName()
    {
        var conversation = new BitAiConversation();
        var message = conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
        var call = new BitAiToolCallPart("call_1", "search_docs");
        message.AddPart(call);
        var step = new BitAiAgentStep("search", "Search");
        step.AddToolCall("call_1");
        step.AddToolCall("call_unknown");

        var component = RenderComponent<BitAiAgentStepView>(p => p.Add(c => c.Step, step).Add(c => c.Conversation, conversation));

        var list = component.Find(".bit-aiasv-tls");
        Assert.AreEqual("UL", list.TagName);
        Assert.AreEqual("Tool calls", list.GetAttribute("aria-label"));

        var chips = component.FindAll(".bit-aiasv-tls > .bit-aiasv-tli .bit-tag");
        Assert.HasCount(2, chips);
        StringAssert.Contains(chips[0].TextContent, "search_docs");
        StringAssert.Contains(chips[1].TextContent, "call_unknown");
        Assert.AreEqual("search_docs", chips[0].GetAttribute("title"));
        Assert.AreEqual("call_unknown", chips[1].GetAttribute("title"));
        // The wrench is no glyph of the core font, so the icon also carries the class of the package's own font.
        var icon = chips[0].QuerySelector(".bit-tag-icn")!;
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Repair"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Repair"));
        // A neutral tag through the host-default class, never a Color, so the app's --bit-Tag-* variables reach it.
        Assert.IsTrue(chips[0].ClassList.Contains("bit-tag-dft-ter"));
        Assert.IsNull(component.FindComponents<BitTag>()[0].Instance.Color);

        // A running call shows the dots of a loader that announces nothing of its own.
        var dots = component.Find(".bit-aiasv-tli .bit-aiasv-tld");
        Assert.AreEqual("none", dots.GetAttribute("role"));
        Assert.IsNull(dots.GetAttribute("aria-live"));

        call.CompleteArguments(JsonDocument.Parse("{}").RootElement);
        call.SetResult(BitAiToolOutcome.Failure("boom"));
        step.Complete(BitAiAgentStepStatus.Failed);

        component.WaitForAssertion(() => Assert.IsTrue(component.FindAll(".bit-aiasv-tls .bit-tag")[0].ClassList.Contains("bit-tag-err")));
        Assert.IsEmpty(component.FindAll(".bit-aiasv-tld"));
    }

    [TestMethod]
    public async Task AStepViewStopsListeningToItsStepWhenRemoved()
    {
        var step = new BitAiAgentStep("search", "Search");
        var before = Subscribers(step);

        RenderComponent<BitAiAgentStepView>(p => p.Add(c => c.Step, step));
        Assert.AreEqual(before + 1, Subscribers(step));

        await Context.DisposeComponentsAsync();

        Assert.AreEqual(before, Subscribers(step));
    }

    [TestMethod]
    public void AStepViewSwitchesItsSubscriptionWhenGivenAnotherStep()
    {
        var first = new BitAiAgentStep("one", "One");
        var second = new BitAiAgentStep("two", "Two");

        var component = RenderComponent<BitAiAgentStepView>(p => p.Add(c => c.Step, first));
        component.Render(p => p.Add(c => c.Step, second));

        Assert.AreEqual(0, Subscribers(first));
        Assert.AreEqual(1, Subscribers(second));
        Assert.AreEqual("Two", component.Find(".bit-aiasv-ttl > span").TextContent);
    }

    [TestMethod]
    public void BitAiAgentStepViewShouldRespectClassStyleAndDisabled()
    {
        var component = RenderComponent<BitAiAgentStepView>(p => p
            .Add(c => c.Step, new BitAiAgentStep("search", "Search"))
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Disabled, true));

        var root = component.Find(".bit-aiasv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiAgentStepViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiAgentStepView>(p => p
            .Add(c => c.Step, new BitAiAgentStep("search", "Search"))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aiasv").GetAttribute("style");

        if (expected is null)
        {
            Assert.IsNull(style);
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiAgentStepViewShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiAgentStepView>(p => p
            .Add(c => c.Step, new BitAiAgentStep("search", "Search"))
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aiasv");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiAgentStepViewShouldHandTheClassesAndStylesToItsParts()
    {
        var step = new BitAiAgentStep("search", "Search");
        step.AddToolCall("call_1");

        var component = RenderComponent<BitAiAgentStepView>(p => p
            .Add(c => c.Step, step)
            .Add(c => c.Classes, new() { Root = "c-root", Marker = "c-marker", Title = "c-title", ToolCall = "c-item", ToolCallTag = new() { Root = "c-tag" } })
            .Add(c => c.Styles, new() { Root = "color: red", Body = "color: blue" }));

        Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains("c-root"));
        StringAssert.Contains(component.Find(".bit-aiasv").GetAttribute("style"), "color: red");
        Assert.AreEqual("color: blue", component.Find(".bit-aiasv-bdy").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aiasv-mrk").ClassList.Contains("c-marker"));
        Assert.IsTrue(component.Find(".bit-aiasv-ttl").ClassList.Contains("c-title"));
        Assert.IsTrue(component.Find(".bit-aiasv-tli").ClassList.Contains("c-item"));
        Assert.IsTrue(component.Find(".bit-aiasv-tli .bit-tag").ClassList.Contains("c-tag"));
    }

    [TestMethod]
    public void BitAiAgentStepViewShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiAgentStepViewParams { Class = "cascaded", Classes = new() { Title = "cascaded-title" } }
            });
            p.AddChildContent<BitAiAgentStepView>(c => c.Add(x => x.Step, new BitAiAgentStep("search", "Search")));
        });

        Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aiasv-ttl").ClassList.Contains("cascaded-title"));
    }

    [TestMethod]
    public void BitAiAgentStepViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiAgentStepView", BitAiAgentStepViewParams.ParamName);
        Assert.AreEqual(BitAiAgentStepViewParams.ParamName, new BitAiAgentStepViewParams().Name);
    }
}
