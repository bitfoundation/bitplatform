using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

/// <summary>
/// The textarea is seeded by the script once the page is interactive, so before that - a prerender, or a static SSR
/// page for good - it is the component that has to put the value into it.
/// </summary>
[TestClass]
public class BitMarkdownEditorPrerenderTests
{
    [TestMethod]
    public async Task BitMarkdownEditorShouldPrerenderItsValue()
    {
        const string value = "# Title\n\nSome *markdown* with <b>html</b> & an \"ampersand\".";

        var html = await Prerenderer.RenderAsync<BitMarkdownEditor>(new Dictionary<string, object?>
        {
            [nameof(BitMarkdownEditor.Value)] = value,
        });

        var document = new HtmlParser().ParseDocument(html);
        var textArea = (IHtmlTextAreaElement)document.QuerySelector("textarea.bit-mde-txa")!;

        Assert.AreEqual(value, textArea.Value);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldPrerenderAnEmptyTextAreaWithoutAValue()
    {
        var html = await Prerenderer.RenderAsync<BitMarkdownEditor>();

        var document = new HtmlParser().ParseDocument(html);
        var textArea = (IHtmlTextAreaElement)document.QuerySelector("textarea.bit-mde-txa")!;

        Assert.AreEqual(string.Empty, textArea.Value);
    }
}
