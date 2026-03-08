using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

[TestClass]
public class BitMarkdownViewerTests : BunitTestContext
{
    private const string MARKED_FILE = "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js";

    [TestInitialize]
    public void RegisterService()
    {
        Services.AddSingleton<BitMarkdownService>();
    }

    private void SetupMarkdownInterop(string markdown, string html)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.initScripts");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parse", markdown).SetResult(html);
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parseAsync", markdown).SetResult(html);
        Context.JSInterop.Setup<bool>("BitBlazorUI.MarkdownViewer.checkScriptLoaded", MARKED_FILE).SetResult(true);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderParsedHtml()
    {
        var markdown = "hello";
        var html = "<p>hello</p>";

        SetupMarkdownInterop(markdown, html);

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        var root = component.Find(".bit-mdv");

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(root.InnerHtml.Contains(html));
            Assert.AreEqual(Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownViewer.parseAsync").Arguments[0], markdown);
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldInvokeCallbacks()
    {
        var markdown = "callbacks";
        var html = "<p>callbacks</p>";

        SetupMarkdownInterop(markdown, html);

        bool parsingCalled = false, parsedCalled = false, renderedCalled = false;
        string? parsedValue = null;

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
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

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains(html));
        });

        markdown = "two";
        html = "<p>two</p>";

        SetupMarkdownInterop(markdown, html);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains(html));
        });
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownViewerShouldRespectIsEnabled(bool isEnabled)
    {
        var markdown = "enable";
        var html = "<p>enable</p>";

        SetupMarkdownInterop(markdown, html);

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
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

        Func<string, string> middleware = (input) =>
        {
            middlewareCalled = input == html;
            return processedHtml;
        };

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.ParseMiddlewares, [middleware]);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(middlewareCalled);
            Assert.IsTrue(component.Markup.Contains(processedHtml));
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyMultipleCSharpMiddlewaresInOrder()
    {
        var markdown = "order";
        var html = "<p>order</p>";

        SetupMarkdownInterop(markdown, html);

        var callOrder = new List<int>();

        Func<string, string> firstMiddleware = (input) =>
        {
            callOrder.Add(1);
            return input + "-first";
        };

        Func<string, string> secondMiddleware = (input) =>
        {
            callOrder.Add(2);
            return input + "-second";
        };

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.ParseMiddlewares, [firstMiddleware, secondMiddleware]);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, callOrder.Count);
            Assert.AreEqual(1, callOrder[0]);
            Assert.AreEqual(2, callOrder[1]);
            Assert.IsTrue(component.Markup.Contains(html + "-first-second"));
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyTsMiddleware()
    {
        var markdown = "ts-middleware";
        var html = "<p>ts-middleware</p>";
        var processedHtml = "<p>ts-processed</p>";

        SetupMarkdownInterop(markdown, html);
        Context.JSInterop.Setup<string>("myApp.sanitize", html).SetResult(processedHtml);

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.ParseJsMiddlewares, ["myApp.sanitize"]);
        });

        component.WaitForAssertion(() =>
        {
            Context.JSInterop.VerifyInvoke("myApp.sanitize");
            Assert.IsTrue(component.Markup.Contains(processedHtml));
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldApplyMultipleTsMiddlewaresInOrder()
    {
        var markdown = "ts-order";
        var html = "<p>ts-order</p>";
        var afterFirst = "<p>ts-order</p>-first";
        var afterSecond = "<p>ts-order</p>-first-second";

        SetupMarkdownInterop(markdown, html);
        Context.JSInterop.Setup<string>("myApp.firstMiddleware", html).SetResult(afterFirst);
        Context.JSInterop.Setup<string>("myApp.secondMiddleware", afterFirst).SetResult(afterSecond);

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.ParseJsMiddlewares, ["myApp.firstMiddleware", "myApp.secondMiddleware"]);
        });

        component.WaitForAssertion(() =>
        {
            Context.JSInterop.VerifyInvoke("myApp.firstMiddleware");
            Context.JSInterop.VerifyInvoke("myApp.secondMiddleware");
            Assert.IsTrue(component.Markup.Contains(afterSecond));
        });
    }
}

