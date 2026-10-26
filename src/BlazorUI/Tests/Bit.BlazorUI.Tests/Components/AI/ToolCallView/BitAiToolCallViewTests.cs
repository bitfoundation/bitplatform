using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ToolCallView;

[TestClass]
public class BitAiToolCallViewTests : BitAiTestBase
{
    private static BitAiToolCallPart CompletedArguments(string toolName, string json = "{}")
    {
        var call = new BitAiToolCallPart("c1", toolName);
        call.CompleteArguments(JsonDocument.Parse(json).RootElement);
        return call;
    }

    private static Delegate? Subscribers(BitAiMessagePart part) =>
        (Delegate?)typeof(BitAiMessagePart)
            .GetField(nameof(BitAiMessagePart.Changed), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(part);



    [TestMethod]
    public void ARunningCallReadsAsRunningAndAFinishedOneAsUsed()
    {
        var call = new BitAiToolCallPart("c1", "get_weather");
        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));
        StringAssert.Contains(component.Markup, "Calling get_weather");

        call.CompleteArguments(JsonDocument.Parse("""{"city":"Oslo"}""").RootElement);
        call.SetResult(BitAiToolOutcome.FromText("12 degrees"));

        component.WaitForAssertion(() => StringAssert.Contains(component.Markup, "Used get_weather"), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void AFailedCallIsExpandedRatherThanHiddenBehindAChevron()
    {
        var call = CompletedArguments("deploy");

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));
        Assert.IsEmpty(component.FindAll(".bit-aitcv-bdy"));

        call.SetResult(BitAiToolOutcome.Failure("permission denied"));

        component.WaitForAssertion(() =>
        {
            StringAssert.Contains(component.Markup, "permission denied");
            Assert.AreEqual("true", component.Find(".bit-aitcv-hdr").GetAttribute("aria-expanded"));
            Assert.IsTrue(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-fld"));
        }, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void PartialArgumentsAreShownWhileTheyStream()
    {
        var call = new BitAiToolCallPart("c1", "search");
        call.AppendArguments("""{"query":"blazor str""");

        var component = RenderComponent<BitAiToolCallView>(p => p
            .Add(c => c.Call, call)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { CollapseToolCalls = false }));

        StringAssert.Contains(component.Markup, "blazor str");
    }

    [TestMethod]
    public void AnApprovalRequestOffersThreeDistinctChoices()
    {
        var call = CompletedArguments("delete_everything");
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        var approval = component.Find(".bit-aitcv-apv");
        Assert.AreEqual(3, approval.QuerySelectorAll("button").Length);
        StringAssert.Contains(component.Markup, "Always allow");
        StringAssert.Contains(component.Markup, "Decline");
        Assert.IsTrue(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-apr"));
    }

    // The prompt under the header asks the question, so the header does not ask it a second time.
    [TestMethod]
    public void TheHeaderOfACallAwaitingApprovalSaysWhatItWaitsForAndThePromptAsks()
    {
        var call = CompletedArguments("delete_everything");
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        Assert.AreEqual("delete_everything is waiting for your approval", component.Find(".bit-aitcv-ttl").TextContent);
        Assert.AreEqual("Allow delete_everything to run?", component.Find(".bit-aitap-txt").TextContent.Trim());
    }

    // The decision takes the prompt away with the button that had the focus, which would otherwise fall to the page.
    [TestMethod,
        DataRow(0, BitAiToolCallStatus.Rejected),
        DataRow(2, BitAiToolCallStatus.Pending)]
    public void ADecisionHandsTheFocusToTheHeader(int answer, BitAiToolCallStatus expected)
    {
        var call = CompletedArguments("delete_everything");
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        // Read before the click: a re-render leaves the reference off the markup of an element it keeps.
        var header = component.Find(".bit-aitcv-hdr").GetAttribute("blazor:elementReference");
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);

        component.FindAll(".bit-aitcv-apv button")[answer].Click();

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(expected, call.Status);
            Assert.IsEmpty(component.FindAll(".bit-aitcv-apv"));
            var focus = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];
            Assert.HasCount(1, focus);
            Assert.AreEqual(header, ((ElementReference)focus[0].Arguments[0]!).Id);
        }, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void ACallWithoutADefinitionIsNamedAndDrawnByTheOneRegisteredForItsTool()
    {
        Services.GetRequiredService<IBitAiToolRegistry>().Register(BitAiTool.Create(
            new BitAiToolDefinition("get_weather") { Title = "Weather", Icon = "Cloud" },
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("12 degrees"))));

