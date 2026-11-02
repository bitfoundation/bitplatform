using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessageActions;

/// <summary>
/// Covers the BitParams cascade of the BitAiMessageActions: what a BitAiMessageActionsParams fills in, and what it leaves
/// alone because the row set it for itself.
/// </summary>
[TestClass]
public class BitAiMessageActionsParamsTests : BitAiTestBase
{
    // What makes one row the one it is.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiMessageActions.CascadingParameters),
        nameof(BitAiMessageActions.CanRegenerate),
        nameof(BitAiMessageActions.ExtraActionsTemplate),
        nameof(BitAiMessageActions.Message),
        nameof(BitAiMessageActions.OnFeedback),
        nameof(BitAiMessageActions.Session),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiMessageActionsParams actionsParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        var message = Assistant(new BitAiTextPart("answer"));

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { actionsParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiMessageActions>(0);
                builder.AddAttribute(1, nameof(BitAiMessageActions.Message), message);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiMessageActionsParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiMessageActions", BitAiMessageActionsParams.ParamName);
        Assert.AreEqual(BitAiMessageActionsParams.ParamName, new BitAiMessageActionsParams().Name);
    }

    [TestMethod]
    public void BitAiMessageActionsParamsShouldCarryEveryParameterThatBelongsToAGroupOfRows()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiMessageActions).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                    .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                    .Select(p => p.Name)
                                                    .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                    .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiMessageActionsParams).GetProperty(name), $"BitAiMessageActionsParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiMessageActionsShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiMessageActionsParams
        {
            Class = "cascaded",
            Pinned = true,
            ShowFeedback = false,
            Classes = new() { Actions = "cascaded-actions" },
        });

        var root = component.Find(".bit-aimac");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-aimac-pin"));
        Assert.IsTrue(component.Find(".bit-aimac-act").ClassList.Contains("cascaded-actions"));
        Assert.IsEmpty(component.FindAll("button[aria-pressed]"));
    }

    [TestMethod]
    public void BitAiMessageActionsShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiMessageActionsParams
        {
            Class = "cascaded",
            Pinned = true,
            ShowFeedback = false,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitAiMessageActions.Pinned), false);
            builder.AddAttribute(4, nameof(BitAiMessageActions.ShowFeedback), true);
        });

        var root = component.Find(".bit-aimac");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("bit-aimac-pin"));
        Assert.HasCount(2, component.FindAll("button[aria-pressed]"));
    }
}
