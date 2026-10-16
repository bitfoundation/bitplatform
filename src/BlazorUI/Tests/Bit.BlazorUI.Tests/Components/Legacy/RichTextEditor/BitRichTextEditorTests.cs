using System.Collections.Generic;
using System.Linq;
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
            parameters.Add(p => p.ToolbarTemplate, b => b.AddContent(0, "Toolbar"));
            parameters.Add(p => p.Classes, new BitRichTextEditorLegacyClassStyles
            {
                Editor = "custom-editor",
                Toolbar = "custom-toolbar",
                Root = "custom-root"
            });
            parameters.Add(p => p.Styles, new BitRichTextEditorLegacyClassStyles
            {
                Editor = "color:red",
                Toolbar = "color:blue",
                Root = "margin:1px"
            });
            parameters.Add(p => p.Reversed, true);
        });

        var root = component.Find(".bit-rte");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-rvs"));
        // The Extras editor shares the bit-rte-* names, so the legacy stylesheet is scoped under a class of its own.
        Assert.IsTrue(root.ClassList.Contains("bit-rte-lgc"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.IsTrue(component.Find(".bit-rte-edt").ClassList.Contains("custom-editor"));
        Assert.AreEqual("color:red", component.Find(".bit-rte-edt").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-rte-tlb").ClassList.Contains("custom-toolbar"));
        Assert.AreEqual("color:blue", component.Find(".bit-rte-tlb").GetAttribute("style"));

        // The toolbar's own style and class are also handed to the script that mounts Quill into it.
        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setup");
        Assert.AreEqual("color:blue", setup.Arguments[8]);
        Assert.AreEqual("custom-toolbar", setup.Arguments[9]);
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderNoToolbarWithoutATemplate()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditorLegacy>();

        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb").Count);
        // Quill draws its own toolbar then, so no container is handed to it.
        Assert.IsNull(Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setup").Arguments[3]);
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

        var component = RenderComponent<BitRichTextEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Theme, BitRichTextEditorLegacyTheme.Bubble);
            parameters.Add(p => p.Placeholder, "Type here");
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.FullToolbar, true);
        });

        // The theme decides which of Quill's stylesheets is loaded.
        var stylesheets = (IEnumerable<string>)Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.Utils.initStylesheets").Arguments[0]!;
        StringAssert.Contains(stylesheets.First(), "quill.bubble");

        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setup");
        Assert.AreEqual(component.Find(".bit-rte").Id, setup.Arguments[0]);
        Assert.AreEqual("bubble", setup.Arguments[4]);
        Assert.AreEqual("Type here", setup.Arguments[5]);
        Assert.AreEqual(true, setup.Arguments[6]);
        Assert.AreEqual(true, setup.Arguments[7]);
    }

    [TestMethod]
    public void BitRichTextEditorShouldSetUpTheSnowThemeByDefault()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditorLegacy>();

        var stylesheets = (IEnumerable<string>)Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.Utils.initStylesheets").Arguments[0]!;
        StringAssert.Contains(stylesheets.First(), "quill.snow");

        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.setup");
        Assert.AreEqual("snow", setup.Arguments[4]);
        Assert.AreEqual(false, setup.Arguments[6]);
        Assert.AreEqual(false, setup.Arguments[7]);
    }

    [TestMethod]
    public void BitRichTextEditorShouldReportWhenQuillAndItsModulesAreReady()
    {
        SetupJsInterop();

        var quillReady = 0;
        var modulesReady = 0;
        string? editorReady = null;

        var component = RenderComponent<BitRichTextEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Modules, new List<BitRichTextEditorLegacyModule> { new() { Name = "mentions", Src = "/mention.js", Config = new { } } });
            parameters.Add(p => p.OnQuillReady, () => quillReady++);
            parameters.Add(p => p.OnQuillModulesReady, () => modulesReady++);
            parameters.Add(p => p.OnEditorReady, id => editorReady = id);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, quillReady);
            Assert.AreEqual(1, modulesReady);
            Assert.AreEqual(component.Find(".bit-rte").Id, editorReady);
        });
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderItsTemplatesWithoutTheBrowser()
    {
        // No OnAfterRender runs before the page is interactive, so Quill is never set up and nothing throws; the
        // containers the templates fill are what the visitor sees until then.
        var html = await Prerenderer.RenderAsync<BitRichTextEditorLegacy>(new Dictionary<string, object?>
        {
            [nameof(BitRichTextEditorLegacy.ToolbarTemplate)] = (RenderFragment)(b => b.AddContent(0, "Toolbar;")),
            [nameof(BitRichTextEditorLegacy.EditorTemplate)] = (RenderFragment)(b => b.AddContent(0, "Editor;")),
            [nameof(BitRichTextEditorLegacy.Reversed)] = true,
        });

        StringAssert.Contains(html, "bit-rte-lgc");
        StringAssert.Contains(html, "bit-rte-rvs");
        StringAssert.Contains(html, "bit-rte-tlb");
        StringAssert.Contains(html, "Toolbar;");
        StringAssert.Contains(html, "bit-rte-edt");
        StringAssert.Contains(html, "Editor;");
    }

    [TestMethod]
    public void BitRichTextEditorShouldSplatTheHtmlAttributesOnTheRoot()
    {
        SetupJsInterop();

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitRichTextEditorLegacy>(0);
            builder.AddAttribute(1, nameof(BitRichTextEditorLegacy.Id), "legacy-editor");
            builder.AddAttribute(2, nameof(BitRichTextEditorLegacy.Style), "padding:1px");
            builder.AddAttribute(3, nameof(BitRichTextEditorLegacy.Class), "custom-class");
            builder.AddAttribute(4, nameof(BitRichTextEditorLegacy.Dir), BitDir.Rtl);
            builder.AddAttribute(5, "data-test", "root");
            builder.AddAttribute(6, "lang", "fa");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-rte");

        Assert.AreEqual("legacy-editor", root.Id);
        Assert.AreEqual("root", root.GetAttribute("data-test"));
        Assert.AreEqual("fa", root.GetAttribute("lang"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "padding:1px");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitRichTextEditorShouldRespectDisabled(bool disabled)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditorLegacy>(parameters => parameters.Add(p => p.Disabled, disabled));

        Assert.AreEqual(disabled, component.Find(".bit-rte").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldDisposeJsInterop()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditorLegacy>();
        var id = component.Find(".bit-rte").Id;

        await component.Instance.DisposeAsync();

        // Quill is taken down by the id it was set up under.
        Assert.AreEqual(id, Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.dispose").Arguments[0]);

        // Disposing again is a no-op rather than a second teardown.
        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.RichTextEditor.dispose", 1);
    }
}
