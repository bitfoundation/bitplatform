using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Legacy.Tests.MarkdownViewer;

[TestClass]
public class BitMarkdownViewerTests : BunitTestContext
{
    private const string MARKED_FILE = "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js";

    [TestInitialize]
    public void RegisterService()
    {
        Services.AddSingleton<BitMarkdownServiceLegacy>();
    }

    private void SetupMarkdownInterop(string markdown, string html)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.Utils.initScripts");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownViewer.parse", markdown).SetResult(html);
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownViewer.parseAsync", markdown, null).SetResult(html);
        Context.JSInterop.Setup<bool>("BitBlazorUI.Legacy.MarkdownViewer.checkScriptLoaded", MARKED_FILE).SetResult(true);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderParsedHtml()
    {
        var markdown = "hello";
        var html = "<p>hello</p>";

        SetupMarkdownInterop(markdown, html);

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        var root = component.Find(".bit-mdv");

        component.WaitForAssertion(() =>
        {
            Assert.Contains(html, root.InnerHtml);
            Assert.AreEqual(Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownViewer.parseAsync").Arguments[0], markdown);
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldInvokeCallbacks()
    {
        var markdown = "callbacks";
        var html = "<p>callbacks</p>";

        SetupMarkdownInterop(markdown, html);

        var parsingCalled = false;
        var parsedCalled = false;
        var renderedCalled = false;
        string? parsedValue = null;

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);

            parameters.Add(p => p.OnParsing, EventCallback.Factory.Create(this, (string? value) =>
            {
                parsingCalled = value == markdown;
                return Task.CompletedTask;
            }));

            parameters.Add(p => p.OnParsed, EventCallback.Factory.Create(this, (string? parsed) =>
            {
                parsedCalled = parsed == html;
                parsedValue = parsed;
                return Task.CompletedTask;
            }));

            parameters.Add(p => p.OnRendered, EventCallback.Factory.Create(this, (string? parsed) =>
            {
                renderedCalled = parsed == parsedValue;
                return Task.CompletedTask;
            }));
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(parsingCalled);
            Assert.IsTrue(parsedCalled);
            Assert.IsTrue(renderedCalled);
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenMarkdownChanges()
    {
        var markdown = "one";
        var html = "<p>one</p>";

        SetupMarkdownInterop(markdown, html);

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        component.WaitForAssertion(() =>
        {
            Assert.Contains(html, component.Markup);
        });

        markdown = "two";
        html = "<p>two</p>";

        SetupMarkdownInterop(markdown, html);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        component.WaitForAssertion(() =>
        {
            Assert.Contains(html, component.Markup);
        });
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownViewerShouldRespectDisabled(bool isEnabled)
    {
        var markdown = "enable";
        var html = "<p>enable</p>";

        SetupMarkdownInterop(markdown, html);

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
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
    public void BitMarkdownViewerShouldApplyCSharpMiddleware()
    {
        var markdown = "middleware";
        var html = "<p>middleware</p>";
        var processedHtml = "<p>middleware-processed</p>";

        SetupMarkdownInterop(markdown, html);

        var middlewareCalled = false;

        async Task<string> middleware(string input)
        {
            middlewareCalled = input == html;
            await Task.Delay(1);
            return processedHtml;
        }

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Middleware, middleware);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(middlewareCalled);
            Assert.Contains(processedHtml, component.Markup);
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyJsMiddleware()
    {
        var markdown = "js-middleware";
        var html = "<p>js-middleware</p>";
        var processedHtml = "<p>js-processed</p>";
        var jsMiddleware = "myApp.sanitize";

        SetupMarkdownInterop(markdown, html);

        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownViewer.parseAsync", markdown, jsMiddleware).SetResult(processedHtml);

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.JsMiddlewareIdentifier, jsMiddleware);
        });

        component.WaitForAssertion(() =>
        {
            Assert.Contains(processedHtml, component.Markup);
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyJsThenCSharpMiddleware()
    {
        var markdown = "combined-middleware";
        var html = "<p>combined-middleware</p>";
        var jsProcessedHtml = "<p>combined-middleware-js-processed</p>";
        var csharpProcessedHtml = "<p>combined-middleware-csharp-processed</p>";
        var jsMiddleware = "myApp.sanitize";

        SetupMarkdownInterop(markdown, html);

        Context.JSInterop
            .Setup<string>("BitBlazorUI.Legacy.MarkdownViewer.parseAsync", markdown, jsMiddleware)
            .SetResult(jsProcessedHtml);

        var middlewareCalled = false;
        string? middlewareInput = null;

        async Task<string> csMiddleware(string input)
        {
            middlewareCalled = true;
            middlewareInput = input;
            await Task.Delay(1);
            return csharpProcessedHtml;
        }

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.JsMiddlewareIdentifier, jsMiddleware);
            parameters.Add(p => p.Middleware, csMiddleware);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(middlewareCalled);
            Assert.AreEqual(jsProcessedHtml, middlewareInput);
            Assert.Contains(csharpProcessedHtml, component.Markup);
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderNothingWithoutMarkdown()
    {
        SetupMarkdownInterop("", "");

        var component = RenderComponent<BitMarkdownViewerLegacy>();

        var root = component.Find(".bit-mdv");

        // The native viewer of the Extras shares the root class, so the legacy stylesheet is keyed to a class of its own.
        Assert.IsTrue(root.ClassList.Contains("bit-mdv-lgc"));
        Assert.AreEqual(string.Empty, root.InnerHtml.Trim());
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Legacy.MarkdownViewer.parseAsync");
    }

    [TestMethod]
    public async Task BitMarkdownViewerShouldPrerenderAnEmptyRootWhenAskedNotToPrerender()
    {
        // NoPrerender leaves the parsing to the interactive render, so the server sends the root alone: no script is
        // asked for, and nothing throws.
        var html = await Prerenderer.RenderAsync<BitMarkdownViewerLegacy>(new Dictionary<string, object?>
        {
            [nameof(BitMarkdownViewerLegacy.Markdown)] = "# Title",
            [nameof(BitMarkdownViewerLegacy.NoPrerender)] = true,
        });

        StringAssert.Contains(html, "bit-mdv-lgc");
        Assert.IsFalse(html.Contains("Title"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSplatTheHtmlAttributesOnTheRoot()
    {
        SetupMarkdownInterop("hello", "<p>hello</p>");

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitMarkdownViewerLegacy>(0);
            builder.AddAttribute(1, nameof(BitMarkdownViewerLegacy.Markdown), "hello");
            builder.AddAttribute(2, nameof(BitMarkdownViewerLegacy.Id), "viewer");
            builder.AddAttribute(3, nameof(BitMarkdownViewerLegacy.Class), "custom-class");
            builder.AddAttribute(4, nameof(BitMarkdownViewerLegacy.Style), "padding:1px");
            builder.AddAttribute(5, nameof(BitMarkdownViewerLegacy.Dir), BitDir.Rtl);
            builder.AddAttribute(6, "data-test", "root");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("viewer", root.Id);
        Assert.AreEqual("root", root.GetAttribute("data-test"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "padding:1px");
    }

    [TestMethod]
    public async Task BitMarkdownViewerShouldDisposeWithoutError()
    {
        SetupMarkdownInterop("hello", "<p>hello</p>");

        var component = RenderComponent<BitMarkdownViewerLegacy>(parameters =>
        {
            parameters.Add(p => p.Markdown, "hello");
        });

        component.WaitForAssertion(() => Assert.Contains("<p>hello</p>", component.Markup));

        await component.Instance.DisposeAsync();
        // The cancellation source is disposed with the component, so a second disposal must not reach it again.
        await component.Instance.DisposeAsync();
    }
}