        var call = new BitAiToolCallPart("c1", "get_weather");
        var unknown = new BitAiToolCallPart("c2", "not_registered");

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));
        var unknownCard = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, unknown));

        Assert.AreEqual("Calling Weather", component.Find(".bit-aitcv-ttl").TextContent);
        Assert.IsTrue(component.Find("i.bit-aitcv-ico").ClassList.Contains("bit-icon--Cloud"));
        Assert.AreEqual("Calling not_registered", unknownCard.Find(".bit-aitcv-ttl").TextContent);
        Assert.IsTrue(unknownCard.Find("i.bit-aitcv-ico").ClassList.Contains("bit-icon-ai--Repair"));

        // The definition a session puts on the call wins over the registered one.
        call.Definition = new BitAiToolDefinition("get_weather") { Title = "Forecast" };
        component.Render(p => p.Add(c => c.Call, call));

        Assert.AreEqual("Calling Forecast", component.Find(".bit-aitcv-ttl").TextContent);
    }

    [TestMethod]
    public void ARegisteredToolRendererReplacesTheDefaultCard()
    {
        Renderers.RegisterTool("get_weather", context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "weather-card");
            builder.AddContent(2, context.Call.ToolName);
            builder.CloseElement();
        });

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, new BitAiToolCallPart("c1", "get_weather")));

        Assert.AreEqual("get_weather", component.Find(".weather-card").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-aitcv-hdr"));
        Assert.IsTrue(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-cst"));
    }

    [TestMethod]
    public void ProgressIsExposedToAssistiveTechnology()
    {
        var call = CompletedArguments("index");
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetProgress(new BitAiToolProgress(0.4, "indexing files"));

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        var progressbar = component.Find("[role=progressbar]");
        Assert.AreEqual("40", progressbar.GetAttribute("aria-valuenow"));
        Assert.AreEqual("indexing files", progressbar.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void AProgressWithoutAFractionIsIndeterminate()
    {
        var call = CompletedArguments("index");
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetProgress(new BitAiToolProgress(null, "warming up"));

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        Assert.IsFalse(component.Find("[role=progressbar]").HasAttribute("aria-valuenow"));
    }

    [TestMethod]
    public async Task AToolCardStopsListeningToItsCallWhenRemoved()
    {
        var call = new BitAiToolCallPart("call_1", "get_weather");
        RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));
        Assert.IsNotNull(Subscribers(call));

        await Context.DisposeComponentsAsync();

        Assert.IsNull(Subscribers(call));
    }

    [TestMethod]
    public void ARunningCallShowsTheBusyDotsAndAFinishedOneItsDuration()
    {
        var call = CompletedArguments("index");
        call.SetStatus(BitAiToolCallStatus.Running);

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        // The heading says the call is running; the dots are only its picture.
        var spinner = component.Find(".bit-aitcv-sts .bit-aitcv-spn");
        Assert.AreEqual("true", spinner.GetAttribute("aria-hidden"));
        Assert.IsTrue(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-run"));

        call.SetResult(BitAiToolOutcome.FromText("done") with { Duration = TimeSpan.FromSeconds(2) });

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-aitcv-spn"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(component.Find(".bit-aitcv-sts").TextContent));
            Assert.IsFalse(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-run"));
        }, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void TheHeaderTogglesTheBody()
    {
        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, CompletedArguments("search", """{"q":"x"}""")));

        var header = component.Find(".bit-aitcv-hdr");
        Assert.AreEqual("false", header.GetAttribute("aria-expanded"));
        Assert.AreEqual("button", header.GetAttribute("type"));
        Assert.IsNotNull(header.QuerySelector("i.bit-aitcv-chv.bit-icon--ChevronRight[aria-hidden=true]"));
        Assert.IsNotNull(header.QuerySelector("i.bit-aitcv-ico.bit-icon-ai--Repair[aria-hidden=true]"));

        header.Click();

        var body = component.Find(".bit-aitcv-bdy");
        Assert.AreEqual(body.Id, component.Find(".bit-aitcv-hdr").GetAttribute("aria-controls"));
        Assert.AreEqual("true", component.Find(".bit-aitcv-hdr").GetAttribute("aria-expanded"));
        Assert.IsTrue(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-opn"));

        component.Find(".bit-aitcv-hdr").Click();

        Assert.IsEmpty(component.FindAll(".bit-aitcv-bdy"));
        Assert.IsFalse(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-opn"));
    }

    [TestMethod,
        DataRow(BitAiToolCallStatus.Cancelled, "get_weather was cancelled"),
        DataRow(BitAiToolCallStatus.Rejected, "get_weather was declined")]
    public void ACallThatEndedWithoutRunningSaysHowItEnded(BitAiToolCallStatus status, string heading)
    {
        var call = CompletedArguments("get_weather");
        call.SetStatus(BitAiToolCallStatus.Running);

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));
        Assert.AreEqual("Calling get_weather", component.Find(".bit-aitcv-ttl").TextContent);

        call.SetStatus(status);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(heading, component.Find(".bit-aitcv-ttl").TextContent);
            Assert.IsEmpty(component.FindAll(".bit-aitcv-spn"));
            Assert.IsFalse(component.Find(".bit-aitcv").ClassList.Contains("bit-aitcv-run"));
        }, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void ADeclinedCallIsOutlinedAsAFailedOneAndACancelledOneAtRest()
    {
        var declined = CompletedArguments("delete_everything");
        declined.SetApproval(BitAiApprovalDecision.Rejected);
        var cancelled = CompletedArguments("index");
        cancelled.SetStatus(BitAiToolCallStatus.Cancelled);

        var declinedCard = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, declined));
        var cancelledCard = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, cancelled));

        var declinedRoot = declinedCard.Find(".bit-aitcv");
        Assert.IsTrue(declinedRoot.ClassList.Contains("bit-aitcv-fld"));
        Assert.IsFalse(declinedRoot.ClassList.Contains("bit-aitcv-apr"));
        Assert.AreEqual("delete_everything was declined", declinedCard.Find(".bit-aitcv-ttl").TextContent);
        Assert.IsEmpty(declinedCard.FindAll(".bit-aitcv-apv"));

        var cancelledRoot = cancelledCard.Find(".bit-aitcv");
        Assert.IsFalse(cancelledRoot.ClassList.Contains("bit-aitcv-fld"));
        Assert.IsFalse(cancelledRoot.ClassList.Contains("bit-aitcv-run"));
        Assert.IsFalse(cancelledRoot.ClassList.Contains("bit-aitcv-apr"));
    }

    [TestMethod]
    public void TheHeadingsOfACallThatEndedWithoutRunningComeFromTheStringTable()
    {
        var options = BitAiUiOptions.Default with { Strings = new ToolStrings() };
        var cancelled = CompletedArguments("index");
        cancelled.SetStatus(BitAiToolCallStatus.Cancelled);
        var declined = CompletedArguments("deploy");
        declined.SetStatus(BitAiToolCallStatus.Rejected);

        var cancelledCard = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, cancelled).Add(c => c.CascadedOptions, options));
        var declinedCard = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, declined).Add(c => c.CascadedOptions, options));

        Assert.AreEqual("index abgebrochen", cancelledCard.Find(".bit-aitcv-ttl").TextContent);
        Assert.AreEqual("deploy abgelehnt", declinedCard.Find(".bit-aitcv-ttl").TextContent);
    }

    [TestMethod]
    public void TheHeadingNamesTheToolByTheTitleOfItsDefinition()
    {
        var call = CompletedArguments("get_weather");
        call.Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" };
        call.SetStatus(BitAiToolCallStatus.Cancelled);

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        Assert.AreEqual("Weather was cancelled", component.Find(".bit-aitcv-ttl").TextContent);
    }

    [TestMethod]
    public void TheIconTheDefinitionNamesIsDrawnInPlaceOfTheWrench()
    {
        var call = CompletedArguments("get_weather");
        call.Definition = new BitAiToolDefinition("get_weather") { Icon = "Cloud" };

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        var icon = component.Find(".bit-aitcv-hdr i.bit-aitcv-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Cloud"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon-ai"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon-ai--Repair"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("  ")]
    public void ADefinitionNamingNoIconKeepsTheWrench(string? iconName)
    {
        var call = CompletedArguments("get_weather");
        call.Definition = new BitAiToolDefinition("get_weather") { Icon = iconName };

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));

        var icon = component.Find(".bit-aitcv-hdr i.bit-aitcv-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Repair"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void TheIconFollowsADefinitionThatChanges()
    {
        var call = CompletedArguments("get_weather");
        call.Definition = new BitAiToolDefinition("get_weather") { Icon = "Cloud" };

        var component = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call));
        Assert.IsTrue(component.Find("i.bit-aitcv-ico").ClassList.Contains("bit-icon--Cloud"));

        call.Definition = new BitAiToolDefinition("get_weather") { Icon = "Sunny" };
        component.Render(p => p.Add(c => c.Call, call));

        var icon = component.Find("i.bit-aitcv-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Sunny"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Cloud"));

        call.Definition = null;
        component.Render(p => p.Add(c => c.Call, call));

        Assert.IsTrue(component.Find("i.bit-aitcv-ico").ClassList.Contains("bit-icon-ai--Repair"));
    }

    [TestMethod]
    public void TheResultIsShownUnderItsLabel()
    {
        var call = CompletedArguments("get_weather", """{"city":"Oslo"}""");
        call.SetResult(BitAiToolOutcome.FromText("12 degrees"));

        var component = RenderComponent<BitAiToolCallView>(p => p
            .Add(c => c.Call, call)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { CollapseToolCalls = false }));

        var labels = component.FindAll(".bit-aitcv-shd").Select(l => l.TextContent).ToArray();
        CollectionAssert.AreEqual(new[] { "Arguments", "Result" }, labels);
        Assert.AreEqual("12 degrees", component.Find(".bit-aitcv-raw").TextContent);
    }

    [TestMethod]
    public void ADisabledCardKeepsItsHeaderClosed()
    {
        var component = RenderComponent<BitAiToolCallView>(p => p
            .Add(c => c.Call, CompletedArguments("search"))
            .Add(c => c.Disabled, true));

        var header = component.Find(".bit-aitcv-hdr");
        Assert.IsTrue(header.HasAttribute("disabled"));
        Assert.AreEqual("true", header.GetAttribute("aria-disabled"));
        Assert.IsTrue(component.Find(".bit-aitcv").ClassList.Contains("bit-dis"));

        header.Click();

        Assert.IsEmpty(component.FindAll(".bit-aitcv-bdy"));
    }

    [TestMethod]
    public void ADisabledCardDisablesItsApproval()
    {
        var call = CompletedArguments("delete_everything");
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        var component = RenderComponent<BitAiToolCallView>(p => p
            .Add(c => c.Call, call)
            .Add(c => c.Disabled, true));

        foreach (var button in component.FindAll(".bit-aitcv-apv button"))
        {
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        }

        component.FindAll(".bit-aitcv-apv button").Last().Click();

        Assert.AreEqual(BitAiApprovalDecision.Undecided, call.Approval);
    }

    // Disabled turns the expander and the approval away, not the arguments and the result the card shows - the way an
    // accordion's turns its header away and leaves its panel live - so the card keeps the handlers the page splatted on it.
    [TestMethod]
    public void ADisabledCardKeepsTheSplattedHandlers()
    {
        var activations = 0;

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiToolCallView>(0);
                builder.AddAttribute(1, nameof(BitAiToolCallView.Call), CompletedArguments("search"));
                builder.AddAttribute(2, nameof(BitAiToolCallView.Disabled), true);
                builder.AddAttribute(3, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => activations++));
                builder.CloseComponent();
            });
        });

        component.Find(".bit-aitcv").DoubleClick();

        Assert.AreEqual(1, activations);
    }

    [TestMethod]
    public void TheRootShouldRenderTheExpectedClassesAndAttributes()
    {
        var component = RenderComponent<BitAiToolCallView>(p => p
            .Add(c => c.Call, CompletedArguments("search"))
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Id, "tool-card")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "root-class", Header = "header-class" })
            .Add(c => c.Styles, new() { Root = "padding:2px", Title = "color:red" }));

        var root = component.Find(".bit-aitcv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("tool-card", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
        Assert.IsTrue(component.Find(".bit-aitcv-hdr").ClassList.Contains("header-class"));
        Assert.AreEqual("color:red", component.Find(".bit-aitcv-ttl").GetAttribute("style"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void TheRootShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiToolCallView>(p => p
            .Add(c => c.Call, CompletedArguments("search"))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aitcv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }



    private sealed class ToolStrings : BitAiStrings
    {
        public override string ToolCancelled(string tool) => $"{tool} abgebrochen";

        public override string ToolRejected(string tool) => $"{tool} abgelehnt";
    }
}
