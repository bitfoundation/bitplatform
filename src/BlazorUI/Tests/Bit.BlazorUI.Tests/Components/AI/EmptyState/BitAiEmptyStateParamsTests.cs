using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.EmptyState;

/// <summary>
/// Covers the BitParams cascade of the BitAiEmptyState: what a BitAiEmptyStateParams fills in, and what it leaves alone
/// because the empty state set it for itself.
/// </summary>
[TestClass]
public class BitAiEmptyStateParamsTests : BitAiTestBase
{
    // What makes one empty state the one it is.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiEmptyState.CascadingParameters),
        nameof(BitAiEmptyState.ChildContent),
        nameof(BitAiEmptyState.IconTemplate),
        nameof(BitAiEmptyState.OnSuggestion),
        nameof(BitAiEmptyState.Suggestions),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiEmptyStateParams emptyParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { emptyParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiEmptyState>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiEmptyStateParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiEmptyState", BitAiEmptyStateParams.ParamName);
        Assert.AreEqual(BitAiEmptyStateParams.ParamName, new BitAiEmptyStateParams().Name);
    }

    [TestMethod]
    public void BitAiEmptyStateParamsShouldCarryEveryParameterThatBelongsToAGroupOfEmptyStates()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiEmptyState).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                .Select(p => p.Name)
                                                .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiEmptyStateParams).GetProperty(name), $"BitAiEmptyStateParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiEmptyStateShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiEmptyStateParams
        {
            Class = "cascaded",
            Title = "Welcome",
            Body = "Ask away.",
            IconName = "Robot",
        });

        Assert.IsTrue(component.Find(".bit-aiems").ClassList.Contains("cascaded"));
        Assert.AreEqual("Welcome", component.Find(".bit-aiems-ttl").TextContent);
        Assert.AreEqual("Ask away.", component.Find(".bit-aiems-bdy").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-aiems-ico").ClassList.Contains("bit-icon--Robot"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiEmptyStateParams
        {
            Class = "cascaded",
            Title = "Welcome",
            IconName = "Robot",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(2, nameof(BitAiEmptyState.Title), "Hello");
            builder.AddAttribute(3, nameof(BitAiEmptyState.IconName), "ChatBot");
        });

        Assert.IsTrue(component.Find(".bit-aiems").ClassList.Contains("own"));
        Assert.IsFalse(component.Find(".bit-aiems").ClassList.Contains("cascaded"));
        Assert.AreEqual("Hello", component.Find(".bit-aiems-ttl").TextContent);
        Assert.IsTrue(component.Find(".bit-aiems-ico").ClassList.Contains("bit-icon--ChatBot"));
    }
}
