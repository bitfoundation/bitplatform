using System.Linq;
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
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.updateOptions");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.enableToolbarRoving");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setHtml");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.exec");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.execBlock");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.focus");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.dispose");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getHtml", _ => true).SetResult("<p>html</p>");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getText", _ => true).SetResult("text");
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
                Root = "custom-root",
                Group = "custom-group",
                Button = "custom-button",
                Count = "custom-count"
            });
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowCount, true);
        });

        var root = component.Find(".bit-rte");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-ro"));
        Assert.IsTrue(component.Find(".bit-rte-edt").ClassList.Contains("custom-editor"));
        Assert.IsTrue(component.Find(".bit-rte-tlb").ClassList.Contains("custom-toolbar"));
        Assert.IsTrue(component.Find(".bit-rte-grp").ClassList.Contains("custom-group"));
        Assert.IsTrue(component.Find(".bit-rte-btn").ClassList.Contains("custom-button"));
        Assert.IsTrue(component.Find(".bit-rte-cnt").ClassList.Contains("custom-count"));
        // Note: the Source hook only renders inside the HTML source-view textarea, which requires
        // toggling into source view (a JS-bridged action) and is covered separately.
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

        // The Inline group must render as a single toolbar group of exactly four toggle buttons -
        // bold, italic, underline, strikethrough (in that order) - with no other groups or
        // non-button controls (color/font selectors, url/find inputs) leaking in.
        Assert.AreEqual(1, component.FindAll(".bit-rte-tlb .bit-rte-grp").Count);

        var buttons = component.FindAll(".bit-rte-tlb .bit-rte-btn");
        Assert.AreEqual(4, buttons.Count);
        CollectionAssert.AreEqual(
            new[] { "Bold", "Italic", "Underline", "Strikethrough" },
            buttons.Select(b => b.GetAttribute("aria-label")).ToArray());
        foreach (var button in buttons)
        {
            Assert.AreEqual("button", button.GetAttribute("type"));
        }

        // No selectors, color pickers, or text inputs belong to the inline-only toolbar.
        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb input").Count);
        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb select").Count);
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
    public async Task BitRichTextEditorShouldGetText()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        var text = await component.Instance.GetTextAsync();

        Assert.AreEqual("text", text);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldExecuteCommand()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.Instance.ExecuteCommandAsync("bold");

        // Assert the exact forwarded command (Arguments: [editor, command, value]) so a wrong
        // command string can't slip through - merely verifying the invoke happened wouldn't.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.exec");
        Assert.AreEqual("bold", invocation.Arguments[1]);
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

        // Capture how many times the sanitize bridge was invoked during setup so the assertion
        // below proves the *update* path invokes it again, not an earlier render.
        var before = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;

        // A value change after initialization routes through the sanitization bridge.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, "<p><script>alert(1)</script>dirty</p>");
        });

        var after = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;
        Assert.IsTrue(after > before, "The Value update should route through the sanitize bridge.");
    }

    [TestMethod]
    public void BitRichTextEditorShouldInvokeSanitizeBridgeWhenPolicyIsNull()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>clean</p>");

        // No SanitizationPolicy is set, so the component relies on the bridge's secure default
        // allowlist; non-empty Value updates must still route through the sanitize bridge.
        var component = RenderComponent<BitRichTextEditor>();

        var before = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;

        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, "<p><script>alert(1)</script>dirty</p>");
        });

        var after = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;
        Assert.IsTrue(after > before, "The Value update should route through the sanitize bridge even without an explicit policy.");
    }
}
