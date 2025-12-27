using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

[TestClass]
public class BitRichTextEditorTests : BunitTestContext
{
    private void SetupJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.initScripts");
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.initStylesheets");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setText");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setHtml");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setContent");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getText", inv => inv.Identifier == "BitBlazorUI.RichTextEditor.getText").SetResult("text");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getHtml", inv => inv.Identifier == "BitBlazorUI.RichTextEditor.getHtml").SetResult("<p>html</p>");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getContent", inv => inv.Identifier == "BitBlazorUI.RichTextEditor.getContent").SetResult("{ content: true }");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.dispose");
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderEditorAndToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ToolbarTemplate, b => b.AddContent(0, "Toolbar"));
            parameters.Add(p => p.EditorTemplate, b => b.AddContent(1, "Editor"));
        });

        var editor = component.Find(".bit-rte-edt");
        var toolbar = component.Find(".bit-rte-tlb");

        Assert.AreEqual("Editor", editor.TextContent);
        Assert.AreEqual("Toolbar", toolbar.TextContent);
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplyClassesAndReversed()
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
            parameters.Add(p => p.Reversed, true);
        });

        var root = component.Find(".bit-rte");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-rvs"));
        Assert.IsTrue(component.Find(".bit-rte-edt").ClassList.Contains("custom-editor"));
        //Assert.IsTrue(component.Find(".bit-rte-tlb").ClassList.Contains("custom-toolbar"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldInvokeOnEditorReady()
    {
        SetupJsInterop();

        var readyCalled = false;

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnEditorReady, EventCallback.Factory.Create<string>(this, _ => readyCalled = true));
        });

        await component.Instance.GetText(); // ensure ready

        component.WaitForAssertion(() => Assert.IsTrue(readyCalled));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldGetAndSetValues()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        var text = await component.Instance.GetText();
        var html = await component.Instance.GetHtml();
        var content = await component.Instance.GetContent();

        Assert.AreEqual("text", text);
        Assert.AreEqual("<p>html</p>", html);
        Assert.AreEqual("{ content: true }", content);

        await component.Instance.SetText("new text");
        await component.Instance.SetHtml("<p>new html</p>");
        await component.Instance.SetContent("{ data: true }");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.setText");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.setHtml");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.setContent");
    }

    [TestMethod]
    public void BitRichTextEditorShouldLoadModules()
    {
        SetupJsInterop();

        var module = new BitRichTextEditorModule { Name = "mentions", Src = "/mention.js", Config = new { } };

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Modules, new List<BitRichTextEditorModule> { module });
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.initScripts", 2);
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplyThemeAndPlaceholder()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Theme, BitRichTextEditorTheme.Bubble);
            parameters.Add(p => p.Placeholder, "Type here");
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.FullToolbar, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.initStylesheets");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.setup");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldDisposeJsInterop()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.dispose");
    }
}
