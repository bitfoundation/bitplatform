using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.PromptInput;

/// <summary>
/// Covers the BitParams cascade of the composer: what a BitAiPromptInputParams fills in, what it leaves alone because
/// the composer wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitAiPromptInputParamsTests : BitAiTestBase
{
    // What belongs to one composer rather than to a group of them: its text, its session and its draft, the model it is
    // for, its templates and its callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiPromptInput.CascadingParameters),
        nameof(BitAiPromptInput.CascadedOptions),
        nameof(BitAiPromptInput.Capabilities),
        nameof(BitAiPromptInput.DraftKey),
        nameof(BitAiPromptInput.Generating),
        nameof(BitAiPromptInput.HintTemplate),
        nameof(BitAiPromptInput.Model),
        nameof(BitAiPromptInput.OnAttachmentRejected),
        nameof(BitAiPromptInput.OnStop),
        nameof(BitAiPromptInput.OnSubmit),
        nameof(BitAiPromptInput.PrefixTemplate),
        nameof(BitAiPromptInput.Session),
        nameof(BitAiPromptInput.SuffixTemplate),
        nameof(BitAiPromptInput.Value),
        nameof(BitAiPromptInput.ValueChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiPromptInputParams promptParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { promptParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiPromptInput>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiPromptInputParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiPromptInput", BitAiPromptInputParams.ParamName);
        Assert.AreEqual(BitAiPromptInputParams.ParamName, new BitAiPromptInputParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAiPromptInputParams());
    }

    [TestMethod]
    public void BitAiPromptInputParamsShouldCarryEveryParameterThatBelongsToAGroupOfComposers()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiPromptInput).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                 .Select(p => p.Name)
                                                 .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                 .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiPromptInputParams).GetProperty(name), $"BitAiPromptInputParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiPromptInputShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiPromptInputParams
        {
            Class = "cascaded",
            Placeholder = "Ask the team",
            MaxLength = 50,
            Accept = ".csv",
            AllowDictation = false,
            Classes = new() { Input = "cascaded-input" },
        });

        Assert.IsTrue(component.Find(".bit-aipri").ClassList.Contains("cascaded"));

        var textarea = component.Find("textarea");
        Assert.AreEqual("Ask the team", textarea.GetAttribute("placeholder"));
        Assert.AreEqual("50", textarea.GetAttribute("maxlength"));
        Assert.IsTrue(textarea.ClassList.Contains("cascaded-input"));
        Assert.AreEqual(".csv", component.Find("input[type=file]").GetAttribute("accept"));
    }

    [TestMethod]
    public void BitAiPromptInputShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiPromptInputParams { Placeholder = "Ask the team", AllowAttachments = false },
                                         builder =>
                                         {
                                             builder.AddAttribute(1, nameof(BitAiPromptInput.Placeholder), "Mine");
                                             builder.AddAttribute(2, nameof(BitAiPromptInput.AllowAttachments), true);
                                         });

        Assert.AreEqual("Mine", component.Find("textarea").GetAttribute("placeholder"));
        Assert.HasCount(1, component.FindAll("input[type=file]"));
    }

    [TestMethod]
    public void BitAiPromptInputShouldPutBackWhatACascadeStopsSupplying()
    {
        var component = RenderWithParams(new BitAiPromptInputParams { AllowAttachments = false });

        Assert.IsEmpty(component.FindAll("input[type=file]"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitAiPromptInputParams() });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiPromptInput>(0);
                builder.CloseComponent();
            });
        });

        Assert.HasCount(1, component.FindAll("input[type=file]"));
    }
}
