using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ChatMessage;

/// <summary>
/// Covers the BitParams cascade of the BitAiChatMessage: what a BitAiChatMessageParams fills in, and what it leaves alone
/// because the message set it for itself.
/// </summary>
[TestClass]
public class BitAiChatMessageParamsTests : BitAiTestBase
{
    // What makes one message the one it is.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiChatMessage.CascadingParameters),
        nameof(BitAiChatMessage.Animate),
        nameof(BitAiChatMessage.ChildContent),
        nameof(BitAiChatMessage.Conversation),
        nameof(BitAiChatMessage.IsLatest),
        nameof(BitAiChatMessage.Message),
        nameof(BitAiChatMessage.OnCitationClick),
        nameof(BitAiChatMessage.OnFeedback),
        nameof(BitAiChatMessage.OnSourceClick),
        nameof(BitAiChatMessage.Session),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiChatMessageParams messageParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        var message = new BitAiMessage(BitAiRole.User);
        message.AddPart(new BitAiTextPart("hello"));

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { messageParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiChatMessage>(0);
                builder.AddAttribute(1, nameof(BitAiChatMessage.Message), message);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiChatMessageParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiChatMessage", BitAiChatMessageParams.ParamName);
        Assert.AreEqual(BitAiChatMessageParams.ParamName, new BitAiChatMessageParams().Name);
    }

    [TestMethod]
    public void BitAiChatMessageParamsShouldCarryEveryParameterThatBelongsToAGroupOfMessages()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiChatMessage).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                 .Select(p => p.Name)
                                                 .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                 .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiChatMessageParams).GetProperty(name), $"BitAiChatMessageParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiChatMessageShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiChatMessageParams
        {
            Class = "cascaded",
            Classes = new() { Bubble = "cascaded-bubble" },
            Styles = new() { Bubble = "color: red;" },
        });

        Assert.IsTrue(component.Find(".bit-aicms").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aicms-bbl").ClassList.Contains("cascaded-bubble"));
        Assert.AreEqual("color: red;", component.Find(".bit-aicms-bbl").GetAttribute("style"));
    }

    [TestMethod]
    public void BitAiChatMessageShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiChatMessageParams
        {
            Class = "cascaded",
            Classes = new() { Bubble = "cascaded-bubble" },
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitAiChatMessage.Classes), new BitAiChatMessageClassStyles { Bubble = "own-bubble" });
        });

        Assert.IsTrue(component.Find(".bit-aicms").ClassList.Contains("own"));
        Assert.IsFalse(component.Find(".bit-aicms").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aicms-bbl").ClassList.Contains("own-bubble"));
        Assert.IsFalse(component.Find(".bit-aicms-bbl").ClassList.Contains("cascaded-bubble"));
    }
}
