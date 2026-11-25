using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.CodeBlock;

/// <summary>
/// The BitParams cascade of the code block: what a BitCodeBlockParams carries (what the code blocks of an app agree on),
/// that the code block takes it, and that what is set on the code block itself wins over it.
/// </summary>
[TestClass]
public class BitCodeBlockParamsTests : BunitTestContext
{
    // What one code block shows and what happens when it is copied.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCodeBlock.CascadingParameters),
        nameof(BitCodeBlock.ChildContent),
        nameof(BitCodeBlock.Code),
        nameof(BitCodeBlock.Language),
        nameof(BitCodeBlock.OnCopy),
        nameof(BitCodeBlock.OnCopyFailed),
        nameof(BitCodeBlock.Title),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitCodeBlockParams codeBlockParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { codeBlockParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCodeBlock>(0);
                builder.AddComponentParameter(1, nameof(BitCodeBlock.Code), "dotnet build");
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCodeBlockParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitCodeBlock", BitCodeBlockParams.ParamName);
        Assert.AreEqual(BitCodeBlockParams.ParamName, new BitCodeBlockParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitCodeBlockParams());
    }

    [TestMethod]
    public void BitCodeBlockParamsShouldCarryEveryParameterButTheContentOfOneCodeBlock()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitCodeBlock).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                             .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitCodeBlockParams).GetProperty(name), $"BitCodeBlockParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitCodeBlockParams).GetProperty(name), $"BitCodeBlockParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitCodeBlockShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitCodeBlockParams
        {
            Class = "cascaded-root",
            CopyOnClick = true,
            CopyText = "Copy it",
            MaxHeight = "5rem",
            Terminal = true,
            Classes = new() { Body = "cascaded-body" },
            Styles = new() { CopyButton = "color:red" },
        });

        var root = component.Find(".bit-cdb");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-cdb-coc"));
        Assert.IsTrue(root.ClassList.Contains("bit-cdb-trm"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-cdb-mxh:5rem");
        Assert.AreEqual("Copy it", component.Find(".bit-cdb-cpb").GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-cdb-bdy").ClassList.Contains("cascaded-body"));
        Assert.AreEqual("color:red", component.Find(".bit-cdb-cpb").GetAttribute("style"));
    }

    [TestMethod]
    public void BitCodeBlockShouldTakeACascadedShowCopyButton()
    {
        var component = RenderWithParams(new BitCodeBlockParams { ShowCopyButton = false });

        Assert.AreEqual(0, component.FindAll(".bit-cdb-cpb").Count);
    }

    [TestMethod]
    public void BitCodeBlockShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitCodeBlockParams { ShowCopyButton = false, CopyText = "Copy it", Terminal = true }, builder =>
        {
            builder.AddComponentParameter(2, nameof(BitCodeBlock.ShowCopyButton), true);
            builder.AddComponentParameter(3, nameof(BitCodeBlock.CopyText), "Mine");
        });

        Assert.AreEqual("Mine", component.Find(".bit-cdb-cpb").GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-cdb").ClassList.Contains("bit-cdb-trm"), "the cascaded value is taken where the code block sets none");
    }
}
