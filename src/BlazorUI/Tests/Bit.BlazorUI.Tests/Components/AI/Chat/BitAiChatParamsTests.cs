using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Chat;

/// <summary>
/// Covers the BitParams cascade of the BitAiChat: what a BitAiChatParams fills in, and what it leaves alone because the
/// chat set it for itself.
/// </summary>
[TestClass]
public class BitAiChatParamsTests : BitAiTestBase
{
    // What makes one chat the one it is: its session and conversation, its templates and its callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiChat.CascadingParameters),
        nameof(BitAiChat.Commands),
        nameof(BitAiChat.ComposerHintTemplate),
        nameof(BitAiChat.ComposerPrefixTemplate),
        nameof(BitAiChat.ComposerSuffixTemplate),
        nameof(BitAiChat.ComposerTemplate),
        nameof(BitAiChat.Conversation),
        nameof(BitAiChat.EmptyTemplate),
        nameof(BitAiChat.FooterTemplate),
        nameof(BitAiChat.HeaderTemplate),
        nameof(BitAiChat.ListFooterTemplate),
        nameof(BitAiChat.ListHeaderTemplate),
        nameof(BitAiChat.Mentions),
        nameof(BitAiChat.MessageTemplate),
        nameof(BitAiChat.OnAttachmentRejected),
        nameof(BitAiChat.OnCitationClick),
        nameof(BitAiChat.OnFeedback),
        nameof(BitAiChat.OnSourceClick),
        nameof(BitAiChat.OnSubmit),
        nameof(BitAiChat.Session),
        nameof(BitAiChat.SessionOptions),
        nameof(BitAiChat.Suggestions),
        nameof(BitAiChat.UiOptions),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiChatParams chatParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        Context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var session = new BitAiChatSession(new SilentTransport());
        session.Conversation.AddMessage(BitAiRole.System, "You are helpful.");

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { chatParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiChat>(0);
                builder.AddAttribute(1, nameof(BitAiChat.Session), session);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiChatParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiChat", BitAiChatParams.ParamName);
        Assert.AreEqual(BitAiChatParams.ParamName, new BitAiChatParams().Name);
    }

    [TestMethod]
    public void BitAiChatParamsShouldCarryEveryParameterThatBelongsToAGroupOfChats()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiChat).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                          .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiChatParams).GetProperty(name), $"BitAiChatParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiChatShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiChatParams
        {
            Class = "cascaded",
            EmptyTitle = "Welcome",
            HideSystemMessages = false,
            AllowAttachments = false,
            Placeholder = "Ask",
        });

        Assert.IsTrue(component.Find(".bit-aicht").ClassList.Contains("cascaded"));
        Assert.HasCount(1, component.FindAll("article.bit-aicms-sys"));

        var chat = component.FindComponent<BitAiChat>().Instance;
        Assert.IsFalse(chat.AllowAttachments);
        Assert.AreEqual("Ask", chat.Placeholder);
        Assert.AreEqual("Welcome", chat.EmptyTitle);
    }

    [TestMethod]
    public void BitAiChatShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiChatParams
        {
            Class = "cascaded",
            HideSystemMessages = false,
            Placeholder = "Ask",
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitAiChat.HideSystemMessages), true);
            builder.AddAttribute(4, nameof(BitAiChat.Placeholder), "Type here");
        });

        Assert.IsTrue(component.Find(".bit-aicht").ClassList.Contains("own"));
        Assert.IsFalse(component.Find(".bit-aicht").ClassList.Contains("cascaded"));
        Assert.IsEmpty(component.FindAll("article"));
        Assert.AreEqual("Type here", component.FindComponent<BitAiChat>().Instance.Placeholder);
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
