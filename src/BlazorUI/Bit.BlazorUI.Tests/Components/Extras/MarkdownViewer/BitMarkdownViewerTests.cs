using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

[TestClass]
public class BitMarkdownViewerTests : BunitTestContext
{
    private void ConfigureMarkdownInterop(string parsedHtml = "<p>parsed</p>")
    {
        Services.AddSingleton<BitMarkdownService>();

        Context.JSInterop.Setup<bool>("BitBlazorUI.MarkdownViewer.checkScriptLoaded").SetResult(true);
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.initScripts");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parseAsync").SetResult(parsedHtml);
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parse").SetResult(parsedHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderParsedHtml()
    {
        ConfigureMarkdownInterop("<p>hello</p>");

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "hello");
        });

        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-mdv");
            Assert.IsTrue(root.InnerHtml.Contains("<p>hello</p>"));
        });
    }

    [TestMethod]
    public void BitMarkdownViewerShouldInvokeCallbacks()
    {
        ConfigureMarkdownInterop("<p>cb</p>");

        bool parsingCalled = false, parsedCalled = false, renderedCalled = false;
        string? parsedValue = null;

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "callbacks");

            parameters.Add(p => p.OnParsing, EventCallback.Factory.Create(this, (string? value) =>
            {
                parsingCalled = value == "callbacks";
                return Task.CompletedTask;
            }));

            parameters.Add(p => p.OnParsed, EventCallback.Factory.Create(this, (string? html) =>
            {
                parsedCalled = html == "<p>cb</p>";
                parsedValue = html;
                return Task.CompletedTask;
            }));

            parameters.Add(p => p.OnRendered, EventCallback.Factory.Create(this, (string? html) =>
            {
                renderedCalled = html == parsedValue;
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
        ConfigureMarkdownInterop("<p>first</p>");

        var parseCount = 0;

        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parseAsync").SetResult("<p>first</p>");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parse").SetResult("<p>first</p>");

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "one");
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, parseCount);
            Assert.IsTrue(component.Markup.Contains("<p>first</p>"));
        });

        // update markdown and parsed html result
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parseAsync").SetResult("<p>second</p>");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownViewer.parse").SetResult("<p>second</p>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Markdown, "two");
        });

        component.WaitForAssertion(() =>
        {
            //Assert.AreEqual(2, parseCount);
            Assert.IsTrue(component.Markup.Contains("<p>second</p>"));
        });
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownViewerShouldRespectIsEnabled(bool isEnabled)
    {
        ConfigureMarkdownInterop("<p>text</p>");

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
