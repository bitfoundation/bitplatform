using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ConversationList;

/// <summary>
/// Covers the BitParams cascade of the BitAiConversationList: what a BitAiConversationListParams fills in, and what it
/// leaves alone because the list set it for itself.
/// </summary>
[TestClass]
public class BitAiConversationListParamsTests : BitAiTestBase
{
    // What makes one list the one it is: its conversations, the one that is open, and its callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiConversationList.CascadingParameters),
        nameof(BitAiConversationList.Conversations),
        nameof(BitAiConversationList.OnDelete),
        nameof(BitAiConversationList.OnNew),
        nameof(BitAiConversationList.OnSelect),
        nameof(BitAiConversationList.SelectedId),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiConversationListParams listParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { listParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiConversationList>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiConversationListParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiConversationList", BitAiConversationListParams.ParamName);
        Assert.AreEqual(BitAiConversationListParams.ParamName, new BitAiConversationListParams().Name);
    }

    [TestMethod]
    public void BitAiConversationListParamsShouldCarryEveryParameterThatBelongsToAGroupOfLists()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiConversationList).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                      .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                      .Select(p => p.Name)
                                                      .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                      .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiConversationListParams).GetProperty(name), $"BitAiConversationListParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiConversationListShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiConversationListParams
        {
            Class = "cascaded",
            ShowNewButton = false,
        });

        Assert.IsTrue(component.Find(".bit-aicvl").ClassList.Contains("cascaded"));
        Assert.IsEmpty(component.FindAll(".bit-acb"));
    }

    [TestMethod]
    public void BitAiConversationListShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiConversationListParams
        {
            Class = "cascaded",
            ShowNewButton = false,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(2, nameof(BitAiConversationList.ShowNewButton), true);
        });

        Assert.IsTrue(component.Find(".bit-aicvl").ClassList.Contains("own"));
        Assert.IsFalse(component.Find(".bit-aicvl").ClassList.Contains("cascaded"));
        Assert.HasCount(1, component.FindAll(".bit-acb"));
    }
}
