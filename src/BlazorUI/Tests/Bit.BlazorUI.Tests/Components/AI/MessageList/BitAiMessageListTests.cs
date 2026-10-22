using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessageList;

[TestClass]
public class BitAiMessageListTests : BitAiTestBase
{
    [TestInitialize]
    public void Interactive()
    {
        // The list draws a bounded tail instead of virtualizing while it is prerendered, so the tests render it the way
        // an interactive circuit does.
#if NET9_0_OR_GREATER
        Context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
#endif
    }

    [TestMethod]
    public void BitAiMessageListShouldRenderTheTranscriptAsALogRegion()
    {
        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, new BitAiConversation()));

        var root = component.Find(".bit-aimsl");

        Assert.AreEqual("log", root.GetAttribute("role"));
        Assert.AreEqual("polite", root.GetAttribute("aria-live"));
        Assert.AreEqual("additions", root.GetAttribute("aria-relevant"));
        Assert.AreEqual("Conversation", root.GetAttribute("aria-label"));
        Assert.IsNotNull(component.Find(".bit-aimsl > .bit-aimsl-con"));
    }

    [TestMethod]
    public void BitAiMessageListShouldBeNamedByItsAriaLabel()
    {
        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, new BitAiConversation())
            .Add(c => c.AriaLabel, "Support chat"));

        Assert.AreEqual("Support chat", component.Find(".bit-aimsl").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAiMessageListShouldRenderEveryMessageAsAnArticle()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "question");
        conversation.AddMessage(BitAiRole.Assistant, "answer");

        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, conversation));

        var articles = component.FindAll(".bit-aimsl-con > article.bit-aicms");

        Assert.HasCount(2, articles);
        Assert.IsTrue(articles[0].ClassList.Contains("bit-aicms-usr"));
        Assert.IsTrue(articles[1].ClassList.Contains("bit-aicms-ast"));
    }

    [TestMethod]
    public void BitAiMessageListShouldRenderTheEmptyContentOnlyWhileThereAreNoMessages()
    {
        var conversation = new BitAiConversation();

        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, conversation)
            .Add(c => c.EmptyTemplate, b => b.AddMarkupContent(0, "<p class=\"empty\">nothing yet</p>")));

        Assert.IsNotNull(component.Find(".empty"));

        conversation.AddMessage(BitAiRole.User, "hello");

        component.WaitForAssertion(() => Assert.IsEmpty(component.FindAll(".empty")));
    }

    [TestMethod]
    public void BitAiMessageListShouldRenderTheHeaderAndTheFooterAroundTheMessages()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, conversation)
            .Add(c => c.HeaderTemplate, b => b.AddMarkupContent(0, "<div class=\"head\"></div>"))
            .Add(c => c.FooterTemplate, b => b.AddMarkupContent(0, "<div class=\"foot\"></div>")));

        var children = component.Find(".bit-aimsl-con").Children;

        Assert.IsTrue(children.First().ClassList.Contains("head"));
        Assert.IsTrue(children.Last().ClassList.Contains("foot"));
    }

    [TestMethod]
    public void BitAiMessageListShouldHideTheSystemMessagesUnlessAskedNotTo()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.System, "You are helpful.");
        conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, conversation));

        Assert.HasCount(1, component.FindAll("article"));

        component.Render(p => p.Add(c => c.HideSystemMessages, false));

        Assert.HasCount(2, component.FindAll("article"));
        Assert.IsNotNull(component.Find("article.bit-aicms-sys"));
    }

    // Ported from ChatBehaviourTests.OnlyTheNewestReplyOffersRegeneration.
    [TestMethod]
    public async Task OnlyTheNewestReplyOffersRegeneration()
    {
        var session = new BitAiChatSession(new SilentTransport(), options: new BitAiChatSessionOptions { Pacing = null });
        session.Conversation.AddMessage(BitAiRole.User, "one");
        session.Conversation.AddMessage(BitAiRole.Assistant, "first reply");
        session.Conversation.AddMessage(BitAiRole.User, "two");
        session.Conversation.AddMessage(BitAiRole.Assistant, "second reply");

        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, session.Conversation)
            .Add(c => c.Session, session));

        var regenerate = component.FindAll("button[title='Regenerate']");

        Assert.HasCount(1, regenerate);
        StringAssert.Contains(regenerate[0].Closest("article")!.TextContent, "second reply");

        await session.DisposeAsync();
    }

    // Ported from TranscriptAndInteropTests.RemovingAMessageDoesNotRecreateTemplatedMessagesAfterIt.
    [TestMethod]
    public void RemovingAMessageDoesNotRecreateTemplatedMessagesAfterIt()
    {
        var conversation = new BitAiConversation();
        var first = conversation.AddMessage(BitAiRole.User, "one");
        var second = conversation.AddMessage(BitAiRole.Assistant, "two");
        var third = conversation.AddMessage(BitAiRole.User, "three");

        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, conversation)
            .Add(c => c.MessageTemplate, message => builder =>
            {
                builder.OpenComponent<InstanceProbe>(0);
                builder.AddComponentParameter(1, nameof(InstanceProbe.Message), message);
                builder.CloseComponent();
            }));

        string InstanceOf(BitAiMessage message) =>
            component.Find($".probe[data-message='{message.MessageId}']").GetAttribute("data-instance")!;

        var secondInstance = InstanceOf(second);
        var thirdInstance = InstanceOf(third);

        conversation.RemoveMessage(first.MessageId);

        component.WaitForAssertion(() => Assert.HasCount(2, component.FindAll(".probe")));
        Assert.AreEqual(secondInstance, InstanceOf(second));
        Assert.AreEqual(thirdInstance, InstanceOf(third));
    }

    // Ported from TranscriptAndInteropTests.ATemplatedMessageAddsNoWrapperElement.
    [TestMethod]
    public void ATemplatedMessageAddsNoWrapperElement()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "one");

        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, conversation)
            .Add(c => c.MessageTemplate, message => builder =>
            {
                builder.OpenElement(0, "article");
                builder.AddContent(1, message.GetText());
                builder.CloseElement();
            }));

        Assert.AreEqual("ARTICLE", component.Find(".bit-aimsl-con").Children.Single().TagName);
    }

    // Ported from LifecycleAndSafetyTests.MessagesAddedFromABackgroundThreadAreRendered.
    [TestMethod]
    public async Task MessagesAddedFromABackgroundThreadAreRendered()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "first");

        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, conversation));

        await Task.Run(() =>
        {
            for (var i = 0; i < 20; i++) conversation.AddMessage(BitAiRole.User, $"message {i}");
        });

        component.WaitForAssertion(() => Assert.HasCount(21, component.FindAll("article.bit-aicms")));
    }

    // Ported from LifecycleAndSafetyTests.RemovingAMessageKeepsTheComponentsOfTheOthers.
    [TestMethod]
    public void RemovingAMessageKeepsTheComponentsOfTheOthers()
    {
        var conversation = new BitAiConversation();
        var first = conversation.AddMessage(BitAiRole.User, "one");
        var second = conversation.AddMessage(BitAiRole.User, "two");
        var third = conversation.AddMessage(BitAiRole.User, "three");

        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, conversation));
        var before = component.FindComponents<BitAiChatMessage>().Single(c => c.Instance.Message == third).Instance;

        conversation.RemoveMessage(second.MessageId);

        component.WaitForAssertion(() => Assert.HasCount(2, component.FindAll("article.bit-aicms")));
        var after = component.FindComponents<BitAiChatMessage>().Single(c => c.Instance.Message == third).Instance;
        Assert.AreSame(before, after);
        Assert.IsTrue(component.FindComponents<BitAiChatMessage>().Any(c => c.Instance.Message == first));
    }

    // The options are cascaded rather than registered: the services are already read by the time a test runs.
    private IRenderedComponent<CascadingValue<BitAiUiOptions>> RenderWithThreshold(BitAiConversation conversation, int threshold)
    {
        return RenderComponent<CascadingValue<BitAiUiOptions>>(p => p
            .Add(c => c.Value, BitAiUiOptions.Default with { VirtualizeThreshold = threshold })
            .AddChildContent<BitAiMessageList>(list => list.Add(c => c.Conversation, conversation)));
    }

    [TestMethod]
    public void BitAiMessageListShouldVirtualizePastTheThreshold()
    {
        var conversation = new BitAiConversation();
        for (var i = 0; i < 5; i++) conversation.AddMessage(BitAiRole.User, $"message {i}");

        var component = RenderWithThreshold(conversation, 3);

        Assert.HasCount(1, component.FindComponents<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<BitAiMessage>>());
    }

