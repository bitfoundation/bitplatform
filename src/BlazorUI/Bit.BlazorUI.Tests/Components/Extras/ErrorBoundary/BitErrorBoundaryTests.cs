using System;
using System.Linq;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

[TestClass]
public partial class BitErrorBoundaryTests : BunitTestContext
{
    [TestMethod]
    public void BitErrorBoundaryShouldRenderChildContentWhenNoError()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        var safe = component.Find(".safe");

        Assert.AreEqual("Hello", safe.TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderDefaultErrorAndShowException()
    {
        var called = false;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, _ => called = true));
            parameters.Add(p => p.ChildContent, ThrowingContent("boom"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsNotNull(errorRoot);
        StringAssert.Contains(errorRoot.TextContent, "Oops, Something went wrong");
        StringAssert.Contains(errorRoot.TextContent, "boom");
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderCustomFooter()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-footer\">Custom</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".custom-footer");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ftr"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderAdditionalButtons()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AdditionalButtons, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"extra-btn\">Extra</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".extra-btn");
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverShouldResetAfterError()
    {
        ThrowOnceComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        var recoverButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase));
        recoverButton.Click();

        var safe = component.Find(".throw-once-safe");
        Assert.AreEqual("Recovered", safe.TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryHomeButtonShouldNavigate()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HomeUrl, "https://example.com/home");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var homeButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Home", StringComparison.OrdinalIgnoreCase));

        homeButton.Click();

        var navMan = Services.GetRequiredService<FakeNavigationManager>();

        Assert.AreEqual("https://example.com/home", navMan.Uri);
    }

    [TestMethod]
    public void BitErrorBoundaryRefreshButtonShouldInvokeNavigation()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var navMan = Services.GetRequiredService<FakeNavigationManager>();

        var initialCount = navMan.History.Count;

        var refreshButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Refresh", StringComparison.OrdinalIgnoreCase));

        refreshButton.Click();

        Assert.IsTrue(navMan.History.Count > initialCount);
    }

    private static RenderFragment ThrowingContent(string message) => b =>
    {
        b.OpenComponent<ThrowingComponent>(0);
        b.AddAttribute(1, "Message", message);
        b.CloseComponent();
    };
}
