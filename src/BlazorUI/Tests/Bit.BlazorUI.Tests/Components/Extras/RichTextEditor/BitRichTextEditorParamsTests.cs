using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

/// <summary>
/// Covers the BitParams cascade of the RichTextEditor: what a BitRichTextEditorParams fills in, what it leaves alone
/// because the editor wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitRichTextEditorParamsTests : BunitTestContext
{
    // What belongs to a single editor rather than to a group of them: its value and events, its label, its
    // auto-focus and its form binding.
    private static readonly string[] _notCascaded =
    [
        nameof(BitRichTextEditor.CascadingParameters),
        nameof(BitRichTextEditor.AutoFocus),
        nameof(BitRichTextEditor.Description),
        nameof(BitRichTextEditor.ErrorMessage),
        nameof(BitRichTextEditor.Invalid),
        nameof(BitRichTextEditor.Label),
        nameof(BitRichTextEditor.OnBlur),
        nameof(BitRichTextEditor.OnChange),
        nameof(BitRichTextEditor.OnError),
        nameof(BitRichTextEditor.OnFocus),
        nameof(BitRichTextEditor.OnMentionSelected),
        nameof(BitRichTextEditor.OnSelectionChange),
        nameof(BitRichTextEditor.Value),
        nameof(BitRichTextEditor.ValueChanged),
        nameof(BitRichTextEditor.ValueExpression),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitRichTextEditorParams editorParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { editorParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitRichTextEditor>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitRichTextEditorParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitRichTextEditor", BitRichTextEditorParams.ParamName);
        Assert.AreEqual(BitRichTextEditorParams.ParamName, new BitRichTextEditorParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitRichTextEditorParams());
    }

    [TestMethod]
    public void BitRichTextEditorParamsShouldCarryEveryParameterThatBelongsToAGroupOfEditors()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitRichTextEditor).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                  .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                  .Select(p => p.Name)
                                                  .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                  .ToArray();

        Assert.IsTrue(parameters.Length > 20, "The reflection above found too few parameters to mean anything.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitRichTextEditorParams).GetProperty(name), $"BitRichTextEditorParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitRichTextEditorShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitRichTextEditorParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root", Editor = "cascaded-editor" },
            Styles = new() { Toolbar = "color:red" },
            Height = "12rem",
            MaxHeight = "20rem",
            Placeholder = "Schreiben...",
            SpellCheck = false,
            Resizable = true,
            StickyToolbar = true,
            ReadOnly = true,
            Required = true,
            ShowCount = true,
            MaxLength = 50,
            Toolbar = BitRichTextEditorToolbar.Inline,
            Localizer = new GermanLocalizer(),
        });

        var root = component.Find(".bit-rte");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-stk"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-ro"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-RichTextEditor-height:12rem");
        StringAssert.Contains(root.GetAttribute("style"), "--bit-RichTextEditor-max-height:20rem");

        var surface = component.Find(".bit-rte-edt");

        Assert.IsTrue(surface.ClassList.Contains("cascaded-editor"));
        Assert.IsTrue(surface.ClassList.Contains("bit-rte-edt-rsz"));
        Assert.AreEqual("Schreiben...", surface.GetAttribute("data-placeholder"));
        Assert.AreEqual("false", surface.GetAttribute("spellcheck"));
        Assert.AreEqual("true", surface.GetAttribute("aria-readonly"));
        Assert.AreEqual("true", surface.GetAttribute("aria-required"));
        Assert.AreEqual("Rich-Text-Editor", surface.GetAttribute("aria-label"));

        StringAssert.Contains(component.Find(".bit-rte-tlb").GetAttribute("style"), "color:red");
        Assert.AreEqual(1, component.FindAll(".bit-rte-grp").Count, "The cascaded toolbar holds the inline group alone.");
        StringAssert.Contains(component.Find(".bit-rte-cnt").TextContent, "/50");
    }

    [TestMethod]
    public void BitRichTextEditorShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitRichTextEditorParams
        {
            Placeholder = "Cascaded",
            ShowToolbar = false,
            Height = "12rem",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitRichTextEditor.Placeholder), "Own");
            builder.AddAttribute(2, nameof(BitRichTextEditor.ShowToolbar), true);
            builder.AddAttribute(3, nameof(BitRichTextEditor.Height), "5rem");
        });

        Assert.AreEqual("Own", component.Find(".bit-rte-edt").GetAttribute("data-placeholder"));
        Assert.AreEqual(1, component.FindAll(".bit-rte-tlb").Count);
        StringAssert.Contains(component.Find(".bit-rte").GetAttribute("style"), "--bit-RichTextEditor-height:5rem");
    }

    [TestMethod]
    public void BitRichTextEditorShouldTakeACascadedToolbarConfig()
    {
        var component = RenderWithParams(new BitRichTextEditorParams
        {
            Toolbar = BitRichTextEditorToolbar.History,
            ToolbarConfig = new()
            {
                CustomItems = [new() { Id = "stamp", Label = "Stamp", OnActivate = _ => System.Threading.Tasks.Task.CompletedTask }]
            },
        });

        Assert.AreEqual(1, component.FindAll("button[aria-label='Stamp']").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldDropTheCascadedValuesWhenTheParamsGoAway()
    {
        var editorParams = new BitRichTextEditorParams { Placeholder = "Cascaded", ShowToolbar = false };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { editorParams });
            parameters.AddChildContent<BitRichTextEditor>();
        });

        Assert.AreEqual("Cascaded", component.Find(".bit-rte-edt").GetAttribute("data-placeholder"));
        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb").Count);

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams>()));

        Assert.IsFalse(component.Find(".bit-rte-edt").HasAttribute("data-placeholder"));
        Assert.AreEqual(1, component.FindAll(".bit-rte-tlb").Count);
    }

    private sealed class GermanLocalizer : IBitRichTextEditorLocalizer
    {
        public string? this[string key] => key == "editor" ? "Rich-Text-Editor" : null;
    }
}
