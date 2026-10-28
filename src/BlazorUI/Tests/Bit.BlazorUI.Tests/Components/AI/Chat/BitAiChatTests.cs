using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests.Components.AI.Core;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Chat;

[TestClass]
public class BitAiChatTests : BitAiTestBase
{
    private static BitAiChatSession NewSession() =>
        new(new SilentTransport(), options: new BitAiChatSessionOptions { Pacing = null });

    [TestMethod]
    public async Task BitAiChatShouldComposeTheTranscriptTheEmptyStateAndTheComposer()
    {
        Interactive();
        var session = NewSession();

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, session));

        Assert.IsNotNull(component.Find(".bit-aicht > .bit-aimsl"));
        Assert.IsNotNull(component.Find(".bit-aimsl .bit-aiems"));
        Assert.IsNotNull(component.Find(".bit-aicht > .bit-aicht-ftr > .bit-aicht-ftc"));
        Assert.HasCount(1, component.FindComponents<BitAiPromptInput>());

        // Pinned to the newest message, so there is nowhere for the scroll button to go.
        Assert.IsEmpty(component.FindAll(".bit-aistl"));

        await session.DisposeAsync();
    }

    // A renderer that does not say whether it is an interactive one - bUnit's until it is told, a host of the app's
    // own - fails no transcript: the JS runtime answers for it, an interactive one here, so a new message enters.
    [TestMethod]
    public async Task BitAiChatShouldRenderOnARendererThatDoesNotSayWhetherItIsInteractive()
    {
        var session = NewSession();
        session.Conversation.AddMessage(BitAiRole.User, "q");

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, session));

        Assert.HasCount(1, component.FindAll("article.bit-aicms"));

        var added = session.Conversation.AddMessage(BitAiRole.Assistant, "a");

        component.WaitForAssertion(() =>
            Assert.AreEqual(added.MessageId, component.Find("article.bit-aicms-ent").GetAttribute("data-message-id")));

        await session.DisposeAsync();
    }

    [TestMethod]
    public async Task BitAiChatShouldRenderTheMessagesOfItsSession()
    {
        Interactive();
        var session = NewSession();
        session.Conversation.AddMessage(BitAiRole.User, "q");
        session.Conversation.AddMessage(BitAiRole.Assistant, "a");

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, session));

        Assert.HasCount(2, component.FindAll("article.bit-aicms"));
        Assert.IsEmpty(component.FindAll(".bit-aiems"));
        Assert.AreSame(session, component.Instance.ActiveSession);
        Assert.AreSame(session.Conversation, component.Instance.ActiveConversation);

        await session.DisposeAsync();
    }

    // Ported from ChatBehaviourTests.FeedbackReachesTheHostWithTheMessage.
    [TestMethod]
    public async Task FeedbackReachesTheHostWithTheMessage()
    {
        Interactive();
        var session = NewSession();
        session.Conversation.AddMessage(BitAiRole.User, "q");
        var reply = session.Conversation.AddMessage(BitAiRole.Assistant, "a");

        BitAiMessageFeedback? received = null;
        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.OnFeedback, feedback => received = feedback));

        component.Find("button[title='Good response']").Click();

        Assert.IsNotNull(received);
        Assert.AreSame(reply, received.Message);
        Assert.AreEqual(BitAiFeedbackKind.Positive, received.Feedback);
        Assert.AreEqual(BitAiFeedbackKind.Positive, reply.Feedback);

        await session.DisposeAsync();
    }

    [TestMethod]
    public async Task ASuggestionIsSubmittedToTheHostWhenItHandlesTheSubmission()
    {
        Interactive();
        var session = NewSession();
        var submitted = new List<string>();

        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.Suggestions, ["Plan a trip"])
            .Add(c => c.OnSubmit, s => submitted.Add(s.Text)));

        component.Find(".bit-aiems-sgs > button").Click();

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Plan a trip" }, submitted));
        Assert.IsEmpty(session.Conversation.Messages);

        await session.DisposeAsync();
    }

    // The suggestion goes with the empty state as soon as its message is in, and the focus would go with it to the page:
    // the composer takes it before the turn, which this transport never ends, is submitted.
    [TestMethod]
    public async Task ASuggestionHandsTheFocusToTheComposerBeforeItIsSubmitted()
    {
        Interactive();
        await using var session = new BitAiChatSession(new BitAiHangingTransport(), options: new BitAiChatSessionOptions { Pacing = null });

        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.Suggestions, ["Plan a trip"]));

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.AiPromptInput.focus"]);

        component.Find(".bit-aiems-sgs > button").Click();

        component.WaitForAssertion(() =>
        {
            Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.AiPromptInput.focus"]);
            Assert.IsTrue(session.IsGenerating);
        });

        session.Stop();
    }

    [TestMethod]
    public async Task ASuggestionIsSubmittedToTheSessionOtherwise()
    {
        Interactive();
        var session = NewSession();

        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.Suggestions, ["Plan a trip"]));

        component.Find(".bit-aiems-sgs > button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("Plan a trip", session.Conversation.Messages[0].GetText()));

        await session.DisposeAsync();
    }

    [TestMethod]
    public async Task TheEmptyStateIsWordedByTheChat()
    {
        Interactive();
        var session = NewSession();

        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.EmptyTitle, "Hi there")
            .Add(c => c.EmptyBody, "Ask me about cats."));

        Assert.AreEqual("Hi there", component.Find(".bit-aiems-ttl").TextContent);
        Assert.AreEqual("Ask me about cats.", component.Find(".bit-aiems-bdy").TextContent.Trim());

        component.Render(p => p.Add(c => c.EmptyTemplate, b => b.AddMarkupContent(0, "<p class=\"own-empty\"></p>")));

        Assert.IsNotNull(component.Find(".own-empty"));
        Assert.IsEmpty(component.FindAll(".bit-aiems"));

        await session.DisposeAsync();
    }

    [TestMethod]
    public async Task TheScrollButtonAppearsOnceTheReaderScrollsAway()
    {
        Interactive();
        var session = NewSession();
        session.Conversation.AddMessage(BitAiRole.User, "q");

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, session));
        var list = component.FindComponent<BitAiMessageList>();

        await list.InvokeAsync(() => list.Instance._OnPinnedChange(false));

        component.WaitForAssertion(() => Assert.IsNotNull(component.Find(".bit-aicht-ftr > .bit-aistl")));

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.AiPromptInput.focus"]);

        component.Find(".bit-aistl button").Click();

        component.WaitForAssertion(() => Assert.IsEmpty(component.FindAll(".bit-aistl")));
        Assert.IsTrue(list.Instance.IsPinned);

        // The button is gone, and the focus with it: the composer takes it rather than the page.
        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.AiPromptInput.focus"]));

        await session.DisposeAsync();
    }

    [TestMethod]
    public async Task TheChatRendersAsATurnStartsAndEndsRatherThanOnEveryEventOfTheSession()
    {
        Interactive();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new BitAiChatSession(new GatedTransport(gate), options: new BitAiChatSessionOptions { Pacing = null });

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, session));

        // A change of the session that leaves the turn where it was draws nothing again: a render of the chat would
        // re-render the transcript and every message in it, and a stream raises one per token.
        var renders = component.RenderCount;
        session.Stop();
        session.Stop();
        session.Stop();
        await Task.Delay(250);
        Assert.AreEqual(renders, component.RenderCount);

        // A turn starting and ending does: the composer offers Stop while it is in flight, and Send again after it.
        var turn = session.SubmitAsync("go");

        component.WaitForAssertion(() => Assert.IsNotNull(component.Find(".bit-aicht-ftc button[title='Stop']")));

        gate.SetResult();
        await turn;

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-aicht-ftc button[title='Stop']"));
            Assert.IsNotNull(component.Find(".bit-aicht-ftc button[title='Send']"));
        });

        await session.DisposeAsync();
    }

    [TestMethod]
    public async Task TheComposerCanBeReplacedAndTheChatFramed()
    {
        Interactive();
        var session = NewSession();

        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.HeaderTemplate, b => b.AddMarkupContent(0, "<header class=\"own-header\"></header>"))
            .Add(c => c.ComposerTemplate, b => b.AddMarkupContent(0, "<div class=\"own-composer\"></div>"))
            .Add(c => c.FooterTemplate, b => b.AddMarkupContent(0, "<small class=\"own-footer\"></small>")));

        Assert.IsNotNull(component.Find(".bit-aicht > header.own-header"));
        Assert.IsNotNull(component.Find(".bit-aicht-ftc > .own-composer"));
        Assert.IsNotNull(component.Find(".bit-aicht-ftc > .own-footer"));
        Assert.IsEmpty(component.FindComponents<BitAiPromptInput>());

        await session.DisposeAsync();
    }

    [TestMethod]
    public void BitAiChatShouldCreateASessionFromTheRegisteredTransport()
    {
        Services.AddSingleton<IBitAiTransport>(new SilentTransport());
        Interactive();

        var component = RenderComponent<BitAiChat>();

        Assert.IsNotNull(component.Instance.ActiveSession);
        Assert.IsNotNull(component.Find(".bit-aicht"));
    }

    [TestMethod]
    public void BitAiChatShouldCreateAnUnpacedSessionWhereSmoothStreamingIsOff()
    {
        Services.AddSingleton<IBitAiTransport>(new SilentTransport());
        Services.AddSingleton(new BitAiUiOptions { SmoothStreaming = false });
        Interactive();

        var component = RenderComponent<BitAiChat>();

        Assert.IsNull(component.Instance.ActiveSession.Options.Pacing);
    }

    [TestMethod]
    public void BitAiChatShouldCreateAPacedSessionByDefault()
    {
        Services.AddSingleton<IBitAiTransport>(new SilentTransport());
        Interactive();

        var component = RenderComponent<BitAiChat>();

        Assert.IsNotNull(component.Instance.ActiveSession.Options.Pacing);
    }

    [TestMethod]
    public async Task BitAiChatShouldEndTheSessionItCreatedOnceOneIsSupplied()
    {
        Services.AddSingleton<IBitAiTransport>(new SilentTransport());
        Interactive();

        var component = RenderComponent<BitAiChat>();
        var owned = component.Instance.ActiveSession;
        var supplied = NewSession();

        component.Render(p => p.Add(c => c.Session, supplied));

        Assert.AreSame(supplied, component.Instance.ActiveSession);
        // A disposed session starts no turn.
        Assert.IsNull(await owned.SubmitAsync("go"));

        component.Render(p => p.Add(c => c.Session, null));

        Assert.AreNotSame(owned, component.Instance.ActiveSession);
        Assert.AreNotSame(supplied, component.Instance.ActiveSession);

        await supplied.DisposeAsync();
    }

    [TestMethod]
    public void BitAiChatShouldPassItsDisabledToWhatItComposes()
    {
        Interactive();

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, NewSession()).Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find(".bit-aicht").ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.FindComponent<BitAiMessageList>().Instance.Disabled);
        Assert.IsTrue(component.FindComponent<BitAiEmptyState>().Instance.Disabled);
    }

    [TestMethod]
    public void BitAiChatShouldRespectClassesAndStyles()
    {
        Interactive();

        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, NewSession())
            .Add(c => c.Classes, new()
            {
                Root = "custom-root",
                Footer = "custom-footer",
                FooterColumn = "custom-column",
                MessageList = new() { Root = "custom-list" },
                EmptyState = new() { Root = "custom-empty" },
            })
            .Add(c => c.Styles, new() { Root = "color: red;", Footer = "color: green;", FooterColumn = "color: blue;" }));

        Assert.IsTrue(component.Find(".bit-aicht").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-aicht").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-aicht-ftr").ClassList.Contains("custom-footer"));
        Assert.AreEqual("color: green;", component.Find(".bit-aicht-ftr").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aicht-ftc").ClassList.Contains("custom-column"));
        Assert.AreEqual("color: blue;", component.Find(".bit-aicht-ftc").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aimsl").ClassList.Contains("custom-list"));
        Assert.IsTrue(component.Find(".bit-aiems").ClassList.Contains("custom-empty"));
    }

    [TestMethod]
    public void BitAiChatShouldRespectClassAndStyle()
    {
        Interactive();

        var component = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, NewSession())
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: tomato;"));

        var root = component.Find(".bit-aicht");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "color: tomato;");
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiChatShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        Interactive();

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, NewSession()).Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aicht").GetAttribute("style");

        if (expectedStyle.Length == 0)
        {
            Assert.IsTrue(style is null || style.Contains("visibility:hidden") is false);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "bit-rtl")]
    [DataRow(BitDir.Ltr, null)]
    public void BitAiChatShouldRespectDir(BitDir dir, string? expectedClass)
    {
        Interactive();

        var component = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, NewSession()).Add(c => c.Dir, dir));

        var root = component.Find(".bit-aicht");

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

    /// <summary>Starts a reply, then holds the turn in flight until the gate opens.</summary>
    private sealed class GatedTransport(TaskCompletionSource gate) : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiMessageStartedEvent();
            await gate.Task.WaitAsync(cancellationToken);
            yield return new BitAiTextDeltaEvent("answer");
            yield return new BitAiMessageCompletedEvent();
        }
    }
}
