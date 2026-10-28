using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Form;

/// <summary>
/// Covers the BitParams cascade of the form: what a BitAiFormParams fills in, and what it leaves alone because the
/// form wrote it for itself.
/// </summary>
[TestClass]
public class BitAiFormParamsTests : BitAiTestBase
{
    // What belongs to a single form rather than to a group of them: its definition, its state and its callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiForm.CascadingParameters),
        nameof(BitAiForm.CascadedOptions),
        nameof(BitAiForm.Definition),
        nameof(BitAiForm.Busy),
        nameof(BitAiForm.Submitted),
        nameof(BitAiForm.OnCancel),
        nameof(BitAiForm.OnSubmit),
        nameof(BitAiForm.OnSubmitJson),
    ];

    private static readonly BitAiFormDefinition Definition = new() { Title = "Book", Fields = [new BitAiFormField("city")] };

    private IRenderedComponent<BitParams> RenderWithParams(BitAiFormParams formParams, string? ownClass = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { formParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiForm>(0);
                builder.AddAttribute(1, nameof(BitAiForm.Definition), Definition);
                if (ownClass is not null) builder.AddAttribute(2, nameof(BitAiForm.Class), ownClass);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiFormParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiForm", BitAiFormParams.ParamName);
        Assert.AreEqual(BitAiFormParams.ParamName, new BitAiFormParams().Name);
    }

    [TestMethod]
    public void BitAiFormParamsShouldCarryEveryParameterThatBelongsToAGroupOfForms()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiForm).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                          .ToList();

        Assert.IsTrue(parameters.Count > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiFormParams).GetProperty(name), $"BitAiFormParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiFormShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiFormParams
        {
            Class = "cascaded",
            Dir = BitDir.Rtl,
            Classes = new() { Title = "cascaded-title" },
            Styles = new() { Title = "color: red" },
        });

        var root = component.Find(".bit-aifrm");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(component.Find(".bit-aifrm-ttl").ClassList.Contains("cascaded-title"));
        Assert.AreEqual("color: red", component.Find(".bit-aifrm-ttl").GetAttribute("style"));
    }

    [TestMethod]
    public void BitAiFormShouldKeepWhatItSetForItself()
    {
        var component = RenderWithParams(new BitAiFormParams { Class = "cascaded" }, ownClass: "own");

        var root = component.Find(".bit-aifrm");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
    }
}
