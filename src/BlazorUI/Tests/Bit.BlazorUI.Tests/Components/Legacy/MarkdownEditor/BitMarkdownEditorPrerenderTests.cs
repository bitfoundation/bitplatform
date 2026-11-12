using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Legacy.Tests.MarkdownEditor;

[TestClass]
public class BitMarkdownEditorPrerenderTests : BunitTestContext
{
    [TestMethod]
    public async Task BitMarkdownEditorShouldPrerenderTheValueInTheTextArea()
    {
        var html = await Prerenderer.RenderAsync<BitMarkdownEditorLegacy>(new Dictionary<string, object?>
        {
            [nameof(BitMarkdownEditorLegacy.Value)] = "# Title & <b>body</b>",
        });

        Assert.Contains("<textarea class=\"bit-mde-txa\"># Title &amp; &lt;b&gt;body&lt;/b&gt;</textarea>", html);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldPrerenderTheDefaultValueInTheTextArea()
    {
        var html = await Prerenderer.RenderAsync<BitMarkdownEditorLegacy>(new Dictionary<string, object?>
        {
            [nameof(BitMarkdownEditorLegacy.DefaultValue)] = "hello",
        });

        Assert.Contains("<textarea class=\"bit-mde-txa\">hello</textarea>", html);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldKeepTheTextAreaValueOnceRendered()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Value, "first");
        });

        // The editor's JS owns the textarea once it is set up, so a later Value reaches it through setValue
        // alone: a value attribute re-rendered by Blazor would overwrite what is being typed.
        component.Render(parameters => parameters.Add(p => p.Value, "second"));

        Assert.AreEqual("first", component.Find("textarea").GetAttribute("value"));
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.setValue", 2);
    }
}
