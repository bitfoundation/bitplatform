using System.Collections.Generic;
using Bit.BlazorUI.Tests;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Legacy.Tests.RichTextEditor;

[TestClass]
public class BitRichTextEditorTests : BunitTestContext
{
    private void SetupJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.Utils.initScripts");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.Utils.initStylesheets");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.RichTextEditor.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.RichTextEditor.setText");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.RichTextEditor.setHtml");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.RichTextEditor.setContent");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.RichTextEditor.getText", inv => inv.Identifier == "BitBlazorUI.Legacy.RichTextEditor.getText").SetResult("text");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.RichTextEditor.getHtml", inv => inv.Identifier == "BitBlazorUI.Legacy.RichTextEditor.getHtml").SetResult("<p>html</p>");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.RichTextEditor.getContent", inv => inv.Identifier == "BitBlazorUI.Legacy.RichTextEditor.getContent").SetResult("{ content: true }");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.RichTextEditor.dispose");
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderEditorAndToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditorLegacy>(parameters =>
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

        var component = RenderComponent<BitRichTextEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitRichTextEditorLegacyClassStyles
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

        var component = RenderComponent<BitRichTextEditorLegacy>(parameters =>
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

        var component = RenderComponent<BitRichTextEditorLegacy>();

        var text = await component.Instance.GetText();
        var html = await component.Instance.GetHtml();
        var content = await component.Instance.GetContent();

        Assert.AreEqual("text", text);
        Assert.AreEqual("<p>html</p>", html);
        Assert.AreEqual("{ content: true }", content);

        await component.Instance.SetText("new text");
        await component.Instance.SetHtml("<p>new html</p>");
        await component.Instance.SetContent("{ data: true }");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setText");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setHtml");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setContent");
    }

    [TestMethod]
    public void BitRichTextEditorShouldLoadModules()
    {
        SetupJsInterop();

        var module = new BitRichTextEditorLegacyModule { Name = "mentions", Src = "/mention.js", Config = new { } };

        RenderComponent<BitRichTextEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Modules, new List<BitRichTextEditorLegacyModule> { module });
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.Utils.initScripts", 2);
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplyThemeAndPlaceholder()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Theme, BitRichTextEditorLegacyTheme.Bubble);
            parameters.Add(p => p.Placeholder, "Type here");
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.FullToolbar, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.Utils.initStylesheets");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setup");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldDisposeJsInterop()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditorLegacy>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.dispose");
    }
}
