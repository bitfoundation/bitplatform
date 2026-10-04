using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

/// <summary>
/// Covers the BitParams cascade of the MarkdownEditor: what a BitMarkdownEditorParams fills in, what it leaves
/// alone because the editor wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitMarkdownEditorParamsTests : BunitTestContext
{
    // What belongs to a single editor rather than to a group of them: its value and events, its label and name,
    // its draft key, its display state and its form binding.
    private static readonly string[] _notCascaded =
    [
        nameof(BitMarkdownEditor.CascadingParameters),
        nameof(BitMarkdownEditor.AutoFocus),
        nameof(BitMarkdownEditor.AutoSaveId),
        nameof(BitMarkdownEditor.DefaultValue),
        nameof(BitMarkdownEditor.Description),
        nameof(BitMarkdownEditor.DescriptionTemplate),
        nameof(BitMarkdownEditor.DisplayName),
        nameof(BitMarkdownEditor.FullScreen),
        nameof(BitMarkdownEditor.FullScreenChanged),
        nameof(BitMarkdownEditor.InputHtmlAttributes),
        nameof(BitMarkdownEditor.Label),
        nameof(BitMarkdownEditor.LabelTemplate),
        nameof(BitMarkdownEditor.Mode),
        nameof(BitMarkdownEditor.ModeChanged),
        nameof(BitMarkdownEditor.Name),
        nameof(BitMarkdownEditor.NoValidate),
        nameof(BitMarkdownEditor.OnBlur),
        nameof(BitMarkdownEditor.OnChange),
        nameof(BitMarkdownEditor.OnDraftRestored),
        nameof(BitMarkdownEditor.OnFocus),
        nameof(BitMarkdownEditor.OnImageRejected),
        nameof(BitMarkdownEditor.OnSubmit),
        nameof(BitMarkdownEditor.Value),
        nameof(BitMarkdownEditor.ValueChanged),
        nameof(BitMarkdownEditor.ValueExpression),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitMarkdownEditorParams editorParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { editorParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMarkdownEditor>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitMarkdownEditorParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitMarkdownEditor", BitMarkdownEditorParams.ParamName);
        Assert.AreEqual(BitMarkdownEditorParams.ParamName, new BitMarkdownEditorParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitMarkdownEditorParams());
    }

    [TestMethod]
    public void BitMarkdownEditorParamsShouldCarryEveryParameterThatBelongsToAGroupOfEditors()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitMarkdownEditor).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                  .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                  .Select(p => p.Name)
                                                  .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitMarkdownEditorParams).GetProperty(name), $"BitMarkdownEditorParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitMarkdownEditorParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root", TextArea = "cascaded-textarea" },
            Styles = new() { Toolbar = "color:red" },
            Height = "12rem",
            Placeholder = "Schreiben...",
            SpellCheck = false,
            Resizable = true,
            StickyToolbar = true,
            ShowReadingTime = true,
            MaxLength = 50,
            Texts = new() { EditorAriaLabel = "Markdown-Editor", ReadingTimeFormat = "{0} Min." },
        });

        var root = component.Find(".bit-mde");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-mde-rsz"));
        Assert.IsTrue(root.ClassList.Contains("bit-mde-stk"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-MarkdownEditor-height:12rem");

        var textArea = component.Find(".bit-mde-txa");

        Assert.IsTrue(textArea.ClassList.Contains("cascaded-textarea"));
        Assert.AreEqual("Schreiben...", textArea.GetAttribute("placeholder"));
        Assert.AreEqual("false", textArea.GetAttribute("spellcheck"));
        Assert.AreEqual("50", textArea.GetAttribute("maxlength"));
        Assert.AreEqual("Markdown-Editor", textArea.GetAttribute("aria-label"));

        StringAssert.Contains(component.Find(".bit-mde-tlb").GetAttribute("style"), "color:red");
        StringAssert.Contains(component.Find(".bit-mde-sbr").TextContent, "0 Min.");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitMarkdownEditorParams
        {
            Placeholder = "Cascaded",
            ShowToolbar = false,
            Height = "12rem",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMarkdownEditor.Placeholder), "Own");
            builder.AddAttribute(2, nameof(BitMarkdownEditor.ShowToolbar), true);
            builder.AddAttribute(3, nameof(BitMarkdownEditor.Height), "5rem");
        });

        Assert.AreEqual("Own", component.Find(".bit-mde-txa").GetAttribute("placeholder"));
        Assert.AreEqual(1, component.FindAll(".bit-mde-tlb").Count);
        StringAssert.Contains(component.Find(".bit-mde").GetAttribute("style"), "--bit-MarkdownEditor-height:5rem");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldTakeACascadedToolbarAndUploadHandler()
    {
        var toolbar = new List<BitMarkdownEditorToolbarItem>
        {
            new() { Name = "bold", Title = "Bold", Command = BitMarkdownEditorCommand.Bold },
            new() { Name = "upload", Title = "Upload image", Type = BitMarkdownEditorToolbarItemType.ImageUpload },
        };

        var component = RenderWithParams(new BitMarkdownEditorParams
        {
            Toolbar = toolbar,
            OnImageUpload = _ => System.Threading.Tasks.Task.FromResult<string?>("https://example.com/a.png"),
        });

        Assert.AreEqual(2, component.FindAll(".bit-mde-tlb .bit-mde-btn").Count);
        Assert.AreEqual(1, component.FindAll("[data-cmd=upload][data-bit-mde-upload]").Count);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldDropTheCascadedValuesWhenTheParamsGoAway()
    {
        var editorParams = new BitMarkdownEditorParams { Placeholder = "Cascaded", ShowStatusBar = false };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { editorParams });
            parameters.AddChildContent<BitMarkdownEditor>();
        });

        Assert.AreEqual("Cascaded", component.Find(".bit-mde-txa").GetAttribute("placeholder"));
        Assert.AreEqual(0, component.FindAll(".bit-mde-sbr").Count);

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams>()));

        Assert.IsFalse(component.Find(".bit-mde-txa").HasAttribute("placeholder"));
        Assert.AreEqual(1, component.FindAll(".bit-mde-sbr").Count);
    }
}
