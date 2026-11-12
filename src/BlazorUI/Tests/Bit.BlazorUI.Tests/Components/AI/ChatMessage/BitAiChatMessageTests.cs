using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests.Components.AI.Core;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ChatMessage;

[TestClass]
public class BitAiChatMessageTests : BitAiTestBase
{
    private static BitAiMessage User(string text)
    {
        var message = new BitAiMessage(BitAiRole.User);
        message.AddPart(new BitAiTextPart(text));
        return message;
    }

    // Ported from MessageRenderingTests.UserMessagesRenderAsABubbleAndAssistantMessagesAsProse.
    [TestMethod]
    public void UserMessagesRenderAsABubbleAndAssistantMessagesAsProse()
    {
        var user = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, User("hello")));

        Assert.IsTrue(user.Find("article.bit-aicms").ClassList.Contains("bit-aicms-usr"));
        Assert.AreEqual("hello", user.Find(".bit-aicms-bbl").TextContent);

        var assistant = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, Assistant(new BitAiTextPart("hi"))));

        Assert.IsTrue(assistant.Find("article.bit-aicms").ClassList.Contains("bit-aicms-ast"));
        Assert.Contains("hi", assistant.Find(".bit-aicms-bdy").TextContent);
        Assert.IsEmpty(assistant.FindAll(".bit-aicms-bbl"));
    }

    // Ported from MessageRenderingTests.SystemMessagesRenderAsAPill.
    [TestMethod]
    public void SystemMessagesRenderAsAPill()
    {
        var system = new BitAiMessage(BitAiRole.System);
        system.AddPart(new BitAiTextPart("You are helpful."));

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, system));

        Assert.IsTrue(component.Find("article").ClassList.Contains("bit-aicms-sys"));
        Assert.AreEqual("You are helpful.", component.Find(".bit-aicms-pil").TextContent);
    }

    // Ported from MessageRenderingTests.APendingMessageWithNoContentShowsTheThinkingIndicator.
    [TestMethod]
    public void APendingMessageWithNoContentShowsTheThinkingIndicator()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.SetStatus(BitAiMessageStatus.Pending);

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.ContainsSingle(component.FindComponents<BitAiThinkingIndicator>());
        Assert.AreEqual("true", component.Find("article").GetAttribute("aria-busy"));
    }

    // Ported from MessageRenderingTests.AStatusLineReplacesTheGenericThinkingLabel.
    [TestMethod]
    public void AStatusLineReplacesTheGenericThinkingLabel()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.SetStatus(BitAiMessageStatus.Streaming);
        message.SetStatusText("Searching the web");

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.Contains("Searching the web", component.Markup);
    }

    [TestMethod]
    public void TheThinkingIndicatorGoesOnceTheAnswerStreamsInAndComesBackForALaterStatus()
    {
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation);
        var message = processor.BeginGeneration();
        processor.Apply(new BitAiStatusEvent("Searching the web"));

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));
        Assert.AreEqual("Searching the web", component.FindComponent<BitAiThinkingIndicator>().Instance.Label);

        processor.Apply(new BitAiTextDeltaEvent("The answer "));

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindComponents<BitAiThinkingIndicator>());
            Assert.Contains("The answer", component.Markup);
        }, TimeSpan.FromSeconds(2));

        processor.Apply(new BitAiStatusEvent("Running search"));

        component.WaitForAssertion(() =>
            Assert.AreEqual("Running search", component.FindComponent<BitAiThinkingIndicator>().Instance.Label), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void AReplyWaitingForItsFirstTokenShowsTheThinkingIndicator()
    {
        // The stream starts the message (it is Streaming from then on) well before its first token arrives.
        var processor = new BitAiConversationStreamProcessor(new BitAiConversation());
        var message = processor.BeginGeneration();
        processor.Apply(new BitAiMessageStartedEvent());
        Assert.AreEqual(BitAiMessageStatus.Streaming, message.Status);

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.IsNull(component.FindComponent<BitAiThinkingIndicator>().Instance.Label);
        Assert.AreEqual("true", component.Find("article").GetAttribute("aria-busy"));

        processor.Apply(new BitAiTextDeltaEvent("The answer"));

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindComponents<BitAiThinkingIndicator>());
            Assert.Contains("The answer", component.Markup);
        }, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void AStatusLineThatClearsBeforeTheAnswerLeavesTheReplyThinking()
    {
        var processor = new BitAiConversationStreamProcessor(new BitAiConversation());
        var message = processor.BeginGeneration();
        processor.Apply(new BitAiMessageStartedEvent());
        processor.Apply(new BitAiStatusEvent("Searching the documentation"));

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));
        Assert.AreEqual("Searching the documentation", component.FindComponent<BitAiThinkingIndicator>().Instance.Label);

        processor.Apply(new BitAiStatusEvent(null));

        component.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(component.FindComponents<BitAiThinkingIndicator>());
            Assert.IsNull(component.FindComponent<BitAiThinkingIndicator>().Instance.Label);
        }, TimeSpan.FromSeconds(2));

        processor.Apply(new BitAiTextDeltaEvent("I found it."));

        component.WaitForAssertion(() => Assert.IsEmpty(component.FindComponents<BitAiThinkingIndicator>()), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void APartOpenedBeforeItsFirstTextIsNotContentYet()
    {
        var processor = new BitAiConversationStreamProcessor(new BitAiConversation());
        var message = processor.BeginGeneration();
        processor.Apply(new BitAiMessageStartedEvent());
        processor.Apply(new BitAiPartStartedEvent(BitAiMessagePartKinds.Markdown) { PartId = "p1" });
        Assert.ContainsSingle(message.Parts);

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));
        Assert.ContainsSingle(component.FindComponents<BitAiThinkingIndicator>());

        // The first text reaching the part changes no structure of the message, and still takes the indicator away.
        processor.Apply(new BitAiTextDeltaEvent("Hello") { PartId = "p1" });

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindComponents<BitAiThinkingIndicator>());
            Assert.Contains("Hello", component.Markup);
        }, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void AReplyThatShowsSomethingOfItsOwnIsNotThinking()
    {
        var reasoning = new BitAiMessage(BitAiRole.Assistant);
        reasoning.SetStatus(BitAiMessageStatus.Streaming);
        reasoning.AddPart(new BitAiReasoningPart());

        var tool = new BitAiMessage(BitAiRole.Assistant);
        tool.SetStatus(BitAiMessageStatus.Streaming);
        tool.AddPart(new BitAiToolCallPart("call_1", "search"));

        foreach (var message in new[] { reasoning, tool })
        {
            var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

            Assert.IsEmpty(component.FindComponents<BitAiThinkingIndicator>());
        }
    }

    // Still being generated - so no actions of a finished reply - but waiting on its reader rather than on the model: a
    // busy message would keep a screen reader from announcing the question it asks.
    [TestMethod]
    public void AReplyWaitingForADecisionOnItsToolIsNotBusy()
    {
        var message = Assistant(new BitAiTextPart("Let me clean up."));
        var call = message.AddPart(new BitAiToolCallPart("c1", "delete_all"));
        message.SetStatus(BitAiMessageStatus.Streaming);

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.AreEqual("true", component.Find("article").GetAttribute("aria-busy"));

        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        component.WaitForAssertion(() => Assert.AreEqual("false", component.Find("article").GetAttribute("aria-busy")), TimeSpan.FromSeconds(2));
        Assert.IsEmpty(component.FindComponents<BitAiMessageActions>());

        call.SetStatus(BitAiToolCallStatus.Running);

        component.WaitForAssertion(() => Assert.AreEqual("true", component.Find("article").GetAttribute("aria-busy")), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void AFinishedReplyWithNothingInItIsNotThinking()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.SetStatus(BitAiMessageStatus.Complete);

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.IsEmpty(component.FindComponents<BitAiThinkingIndicator>());
        Assert.AreEqual("false", component.Find("article").GetAttribute("aria-busy"));
    }

    // Ported from MessageRenderingTests.AFailedMessageShowsAnAlertWithItsReason.
    [TestMethod]
    public void AFailedMessageShowsAnAlertWithItsReason()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.SetError(new BitAiError("rate limited") { IsRetryable = true });

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.IsNotNull(component.Find("[role=alert]"));
        Assert.Contains("rate limited", component.Markup);
    }

    // Ported from MessageRenderingTests.CitedSourcesAreListedUnderTheMessage.
    [TestMethod]
    public void CitedSourcesAreListedUnderTheMessage()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        message.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Reference Guide")
        {
            Url = "https://example.com/guide",
        }));

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.ContainsSingle(component.FindComponents<BitAiSourceList>());
        Assert.Contains("Reference Guide", component.Markup);
    }

    [TestMethod]
    public void TheSourcesAreNotListedTwiceWhenAPartListsThem()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        message.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Reference Guide")));
        message.AddPart(new BitAiSourcesPart());

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        // The one list is the part's, drawn where the part sits, rather than a second one under the message.
        Assert.ContainsSingle(component.FindComponents<BitAiSourceList>());
        Assert.ContainsSingle(component.FindComponents<BitAiMessagePartView>().Where(v => v.Instance.Part is BitAiSourcesPart).ToList());
    }

    // Ported from MessageRenderingTests.MessagePartsAreDispatchedByKind.
    [TestMethod]
    public void MessagePartsAreDispatchedByKind()
    {
        var message = Assistant(
            new BitAiReasoningPart("thinking"),
            new BitAiTextPart("answer"),
            new BitAiToolCallPart("c1", "search"));

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.HasCount(3, component.FindComponents<BitAiMessagePartView>());
        Assert.ContainsSingle(component.FindComponents<BitAiReasoningPartView>());
        Assert.ContainsSingle(component.FindComponents<BitAiToolCallView>());
        Assert.Contains("answer", component.Markup);
    }

    // Ported from MessageRenderingTests.ARegisteredPartRendererReplacesTheBuiltInOne.
    [TestMethod]
    public void ARegisteredPartRendererReplacesTheBuiltInOne()
    {
        Renderers.RegisterPart("acme:badge", context => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "acme-badge");
            builder.AddContent(2, ((BitAiCustomPart<string>)context.Part).Data);
            builder.CloseElement();
        });

        var message = Assistant(new BitAiCustomPart<string>("acme:badge", "shipped"));

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.AreEqual("shipped", component.Find(".acme-badge").TextContent);
    }

    // Ported from MessageRenderingTests.StreamingATokenUpdatesTheRenderedText.
    [TestMethod]
    public void StreamingATokenUpdatesTheRenderedText()
    {
        var part = new BitAiTextPart();
        var message = Assistant(part);

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        part.Append("streamed in");

        component.WaitForAssertion(() => Assert.Contains("streamed in", component.Markup), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void TheMessageIsIdentifiedByItsIdAndRole()
    {
        var message = User("hello");

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        var root = component.Find("article");

        Assert.AreEqual(message.MessageId, root.GetAttribute("data-message-id"));
        Assert.AreEqual("user", root.GetAttribute("data-role"));
        Assert.AreEqual("false", root.GetAttribute("aria-busy"));
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitAiChatMessageShouldRespectAnimate(bool animate)
    {
        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Animate, animate));

        Assert.AreEqual(animate, component.Find("article").ClassList.Contains("bit-aicms-ent"));
    }

    [TestMethod]
    public void AnAssistantMessageOffersItsActionsOnlyOnceItIsDone()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        message.SetStatus(BitAiMessageStatus.Streaming);

        var component = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        Assert.IsEmpty(component.FindComponents<BitAiMessageActions>());

        message.SetStatus(BitAiMessageStatus.Complete);

        component.WaitForAssertion(() => Assert.ContainsSingle(component.FindComponents<BitAiMessageActions>()));
    }

    [TestMethod]
    public void AUserMessageOffersCopyAndEditOnlyWithASession()
    {
        var without = RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, User("hello")));

        Assert.IsEmpty(without.FindAll(".bit-aicms-act"));

        var session = new BitAiChatSession(new SilentTransport());
        var with = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Session, session));

        Assert.ContainsSingle(with.FindComponents<BitAiCopyButton>());
        Assert.AreEqual("Edit", with.Find(".bit-aicms-act button[title='Edit']").GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task AUserMessageIsEditedInPlaceAndResent()
    {
        var session = new BitAiChatSession(new SilentTransport(), options: new BitAiChatSessionOptions { Pacing = null });
        var message = session.Conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.Session, session));

        component.Find("button[title='Edit']").Click();

        var editor = component.Find(".bit-aicms-edt textarea");
        Assert.AreEqual("hello", editor.GetAttribute("value") ?? editor.TextContent);
        Assert.IsEmpty(component.FindAll(".bit-aicms-bbl"));

        editor.Input("hello again");
        component.FindAll(".bit-aicms-eda button")[1].Click();

        component.WaitForAssertion(() => Assert.IsEmpty(component.FindAll(".bit-aicms-edt")));
        component.WaitForAssertion(() => Assert.AreEqual("hello again", session.Conversation.Messages[0].GetText()));

        await session.DisposeAsync();
    }

    [TestMethod]
    public void CancellingAnEditKeepsTheMessage()
    {
        var session = new BitAiChatSession(new SilentTransport());
        var message = session.Conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.Session, session));

        component.Find("button[title='Edit']").Click();
        component.Find(".bit-aicms-edt textarea").Input("changed");
        component.FindAll(".bit-aicms-eda button")[0].Click();

        Assert.IsEmpty(component.FindAll(".bit-aicms-edt"));
        Assert.AreEqual("hello", component.Find(".bit-aicms-bbl").TextContent);
        Assert.AreEqual("hello", message.GetText());
    }

    // The field takes the focus as the editor opens, and the Edit button the edit was started from takes it back as the
    // editor goes, rather than leaving it to drop to the page.
    [TestMethod]
    public void TheEditorTakesTheFocusAndHandsItBackToEdit()
    {
        var session = new BitAiChatSession(new SilentTransport());
        var message = session.Conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.Session, session));

        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);

        component.Find("button[title='Edit']").Click();

        component.WaitForAssertion(() => Assert.ContainsSingle(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]));

        component.FindAll(".bit-aicms-eda button")[0].Click();

        component.WaitForAssertion(() => Assert.HasCount(2, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]));

        var edit = component.FindComponents<BitButton>().Single(b => b.Instance.IconName == "Edit");
        var focused = (ElementReference)Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"][1].Arguments[0]!;
        Assert.AreEqual(edit.Instance.RootElement.Id, focused.Id);
    }

    // A plain Escape in the editor cancels the edit, and is claimed while it does, so a dialog around the message stays
    // open; a modified one, or one in a disabled message, is left to what is around it.
    [TestMethod]
    public void EscapeCancelsTheEditAndIsClaimedWhileItDoes()
    {
        var session = new BitAiChatSession(new SilentTransport());
        var message = session.Conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.Session, session));

        component.Find("button[title='Edit']").Click();
        component.Find(".bit-aicms-edt textarea").Input("changed");

        Assert.AreEqual("claim", component.Find(".bit-aicms-edt").GetAttribute("data-bit-esc"));

        component.Find(".bit-aicms-edt").KeyDown(new KeyboardEventArgs { Key = "Escape", ShiftKey = true });

        Assert.ContainsSingle(component.FindAll(".bit-aicms-edt"));

        component.Render(p => p.Add(c => c.Disabled, true));

        Assert.IsFalse(component.Find(".bit-aicms-edt").HasAttribute("data-bit-esc"));

        component.Find(".bit-aicms-edt").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.ContainsSingle(component.FindAll(".bit-aicms-edt"));

        component.Render(p => p.Add(c => c.Disabled, false));
        component.Find(".bit-aicms-edt").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsEmpty(component.FindAll(".bit-aicms-edt"));
        Assert.AreEqual("hello", component.Find(".bit-aicms-bbl").TextContent);
        Assert.AreEqual("hello", message.GetText());
    }

    // An edit starts a turn of its own, which the session takes only while it is free: Edit is unavailable while a reply
    // is being generated, and an editor already open keeps its draft and saves nothing until the turn is over.
    [TestMethod]
    public async Task AQuestionIsEditedOnlyWhileTheSessionIsFree()
    {
        await using var session = new BitAiChatSession(new BitAiHangingTransport(), options: new BitAiChatSessionOptions { Pacing = null });
        var question = session.Conversation.AddMessage(BitAiRole.User, "hello");
        var other = session.Conversation.AddMessage(BitAiRole.User, "and another thing");

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, question)
            .Add(c => c.Session, session));
        var idle = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, other)
            .Add(c => c.Session, session));

        component.Find("button[title='Edit']").Click();
        component.Find(".bit-aicms-edt textarea").Input("hello again");

        var turn = session.SubmitAsync("one more question", cancellationToken: TestContext.CancellationToken);

        component.WaitForAssertion(() => Assert.AreEqual("true", component.FindAll(".bit-aicms-eda button")[1].GetAttribute("aria-disabled")), TimeSpan.FromSeconds(2));
        idle.WaitForAssertion(() => Assert.AreEqual("true", idle.Find("button[title='Edit']").GetAttribute("aria-disabled")), TimeSpan.FromSeconds(2));

        component.FindAll(".bit-aicms-eda button")[1].Click();
        idle.Find("button[title='Edit']").Click();

        var editor = component.Find(".bit-aicms-edt textarea");
        Assert.AreEqual("hello again", editor.GetAttribute("value") ?? editor.TextContent);
        Assert.AreEqual("hello", question.GetText());
        Assert.IsEmpty(idle.FindAll(".bit-aicms-edt"));

        session.Stop();
        await turn;

        component.WaitForAssertion(() => Assert.IsNull(component.FindAll(".bit-aicms-eda button")[1].GetAttribute("aria-disabled")), TimeSpan.FromSeconds(2));
        idle.WaitForAssertion(() => Assert.IsNull(idle.Find("button[title='Edit']").GetAttribute("aria-disabled")), TimeSpan.FromSeconds(2));
    }

    // A draft of nothing but spaces would send an empty question and get a reply to nothing.
    [TestMethod]
    public void ABlankEditIsNotSent()
    {
        var session = new BitAiChatSession(new SilentTransport(), options: new BitAiChatSessionOptions { Pacing = null });
        var message = session.Conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.Session, session));

        component.Find("button[title='Edit']").Click();
        component.Find(".bit-aicms-edt textarea").Input("   ");

        var save = component.FindAll(".bit-aicms-eda button")[1];
        Assert.AreEqual("true", save.GetAttribute("aria-disabled"));

        save.Click();

        Assert.ContainsSingle(component.FindAll(".bit-aicms-edt"));
        Assert.AreEqual("hello", message.GetText());
        Assert.ContainsSingle(session.Conversation.Messages);
    }

    // Attachments sent on their own have no words to put in a bubble, or to copy; they can still be edited, to add some.
    [TestMethod]
    public void AttachmentsSentOnTheirOwnHaveNoBubbleAndNothingToCopy()
    {
        var session = new BitAiChatSession(new SilentTransport());
        var message = new BitAiMessage(BitAiRole.User);
        message.AddAttachment(new BitAiAttachment("a1", "report.pdf"));

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.Session, session));

        Assert.ContainsSingle(component.FindComponents<BitAiAttachmentChip>());
        Assert.IsEmpty(component.FindAll(".bit-aicms-bbl"));
        Assert.IsEmpty(component.FindComponents<BitAiCopyButton>());

        // With nothing typed, the attachments may still go on alone.
        component.Find("button[title='Edit']").Click();

        Assert.IsNull(component.FindAll(".bit-aicms-eda button")[1].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void ADisabledMessageDisablesItsActions()
    {
        var session = new BitAiChatSession(new SilentTransport());

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Session, session)
            .Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find("article").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", component.Find("button[title='Edit']").GetAttribute("aria-disabled"));

        var assistant = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.Session, session)
            .Add(c => c.Disabled, true));

        Assert.IsTrue(assistant.FindComponent<BitAiMessageActions>().Instance.Disabled);
    }

    [TestMethod]
    public void FeedbackOnTheMessageReachesTheHost()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        BitAiMessageFeedback? received = null;

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.OnFeedback, feedback => received = feedback));

        component.Find("button[title='Bad response']").Click();

        Assert.IsNotNull(received);
        Assert.AreSame(message, received.Message);
        Assert.AreEqual(BitAiFeedbackKind.Negative, received.Feedback);
    }

    [TestMethod]
    public void TheAppsActionsAreAppendedToTheActionsOfEveryMessage()
    {
        RenderFragment<BitAiMessage> actions = m => builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "app-action");
            builder.AddContent(2, m.Role.Value);
            builder.CloseElement();
        };

        var assistant = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.MessageActionsTemplate, actions));

        Assert.AreEqual("assistant", assistant.Find(".bit-aimac-act .app-action").TextContent);

        var user = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Session, new BitAiChatSession(new BitAiFakeTransport()))
            .Add(c => c.MessageActionsTemplate, actions));

        Assert.AreEqual("user", user.Find(".bit-aicms-act .app-action").TextContent);
    }

    [TestMethod]
    public void TheFollowUpsOfTheNewestReplyAreOfferedOnceItHasFinished()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        message.SetStatus(BitAiMessageStatus.Streaming);
        message.SetFollowUps(["Show more", "Undo it"]);
        string? picked = null;

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.OnFollowUp, text => picked = text));

        Assert.IsEmpty(component.FindAll(".bit-aicms-fup"));

        message.SetStatus(BitAiMessageStatus.Complete);

        component.WaitForAssertion(() => Assert.ContainsSingle(component.FindAll(".bit-aicms-fup")));
        var group = component.Find(".bit-aicms-fup");
        Assert.AreEqual("group", group.GetAttribute("role"));
        Assert.AreEqual(BitAiStrings.Default.FollowUps, group.GetAttribute("aria-label"));

        var buttons = group.QuerySelectorAll("button");
        Assert.AreEqual(2, buttons.Length);
        Assert.AreEqual("Undo it", buttons[1].TextContent.Trim());

        component.FindAll(".bit-aicms-fup button")[1].Click();
        Assert.AreEqual("Undo it", picked);
    }

    [TestMethod]
    public void AnOlderReplyOffersNoFollowUps()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        message.SetFollowUps(["Show more"]);

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.IsLatest, false)
            .Add(c => c.OnFollowUp, _ => { }));

        Assert.IsEmpty(component.FindAll(".bit-aicms-fup"));
    }

    [TestMethod]
    public async Task AFollowUpGoesToTheSessionWhenTheHostDoesNotHandleIt()
    {
        var transport = new BitAiFakeTransport(new BitAiTextDeltaEvent("more"), new BitAiMessageCompletedEvent());
        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null });
        session.Conversation.AddMessage(BitAiRole.User, "go");
        var reply = session.Conversation.AddMessage(BitAiRole.Assistant, "answer");
        reply.SetFollowUps(["Show more"]);

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, reply)
            .Add(c => c.Session, session));

        component.Find(".bit-aicms-fup button").Click();

        await WaitForAsync(() => session.Conversation.Messages.Count == 4 && session.IsGenerating is false);
        Assert.AreEqual("Show more", session.Conversation.Messages[2].GetText());
    }

    [TestMethod]
    public void AReplyWhoseOnlyToolCallsAreHiddenStillShowsItIsThinking()
    {
        var call = new BitAiToolCallPart("c1", "lookup");
        var message = Assistant(call);
        message.SetStatus(BitAiMessageStatus.Streaming);

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .AddCascadingValue(new BitAiUiOptions { ToolCallDisplay = _ => BitAiToolCallDisplay.Hidden }));

        Assert.ContainsSingle(component.FindComponents<BitAiThinkingIndicator>());
        Assert.IsEmpty(component.FindAll(".bit-aitcv"));

        // A failure is the reader's to see whatever the options say, so the reply has something to show.
        call.SetResult(BitAiToolOutcome.Failure(new BitAiError("down")));

        component.WaitForAssertion(() => Assert.IsEmpty(component.FindComponents<BitAiThinkingIndicator>()));
        Assert.ContainsSingle(component.FindAll(".bit-aitcv"));
    }

    [TestMethod]
    public async Task FollowUpsWaitForTheTurnInFlightToEnd()
    {
        var linger = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var session = new BitAiChatSession(new LingeringTransport(linger.Task), null, new() { Pacing = null });

        var turn = session.SubmitAsync("go", cancellationToken: TestContext.CancellationToken);
        await WaitForAsync(() => session.Conversation.Messages.Count == 2
                                 && session.Conversation.Messages[1] is { IsBusy: false, FollowUps: not null });

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, session.Conversation.Messages[1])
            .Add(c => c.Session, session));

        // The reply has finished, but the stream is still open: picking a follow-up now would go nowhere.
        Assert.IsTrue(session.IsGenerating);
        Assert.IsEmpty(component.FindAll(".bit-aicms-fup"));

        linger.SetResult();
        await turn;

        component.WaitForAssertion(() => Assert.ContainsSingle(component.FindAll(".bit-aicms-fup")), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void AToolCallTheOptionsHideLeavesNoPartViewBehind()
    {
        var message = Assistant(new BitAiTextPart("answer"), new BitAiToolCallPart("c1", "lookup"));

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, message)
            .AddCascadingValue(new BitAiUiOptions { ToolCallDisplay = _ => BitAiToolCallDisplay.Hidden }));

        Assert.ContainsSingle(component.FindAll(".bit-aimpv"));
        Assert.IsEmpty(component.FindAll(".bit-aitcv"));
    }

    /// <summary>Answers at once, then keeps the stream open until it is let go.</summary>
    private sealed class LingeringTransport(Task linger) : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiTextDeltaEvent("answer");
            yield return new BitAiFollowUpsEvent(["Show more"]);
            yield return new BitAiMessageCompletedEvent();

            await linger.WaitAsync(cancellationToken);
        }
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5);
        Assert.IsTrue(condition(), "The expected condition was not reached in time.");
    }

    [TestMethod]
    public async Task BitAiChatMessageShouldStopListeningToTheMessageWhenDisposed()
    {
        var part = new BitAiTextPart("x");
        var message = Assistant(part);

        RenderComponent<BitAiChatMessage>(p => p.Add(c => c.Message, message));

        await Context.DisposeComponentsAsync();

        // A change after the disposal reaches no component.
        message.SetStatus(BitAiMessageStatus.Complete);
    }

    [TestMethod]
    public void BitAiChatMessageShouldRespectClassesAndStyles()
    {
        var session = new BitAiChatSession(new SilentTransport());

        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Session, session)
            .Add(c => c.Classes, new() { Root = "custom-root", Bubble = "custom-bubble", Actions = "custom-actions", EditButton = new() { Root = "custom-edit" } })
            .Add(c => c.Styles, new() { Root = "color: red;", Bubble = "color: green;", Actions = "color: blue;" }));

        Assert.IsTrue(component.Find(".bit-aicms").ClassList.Contains("custom-root"));
        Assert.Contains("color: red;", component.Find(".bit-aicms").GetAttribute("style")!);
        Assert.IsTrue(component.Find(".bit-aicms-bbl").ClassList.Contains("custom-bubble"));
        Assert.AreEqual("color: green;", component.Find(".bit-aicms-bbl").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aicms-act").ClassList.Contains("custom-actions"));
        Assert.AreEqual("color: blue;", component.Find(".bit-aicms-act").GetAttribute("style"));
        Assert.IsTrue(component.Find("button[title='Edit']").ClassList.Contains("custom-edit"));
    }

    [TestMethod]
    public void BitAiChatMessageShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: tomato;"));

        var root = component.Find(".bit-aicms");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.Contains("color: tomato;", root.GetAttribute("style")!);
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiChatMessageShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aicms").GetAttribute("style");

        if (expectedStyle.Length == 0)
        {
            Assert.IsTrue(style is null || style.Contains("visibility:hidden") is false);
        }
        else
        {
            Assert.Contains(expectedStyle, style!);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "bit-rtl")]
    [DataRow(BitDir.Ltr, null)]
    public void BitAiChatMessageShouldRespectDir(BitDir dir, string? expectedClass)
    {
        var component = RenderComponent<BitAiChatMessage>(p => p
            .Add(c => c.Message, User("hello"))
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aicms");

        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));
        Assert.AreEqual(expectedClass is not null, root.ClassList.Contains("bit-rtl"));
    }



    private sealed class SilentTransport : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiMessageStartedEvent();
            yield return new BitAiMessageCompletedEvent();
            await Task.CompletedTask;
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
