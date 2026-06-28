using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

[TestClass]
public class BitRichTextEditorTests : BunitTestContext
{
    private void SetupJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.initialize");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.enableToolbarRoving");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setHtml");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.exec");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.execBlock");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.focus");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.dispose");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getHtml", _ => true).SetResult("<p>html</p>");
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderEditorAndToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        Assert.IsNotNull(component.Find(".bit-rte"));
        Assert.IsNotNull(component.Find(".bit-rte-edt"));
        Assert.IsNotNull(component.Find(".bit-rte-tlb"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldHideToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplyClassesAndReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitRichTextEditorClassStyles
            {
                Editor = "custom-editor",
                Toolbar = "custom-toolbar",
                Root = "custom-root"
            });
            parameters.Add(p => p.ReadOnly, true);
        });

        var root = component.Find(".bit-rte");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-ro"));
        Assert.IsTrue(component.Find(".bit-rte-edt").ClassList.Contains("custom-editor"));
        Assert.IsTrue(component.Find(".bit-rte-tlb").ClassList.Contains("custom-toolbar"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderPlaceholder()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Type here");
        });

        Assert.AreEqual("Type here", component.Find(".bit-rte-edt").GetAttribute("data-placeholder"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderToolbarGroups()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
        });

        // The inline group renders exactly four buttons (bold, italic, underline, strikethrough)
        // and no other groups, so the count must be exact to catch extra groups leaking in.
        Assert.AreEqual(4, component.FindAll(".bit-rte-tlb .bit-rte-btn").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldGetHtml()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        var html = await component.Instance.GetHtmlAsync();

        Assert.AreEqual("<p>html</p>", html);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldExecuteCommand()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.Instance.ExecuteCommandAsync("bold");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.exec");
    }

    [TestMethod]
    public void BitRichTextEditorShouldSetupOnFirstRender()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.initialize");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.enableToolbarRoving");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldDisposeJsInterop()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.dispose");
    }

    [TestMethod]
    public void BitRichTextEditorShouldInvokeSanitizeBridgeWhenPolicyIsSet()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>clean</p>");

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.SanitizationPolicy, BitRichTextEditorSanitizationPolicy.Default);
        });

        // A value change after initialization routes through the sanitization bridge.
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Value, "<p><script>alert(1)</script>dirty</p>");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.sanitizeHtml");
    }
}