#if NET9_0_OR_GREATER
    // A static render is told apart through RendererInfo, which is .NET 9+; on 8 it is the JS runtime that says so.
    [TestMethod]
    public void BitAiMessageListShouldDrawABoundedTailWhilePrerendered()
    {
        Context.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var conversation = new BitAiConversation();
        for (var i = 0; i < 5; i++) conversation.AddMessage(BitAiRole.User, $"message {i}");

        var component = RenderWithThreshold(conversation, 3);

        var articles = component.FindAll("article.bit-aicms");

        Assert.HasCount(3, articles);
        StringAssert.Contains(articles[^1].TextContent, "message 4");
        Assert.IsEmpty(component.FindComponents<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<BitAiMessage>>());
    }
#endif

    [TestMethod]
    public void BitAiMessageListShouldObserveItsScrollAndStartAtTheNewestMessage()
    {
        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, new BitAiConversation()));

        var observe = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMessageList.observe");

        Assert.AreEqual(component.Instance.UniqueId, observe.Arguments[0]);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMessageList.scrollToBottom");
    }

    [TestMethod]
    public void TheListIsAtItsNewestMessageBeforeItsScrollIsObserved()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "question");
        conversation.AddMessage(BitAiRole.Assistant, "answer");

        RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, conversation));

        // Observed first, the script measured the top of a transcript about to be scrolled to its end and reported the
        // reader away from the newest message, then back at it, on every open.
        var calls = Context.JSInterop.Invocations.Select(i => i.Identifier).ToList();
        var scroll = calls.IndexOf("BitBlazorUI.AiMessageList.scrollToBottom");
        var observe = calls.IndexOf("BitBlazorUI.AiMessageList.observe");

        Assert.IsGreaterThanOrEqualTo(0, scroll);
        Assert.IsGreaterThan(scroll, observe);
        Assert.AreEqual(1, calls.Count(c => c == "BitBlazorUI.AiMessageList.observe"));
    }

    [TestMethod]
    public void TheScriptFollowsTheContentOnlyWhileAutoScrollIsOn()
    {
        var conversation = new BitAiConversation();

        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, BitAiUiOptions.Default with { AutoScroll = false })
            .AddChildContent<BitAiMessageList>(list => list.Add(c => c.Conversation, conversation)));

        var observe = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMessageList.observe");
        Assert.AreEqual(false, observe.Arguments[3]);
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.AiMessageList.setAutoScroll");

        // The options changing while the list is open reach the script, once per change of the option.
        provider.Render(p => p.Add(c => c.UiOptions, BitAiUiOptions.Default));
        provider.Render(p => p.Add(c => c.UiOptions, BitAiUiOptions.Default with { RenderInterval = TimeSpan.FromMilliseconds(5) }));

        var set = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMessageList.setAutoScroll");
        Assert.AreEqual(provider.FindComponent<BitAiMessageList>().Instance.UniqueId, set.Arguments[0]);
        Assert.AreEqual(true, set.Arguments[1]);
    }

    [TestMethod]
    public void AListOpenedWithAutoScrollTellsTheScriptToFollow()
    {
        RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, new BitAiConversation()));

        Assert.AreEqual(true, Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMessageList.observe").Arguments[3]);
    }

    [TestMethod]
    public void OnlyTheMessagesAddedAfterTheListOpenedPlayTheEntrance()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "restored question");
        conversation.AddMessage(BitAiRole.Assistant, "restored answer");

        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, conversation));

        // The history it opened on stays still through the renders that follow, its own and the app's.
        component.Render();
        component.Render(p => p.Add(c => c.Disabled, false));
        Assert.IsEmpty(component.FindAll("article.bit-aicms-ent"));

        var added = conversation.AddMessage(BitAiRole.User, "a new question");

        component.WaitForAssertion(() =>
        {
            var entering = component.FindAll("article.bit-aicms-ent");
            Assert.HasCount(1, entering);
            Assert.AreEqual(added.MessageId, entering[0].GetAttribute("data-message-id"));
        });

        // A new message keeps the class it entered with, so a later render neither replays nor drops the entrance.
        component.Render();
        Assert.HasCount(1, component.FindAll("article.bit-aicms-ent"));
        Assert.HasCount(3, component.FindAll("article.bit-aicms"));
    }

    [TestMethod]
    public void TheHistoryOfAnotherConversationDoesNotPlayTheEntrance()
    {
        var first = new BitAiConversation();
        var component = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, first));

        first.AddMessage(BitAiRole.User, "hello");
        component.WaitForAssertion(() => Assert.HasCount(1, component.FindAll("article.bit-aicms-ent")));

        var second = new BitAiConversation();
        second.AddMessage(BitAiRole.User, "restored");
        second.AddMessage(BitAiRole.Assistant, "restored too");

        component.Render(p => p.Add(c => c.Conversation, second));
        component.Render();

        Assert.HasCount(2, component.FindAll("article.bit-aicms"));
        Assert.IsEmpty(component.FindAll("article.bit-aicms-ent"));
    }

    [TestMethod]
    public async Task BitAiMessageListShouldReportWhetherTheReaderIsAtTheNewestMessage()
    {
        var reported = new List<bool>();

        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, new BitAiConversation())
            .Add(c => c.OnPinnedChange, pinned => reported.Add(pinned)));

        Assert.IsTrue(component.Instance.IsPinned);

        await component.InvokeAsync(() => component.Instance._OnPinnedChange(false));
        await component.InvokeAsync(() => component.Instance._OnPinnedChange(false));

        Assert.IsFalse(component.Instance.IsPinned);
        CollectionAssert.AreEqual(new[] { false }, reported);

        await component.InvokeAsync(() => component.Instance.ScrollToLatestAsync());

        Assert.IsTrue(component.Instance.IsPinned);
        CollectionAssert.AreEqual(new[] { false, true }, reported);
    }

    [TestMethod]
    public async Task BitAiMessageListShouldReleaseItsScrollObserverWhenDisposed()
    {
        var conversation = new BitAiConversation();

        RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, conversation));

        await Context.DisposeComponentsAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMessageList.dispose");

        // Nothing is listening to the conversation any more.
        conversation.AddMessage(BitAiRole.User, "after");
    }

    [TestMethod]
    public void BitAiMessageListShouldPassItsDisabledToTheMessages()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "hello");

        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, conversation)
            .Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find(".bit-aimsl").ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.FindComponents<BitAiChatMessage>().All(m => m.Instance.Disabled));
    }

    [TestMethod]
    public void BitAiMessageListShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, new BitAiConversation())
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: tomato;"));

        var root = component.Find(".bit-aimsl");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "color: tomato;");
    }

    [TestMethod]
    public void BitAiMessageListShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, new BitAiConversation())
            .Add(c => c.Classes, new() { Root = "custom-root", Content = "custom-content" })
            .Add(c => c.Styles, new() { Root = "color: red;", Content = "color: green;" }));

        Assert.IsTrue(component.Find(".bit-aimsl").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-aimsl").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-aimsl-con").ClassList.Contains("custom-content"));
        Assert.AreEqual("color: green;", component.Find(".bit-aimsl-con").GetAttribute("style"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiMessageListShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, new BitAiConversation())
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aimsl").GetAttribute("style");

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
    public void BitAiMessageListShouldRespectDir(BitDir dir, string? expectedClass)
    {
        var component = RenderComponent<BitAiMessageList>(p => p
            .Add(c => c.Conversation, new BitAiConversation())
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aimsl");

        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));
        Assert.AreEqual(expectedClass is not null, root.ClassList.Contains("bit-rtl"));
    }



    /// <summary>Renders which component instance is showing which message.</summary>
    private sealed class InstanceProbe : ComponentBase
    {
        private static int s_next;
        private readonly int _instance = Interlocked.Increment(ref s_next);

        [Parameter] public BitAiMessage Message { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "probe");
            builder.AddAttribute(2, "data-message", Message.MessageId);
            builder.AddAttribute(3, "data-instance", _instance);
            builder.CloseElement();
        }
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
}
