using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessageList;

/// <summary>
/// Covers the BitParams cascade of the BitAiMessageList: what a BitAiMessageListParams fills in, and what it leaves alone
/// because the list set it for itself.
/// </summary>
[TestClass]
public class BitAiMessageListParamsTests : BitAiTestBase
{
    // What makes one transcript the one it is: its conversation and session, its templates and its callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiMessageList.CascadingParameters),
        nameof(BitAiMessageList.CascadedOptions),
        nameof(BitAiMessageList.Conversation),
        nameof(BitAiMessageList.EmptyTemplate),
        nameof(BitAiMessageList.FooterTemplate),
        nameof(BitAiMessageList.HeaderTemplate),
        nameof(BitAiMessageList.MessageTemplate),
        nameof(BitAiMessageList.OnCitationClick),
        nameof(BitAiMessageList.OnFeedback),
        nameof(BitAiMessageList.OnPinnedChange),
        nameof(BitAiMessageList.OnSourceClick),
        nameof(BitAiMessageList.Session),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiMessageListParams listParams, BitAiConversation conversation, Action<RenderTreeBuilder>? extraAttributes = null)
    {
#if NET9_0_OR_GREATER
        Context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
#endif

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { listParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiMessageList>(0);
                builder.AddAttribute(1, nameof(BitAiMessageList.Conversation), conversation);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    private static BitAiConversation WithASystemMessage()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.System, "You are helpful.");
        conversation.AddMessage(BitAiRole.User, "hello");
        return conversation;
    }

    [TestMethod]
    public void BitAiMessageListParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiMessageList", BitAiMessageListParams.ParamName);
        Assert.AreEqual(BitAiMessageListParams.ParamName, new BitAiMessageListParams().Name);
    }

    [TestMethod]
    public void BitAiMessageListParamsShouldCarryEveryParameterThatBelongsToAGroupOfLists()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiMessageList).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                 .Select(p => p.Name)
                                                 .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                 .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiMessageListParams).GetProperty(name), $"BitAiMessageListParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiMessageListShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiMessageListParams
        {
            Class = "cascaded",
            HideSystemMessages = false,
            Classes = new() { Content = "cascaded-content" },
        }, WithASystemMessage());

        Assert.IsTrue(component.Find(".bit-aimsl").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aimsl-con").ClassList.Contains("cascaded-content"));
        Assert.HasCount(2, component.FindAll("article"));
    }

    [TestMethod]
    public void BitAiMessageListShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiMessageListParams
        {
            Class = "cascaded",
            HideSystemMessages = false,
        }, WithASystemMessage(), builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitAiMessageList.HideSystemMessages), true);
        });

        var root = component.Find(".bit-aimsl");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.HasCount(1, component.FindAll("article"));
    }
}
