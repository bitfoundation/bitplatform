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

    [DataTestMethod,
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
}
