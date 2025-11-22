using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AppShell;

[TestClass]
public class BitAppShellTests : BunitTestContext
{
    [TestMethod]
    public void BitAppShellShouldRenderStructureAndContent()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        var root = component.Find(".bit-ash");
        Assert.IsNotNull(root);

        component.Find(".bit-ash-top");
        component.Find(".bit-ash-center");
        component.Find(".bit-ash-left");
        component.Find(".bit-ash-main");
        component.Find(".bit-ash-right");
        component.Find(".bit-ash-bottom");

        var content = component.Find(".content");

        Assert.AreEqual("Hello", content.TextContent);
    }

    [TestMethod]
    public void BitAppShellShouldRespectClassesAndStyles()
    {
        var classes = new BitAppShellClassStyles
        {
            Root = "root-class",
            Top = "top-class",
            Center = "center-class",
            Left = "left-class",
            Main = "main-class",
            Right = "right-class",
            Bottom = "bottom-class"
        };

        var styles = new BitAppShellClassStyles
        {
            Root = "margin:1px;",
            Top = "padding:2px;",
            Center = "gap:3px;",
            Left = "width:4px;",
            Main = "height:5px;",
            Right = "border:6px solid transparent;",
            Bottom = "background:red;"
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
        });

        var root = component.Find(".bit-ash");

        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style") ?? string.Empty, "margin:1px");

        Assert.IsTrue(component.Find(".bit-ash-top").ClassList.Contains("top-class"));
        Assert.IsTrue(component.Find(".bit-ash-center").ClassList.Contains("center-class"));
        Assert.IsTrue(component.Find(".bit-ash-left").ClassList.Contains("left-class"));
        Assert.IsTrue(component.Find(".bit-ash-main").ClassList.Contains("main-class"));
        Assert.IsTrue(component.Find(".bit-ash-right").ClassList.Contains("right-class"));
        Assert.IsTrue(component.Find(".bit-ash-bottom").ClassList.Contains("bottom-class"));

        StringAssert.Contains(component.Find(".bit-ash-top").GetAttribute("style") ?? string.Empty, "padding:2px");
        StringAssert.Contains(component.Find(".bit-ash-center").GetAttribute("style") ?? string.Empty, "gap:3px");
        StringAssert.Contains(component.Find(".bit-ash-left").GetAttribute("style") ?? string.Empty, "width:4px");
        StringAssert.Contains(component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty, "height:5px");
        StringAssert.Contains(component.Find(".bit-ash-right").GetAttribute("style") ?? string.Empty, "border:6px");
        StringAssert.Contains(component.Find(".bit-ash-bottom").GetAttribute("style") ?? string.Empty, "background:red");
    }

    [TestMethod]
    public void BitAppShellShouldPersistScroll()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.initScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.locationChangedScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.afterRenderScroll");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.initScroll");

        InvokeLocationChanged(component.Instance, "https://example.com/page2");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.locationChangedScroll");

        component.Render(); // trigger OnAfterRenderAsync for non-first render

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.afterRenderScroll");
    }

    [TestMethod]
    public void BitAppShellShouldGoToTopWhenAutoGoToTop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/other");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.goToTop");
    }

    private static void InvokeLocationChanged(BitAppShell instance, string uri)
    {
        var method = instance.GetType().GetMethod("LocationChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        method!.Invoke(instance, new object?[] { null, new LocationChangedEventArgs(uri, false) });
    }
}
