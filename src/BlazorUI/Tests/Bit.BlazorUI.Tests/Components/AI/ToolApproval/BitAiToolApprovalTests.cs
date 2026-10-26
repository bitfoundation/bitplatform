using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests.Components.AI.Core;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ToolApproval;

[TestClass]
public class BitAiToolApprovalTests : BitAiTestBase
{
    private static BitAiToolCallPart AwaitingApproval(BitAiToolRisk? risk = null)
    {
        var call = new BitAiToolCallPart("c1", "delete_everything");
        if (risk is { } r)
        {
            call.Definition = new BitAiToolDefinition("delete_everything") { Risk = r };
        }
        call.CompleteArguments(JsonDocument.Parse("{}").RootElement);
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);
        return call;
    }



    [TestMethod]
    public void ApprovingRecordsTheDecisionOnTheCall()
    {
        var call = AwaitingApproval();

        var component = RenderComponent<BitAiToolApproval>(p => p.Add(c => c.Call, call));
        component.FindAll("button").Last().Click();

        Assert.AreEqual(BitAiApprovalDecision.Approved, call.Approval);
    }

    [TestMethod]
    public void DecliningMarksTheCallRejected()
    {
        var call = AwaitingApproval();

        var component = RenderComponent<BitAiToolApproval>(p => p.Add(c => c.Call, call));
        component.FindAll("button").First().Click();

        Assert.AreEqual(BitAiToolCallStatus.Rejected, call.Status);
    }

    [TestMethod]
    public void AllowingAlwaysIsADecisionOfItsOwn()
    {
        var call = AwaitingApproval();
        BitAiApprovalDecision? raised = null;

        var component = RenderComponent<BitAiToolApproval>(p => p
            .Add(c => c.Call, call)
            .Add(c => c.OnDecision, decision => raised = decision));
        component.FindAll("button")[1].Click();

        Assert.AreEqual(BitAiApprovalDecision.ApprovedAlways, call.Approval);
        Assert.AreEqual(BitAiApprovalDecision.ApprovedAlways, raised);
    }

    // As a session refuses a decision on a call no longer waiting for one, so does the prompt without a session: a
    // declined call never moves back to pending.
    [TestMethod]
    public void ACallIsDecidedOnce()
    {
        var call = AwaitingApproval();
        var raised = new List<BitAiApprovalDecision>();

        var component = RenderComponent<BitAiToolApproval>(p => p
            .Add(c => c.Call, call)
            .Add(c => c.OnDecision, decision => raised.Add(decision)));

        component.FindAll("button").First().Click();
        component.FindAll("button").Last().Click();
        component.FindAll("button")[1].Click();

        Assert.AreEqual(BitAiApprovalDecision.Rejected, call.Approval);
        Assert.AreEqual(BitAiToolCallStatus.Rejected, call.Status);
        CollectionAssert.AreEqual(new[] { BitAiApprovalDecision.Rejected }, raised);
    }

    // With a session the decision is its to take, and one it refuses - the call is no longer waiting - is not reported.
    [TestMethod]
    public async Task ADecisionTheSessionRefusesIsNotReported()
    {
        await using var session = new BitAiChatSession(new BitAiFakeTransport(new BitAiMessageCompletedEvent()));
        var call = AwaitingApproval();
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(call);
        session.Conversation.AddMessage(message);

        var raised = new List<BitAiApprovalDecision>();

        var component = RenderComponent<BitAiToolApproval>(p => p
            .Add(c => c.Call, call)
            .Add(c => c.Session, session)
            .Add(c => c.OnDecision, decision => raised.Add(decision)));

        component.FindAll("button").First().Click();
        component.FindAll("button").Last().Click();

        Assert.AreEqual(BitAiToolCallStatus.Rejected, call.Status);
        CollectionAssert.AreEqual(new[] { BitAiApprovalDecision.Rejected }, raised);
    }

    [TestMethod]
    public void TheAnswersAreAGroupNamedByTheQuestionAndNeverSubmitAForm()
    {
        var component = RenderComponent<BitAiToolApproval>(p => p.Add(c => c.Call, AwaitingApproval()));

        var root = component.Find(".bit-aitap");
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Allow delete_everything to run?", root.GetAttribute("aria-label"));
        Assert.IsTrue(component.FindAll("button").All(b => b.GetAttribute("type") == "button"));
        CollectionAssert.AreEqual(new[] { "Decline", "Always allow", "Allow" }, component.FindAll("button").Select(b => b.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void TheReasonForTheApprovalIsShownWhenThereIsOne()
    {
        var call = AwaitingApproval();
        call.ApprovalReason = "This deletes every file.";

        var component = RenderComponent<BitAiToolApproval>(p => p.Add(c => c.Call, call));

        Assert.AreEqual("This deletes every file.", component.Find(".bit-aitap-txt").TextContent.Trim());
    }

    [TestMethod,
        DataRow(null, false),
        DataRow(BitAiToolRisk.ReadOnly, false),
        DataRow(BitAiToolRisk.Write, false),
        DataRow(BitAiToolRisk.Destructive, true)]
    public void ADestructiveToolIsSetApart(BitAiToolRisk? risk, bool destructive)
    {
        var component = RenderComponent<BitAiToolApproval>(p => p.Add(c => c.Call, AwaitingApproval(risk)));

        Assert.AreEqual(destructive, component.Find(".bit-aitap").ClassList.Contains("bit-aitap-dst"));

        // The allow button is drawn in the error role through the host-default class, never through its Color, so an
        // app's --bit-Button-* variables still reach it; the other two keep the neutral default.
        var buttons = component.FindAll("button");
        Assert.AreEqual(destructive, buttons[2].ClassList.Contains("bit-btn-dft-err"));
        Assert.IsFalse(buttons[2].ClassList.Contains("bit-btn-err"));
        Assert.IsTrue(buttons[0].ClassList.Contains("bit-btn-dft-ter"));
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-btn-dft-ter"));
    }

    [TestMethod]
    public void ADisabledPromptDecidesNothing()
    {
        var call = AwaitingApproval();
        var raised = false;

        var component = RenderComponent<BitAiToolApproval>(p => p
            .Add(c => c.Call, call)
            .Add(c => c.Disabled, true)
            .Add(c => c.OnDecision, _ => raised = true));

        Assert.IsTrue(component.Find(".bit-aitap").ClassList.Contains("bit-dis"));

        foreach (var button in component.FindAll("button"))
        {
            button.Click();
        }

        Assert.AreEqual(BitAiApprovalDecision.Undecided, call.Approval);
        Assert.AreEqual(BitAiToolCallStatus.AwaitingApproval, call.Status);
        Assert.IsFalse(raised);
    }

    // The prompt is unavailable while it is disabled, so the page's activation handlers splatted on it do not run;
    // the ones that only follow the pointer still do.
    [TestMethod]
    public void ADisabledPromptDropsTheSplattedActivationHandlers()
    {
        var activations = 0;
        var hovers = 0;

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiToolApproval>(0);
                builder.AddAttribute(1, nameof(BitAiToolApproval.Call), AwaitingApproval());
                builder.AddAttribute(2, nameof(BitAiToolApproval.Disabled), true);
                builder.AddAttribute(3, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => activations++));
                builder.AddAttribute(4, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, () => hovers++));
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-aitap");

        // A handler that was dropped is one bUnit finds nothing to dispatch to.
        try { root.DoubleClick(); } catch (MissingEventHandlerException) { }
        root.MouseOver();

        Assert.AreEqual(0, activations);
        Assert.AreEqual(1, hovers);
    }

    [TestMethod]
    public void TheRootShouldRenderTheExpectedClassesAndAttributes()
    {
        var component = RenderComponent<BitAiToolApproval>(p => p
            .Add(c => c.Call, AwaitingApproval())
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Id, "approval")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "root-class", Text = "text-class" })
            .Add(c => c.Styles, new() { Root = "padding:2px", Actions = "gap:3px" }));

        var root = component.Find(".bit-aitap");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("approval", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
        Assert.IsTrue(component.Find(".bit-aitap-txt").ClassList.Contains("text-class"));
        Assert.AreEqual("gap:3px", component.Find(".bit-aitap-act").GetAttribute("style"));
    }

    [TestMethod]
    public void AnAriaLabelReplacesTheQuestionAsTheNameOfTheGroup()
    {
        var component = RenderComponent<BitAiToolApproval>(p => p
            .Add(c => c.Call, AwaitingApproval())
            .Add(c => c.AriaLabel, "Tool approval"));

        Assert.AreEqual("Tool approval", component.Find(".bit-aitap").GetAttribute("aria-label"));
    }
}
