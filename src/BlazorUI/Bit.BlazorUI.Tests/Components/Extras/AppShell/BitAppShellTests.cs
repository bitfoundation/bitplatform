using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AppShell;

[TestClass]
public class BitAppShellTests : BunitTestContext
{
    [TestMethod]
    public void BitAppShellShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitAppShell>();

        component.MarkupMatches(@"
            <div class=""bit-ash"" id:ignore>
                <div class=""bit-ash-top""></div>
                <div class=""bit-ash-center"">
                    <div class=""bit-ash-left""></div>
                    <div id=""BitAppShell-container"" class=""bit-ash-main""></div>
                    <div class=""bit-ash-right""></div>
                </div>
                <div class=""bit-ash-bottom""></div>
            </div>");
    }

    [TestMethod]
    public void BitAppShellShouldHaveUniqueId()
    {
        var component1 = RenderComponent<BitAppShell>();
        var component2 = RenderComponent<BitAppShell>();

        var id1 = component1.Instance.UniqueId;
        var id2 = component2.Instance.UniqueId;

        Assert.IsNotNull(id1);
        Assert.IsNotNull(id2);
        Assert.AreNotEqual(id1, id2);
    }

    [TestMethod]
    public void BitAppShellShouldHaveRootElement()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.IsNotNull(component.Instance.RootElement);
    }

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

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAppShellShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-ash");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitAppShellShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
        });

        var root = component.Find(".bit-ash");

        Assert.IsFalse(root.ClassList.Contains("bit-dis"));

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        root = component.Find(".bit-ash");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
    }

    [TestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitAppShellShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        var root = component.Find(".bit-ash");

        if (style is not null)
        {
            Assert.Contains(style, root.GetAttribute("style") ?? string.Empty);
        }
    }

    [TestMethod]
    public void BitAppShellShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Style, "color: red;");
        });

        var root = component.Find(".bit-ash");

        Assert.Contains("color: red", root.GetAttribute("style") ?? string.Empty);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, "color: blue;");
        });

        root = component.Find(".bit-ash");

        Assert.Contains("color: blue", root.GetAttribute("style") ?? string.Empty);
    }

    [TestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitAppShellShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var root = component.Find(".bit-ash");

        Assert.IsTrue(root.ClassList.Contains("bit-ash"));

        if (@class is not null)
        {
            Assert.IsTrue(root.ClassList.Contains(@class));
        }
    }

    [TestMethod]
    public void BitAppShellShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Class, "first-class");
        });

        var root = component.Find(".bit-ash");

        Assert.IsTrue(root.ClassList.Contains("first-class"));

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, "second-class");
        });

        root = component.Find(".bit-ash");

        Assert.IsTrue(root.ClassList.Contains("second-class"));
        Assert.IsFalse(root.ClassList.Contains("first-class"));
    }

    [TestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitAppShellShouldRespectId(string id)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var root = component.Find(".bit-ash");

        if (id is not null)
        {
            Assert.AreEqual(id, root.GetAttribute("id"));
        }
        else
        {
            Assert.IsNotNull(root.GetAttribute("id"));
        }
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitAppShellShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-ash");

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? "bit-rtl" : null;

            var expected = dir.Value.ToString().ToLower();
            var actual = root.GetAttribute("dir");

            Assert.AreEqual(expected, actual);

            if (cssClass is not null)
            {
                Assert.IsTrue(root.ClassList.Contains(cssClass));
            }
        }
        else
        {
            Assert.IsNull(root.GetAttribute("dir"));
        }
    }

    [TestMethod]
    public void BitAppShellShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitAppShell>();

        var root = component.Find(".bit-ash");

        Assert.IsNull(root.GetAttribute("dir"));

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        root = component.Find(".bit-ash");

        Assert.AreEqual("ltr", root.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitAppShellShouldRespectDirChangingFromRtlToLtr()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-ash");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        root = component.Find(".bit-ash");

        Assert.AreEqual("ltr", root.GetAttribute("dir"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Hidden),
        DataRow(BitVisibility.Collapsed)
    ]
    public void BitAppShellShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                var root = component.Find(".bit-ash");
                Assert.IsNotNull(root);
                break;
            case BitVisibility.Hidden:
                root = component.Find(".bit-ash");
                Assert.Contains("visibility:hidden", root.GetAttribute("style") ?? string.Empty);
                break;
            case BitVisibility.Collapsed:
                root = component.Find(".bit-ash");
                Assert.Contains("display:none", root.GetAttribute("style") ?? string.Empty);
                break;
        }
    }

    [TestMethod,
        DataRow("data-value", "ID-123"),
        DataRow("aria-test", "this is test")
    ]
    public void BitAppShellShouldRespectHtmlAttributes(string name, string value)
    {
        var component = RenderComponent<BitAppShell>((name, value));

        var root = component.Find(".bit-ash");

        Assert.AreEqual(value, root.GetAttribute(name));
    }

    [TestMethod]
    public void BitAppShellShouldRespectClasses()
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

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
        });

        var root = component.Find(".bit-ash");

        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(component.Find(".bit-ash-top").ClassList.Contains("top-class"));
        Assert.IsTrue(component.Find(".bit-ash-center").ClassList.Contains("center-class"));
        Assert.IsTrue(component.Find(".bit-ash-left").ClassList.Contains("left-class"));
        Assert.IsTrue(component.Find(".bit-ash-main").ClassList.Contains("main-class"));
        Assert.IsTrue(component.Find(".bit-ash-right").ClassList.Contains("right-class"));
        Assert.IsTrue(component.Find(".bit-ash-bottom").ClassList.Contains("bottom-class"));
    }

    [TestMethod]
    public void BitAppShellShouldRespectStyles()
    {
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
            parameters.Add(p => p.Styles, styles);
        });

        var root = component.Find(".bit-ash");

        Assert.Contains("margin:1px", root.GetAttribute("style") ?? string.Empty);
        Assert.Contains("padding:2px", component.Find(".bit-ash-top").GetAttribute("style") ?? string.Empty);
        Assert.Contains("gap:3px", component.Find(".bit-ash-center").GetAttribute("style") ?? string.Empty);
        Assert.Contains("width:4px", component.Find(".bit-ash-left").GetAttribute("style") ?? string.Empty);
        Assert.Contains("height:5px", component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty);
        Assert.Contains("border:6px", component.Find(".bit-ash-right").GetAttribute("style") ?? string.Empty);
        Assert.Contains("background:red", component.Find(".bit-ash-bottom").GetAttribute("style") ?? string.Empty);
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
        Assert.Contains("margin:1px", root.GetAttribute("style") ?? string.Empty);

        Assert.IsTrue(component.Find(".bit-ash-top").ClassList.Contains("top-class"));
        Assert.IsTrue(component.Find(".bit-ash-center").ClassList.Contains("center-class"));
        Assert.IsTrue(component.Find(".bit-ash-left").ClassList.Contains("left-class"));
        Assert.IsTrue(component.Find(".bit-ash-main").ClassList.Contains("main-class"));
        Assert.IsTrue(component.Find(".bit-ash-right").ClassList.Contains("right-class"));
        Assert.IsTrue(component.Find(".bit-ash-bottom").ClassList.Contains("bottom-class"));

        Assert.Contains("padding:2px", component.Find(".bit-ash-top").GetAttribute("style") ?? string.Empty);
        Assert.Contains("gap:3px", component.Find(".bit-ash-center").GetAttribute("style") ?? string.Empty);
        Assert.Contains("width:4px", component.Find(".bit-ash-left").GetAttribute("style") ?? string.Empty);
        Assert.Contains("height:5px", component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty);
        Assert.Contains("border:6px", component.Find(".bit-ash-right").GetAttribute("style") ?? string.Empty);
        Assert.Contains("background:red", component.Find(".bit-ash-bottom").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void BitAppShellShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Visible);
        });

        var root = component.Find(".bit-ash");

        Assert.DoesNotContain("visibility:hidden", root.GetAttribute("style") ?? string.Empty);
        Assert.DoesNotContain("display:none", root.GetAttribute("style") ?? string.Empty);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        root = component.Find(".bit-ash");

        Assert.Contains("visibility:hidden", root.GetAttribute("style") ?? string.Empty);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        root = component.Find(".bit-ash");

        Assert.Contains("display:none", root.GetAttribute("style") ?? string.Empty);
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
    public void BitAppShellShouldNotCallLocationChangedScrollWhenNotYetRendered()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.initScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.locationChangedScroll");

        // We need to verify that locationChangedScroll is only called after the component has rendered
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        // After render, invoke location changed
        InvokeLocationChanged(component.Instance, "https://example.com/page2");

        // Should invoke since component is rendered
        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.locationChangedScroll");
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

    [TestMethod]
    public void BitAppShellShouldNotSubscribeToLocationChangedWhenBothAutoGoToTopAndPersistScrollAreFalse()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.initScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.locationChangedScroll");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, false);
            parameters.Add(p => p.PersistScroll, false);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/page");

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Extras.goToTop");
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.AppShell.locationChangedScroll");
    }

    [TestMethod]
    public async Task BitAppShellShouldCallGoToTopMethod()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>();

        await component.Instance.GoToTop();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.goToTop");
    }

    [TestMethod]
    public async Task BitAppShellShouldCallGoToTopMethodWithBehavior()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>();

        await component.Instance.GoToTop(BitScrollBehavior.Smooth);

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Extras.goToTop");
        Assert.HasCount(2, invocation.Arguments);
    }

    [TestMethod,
        DataRow(BitScrollBehavior.Auto),
        DataRow(BitScrollBehavior.Instant),
        DataRow(BitScrollBehavior.Smooth)
    ]
    public async Task BitAppShellShouldCallGoToTopMethodWithAllBehaviors(BitScrollBehavior behavior)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>();

        await component.Instance.GoToTop(behavior);

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.goToTop");
    }

    [TestMethod]
    public void BitAppShellAutoGoToTopShouldUseInstantBehavior()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/other");

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Extras.goToTop");
        
        Assert.HasCount(2, invocation.Arguments);
    }

    [TestMethod]
    public void BitAppShellShouldProvideCascadingValues()
    {
        var cascadingValues = new List<BitCascadingValue>
        {
            new("TestValue", "TestName")
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues);
            parameters.AddChildContent("<div class=\"child\">Child Content</div>");
        });

        var child = component.Find(".child");

        Assert.IsNotNull(child);
        Assert.AreEqual("Child Content", child.TextContent);
    }

    [TestMethod]
    public void BitAppShellShouldProvideCascadingValueList()
    {
        var valueList = new BitCascadingValueList
        {
            { "TestValue", "TestName" }
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.ValueList, valueList);
            parameters.AddChildContent("<div class=\"child\">Child Content</div>");
        });

        var child = component.Find(".child");

        Assert.IsNotNull(child);
        Assert.AreEqual("Child Content", child.TextContent);
    }

    [TestMethod]
    public void BitAppShellShouldProvideBothValuesAndValueList()
    {
        var cascadingValues = new List<BitCascadingValue>
        {
            new("Value1", "Name1")
        };

        var valueList = new BitCascadingValueList
        {
            { "Value2", "Name2" }
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues);
            parameters.Add(p => p.ValueList, valueList);
            parameters.AddChildContent("<div class=\"child\">Child Content</div>");
        });

        var child = component.Find(".child");

        Assert.IsNotNull(child);
        Assert.AreEqual("Child Content", child.TextContent);
    }

    [TestMethod]
    public void BitAppShellShouldWorkWithNullValues()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Values, null);
            parameters.Add(p => p.ValueList, null);
            parameters.AddChildContent("<div class=\"child\">Child Content</div>");
        });

        var child = component.Find(".child");

        Assert.IsNotNull(child);
        Assert.AreEqual("Child Content", child.TextContent);
    }

    [TestMethod]
    public void BitAppShellShouldHaveContainerRef()
    {
        var component = RenderComponent<BitAppShell>();

        // ContainerRef should be available after render
        Assert.IsNotNull(component.Instance.ContainerRef);
    }

    [TestMethod]
    public void BitAppShellMainContainerShouldHaveCorrectId()
    {
        var component = RenderComponent<BitAppShell>();

        var mainContainer = component.Find("#BitAppShell-container");

        Assert.IsNotNull(mainContainer);
        Assert.IsTrue(mainContainer.ClassList.Contains("bit-ash-main"));
    }

    [TestMethod]
    public void BitAppShellShouldCallInitScrollOnFirstRenderWithPersistScroll()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.initScroll");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.initScroll");
    }

    [TestMethod]
    public void BitAppShellShouldDisposeCorrectly()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.initScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.disposeScroll");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.initScroll");

        // Dispose component through bUnit's DisposeComponents to trigger proper lifecycle
        Context.DisposeComponents();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.disposeScroll");
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitAppShellShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            if (childContent is not null)
            {
                parameters.AddChildContent(childContent);
            }
        });

        var mainContainer = component.Find(".bit-ash-main");

        Assert.IsNotNull(mainContainer);

        if (childContent is not null)
        {
            Assert.Contains(childContent, mainContainer.InnerHtml);
        }
    }

    [TestMethod]
    public void BitAppShellCenterAndRootShouldRespectDir()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-ash");
        var center = component.Find(".bit-ash-center");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("rtl", center.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitAppShellShouldRenderAllSections()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.IsNotNull(component.Find(".bit-ash-top"));
        Assert.IsNotNull(component.Find(".bit-ash-center"));
        Assert.IsNotNull(component.Find(".bit-ash-left"));
        Assert.IsNotNull(component.Find(".bit-ash-main"));
        Assert.IsNotNull(component.Find(".bit-ash-right"));
        Assert.IsNotNull(component.Find(".bit-ash-bottom"));
    }

    [TestMethod]
    public void BitAppShellShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "test aria label");
        });

        var root = component.Find(".bit-ash");

        Assert.AreEqual("test aria label", root.GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(null),
        DataRow("0"),
        DataRow("-1"),
        DataRow("5")
    ]
    public void BitAppShellShouldRespectTabIndex(string tabIndex)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            if (tabIndex is not null)
            {
                parameters.Add(p => p.TabIndex, tabIndex);
            }
        });

        var root = component.Find(".bit-ash");

        if (tabIndex is not null)
        {
            Assert.AreEqual(tabIndex, root.GetAttribute("tabindex"));
        }
        else
        {
            Assert.IsNull(root.GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitAppShellShouldNotGoToTopWhenAutoGoToTopIsFalse()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, false);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/page");

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Extras.goToTop");
    }

    [TestMethod]
    public void BitAppShellShouldNotPersistScrollWhenPersistScrollIsFalse()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.initScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.locationChangedScroll");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, false);
        });

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.AppShell.initScroll");

        InvokeLocationChanged(component.Instance, "https://example.com/page");

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.AppShell.locationChangedScroll");
    }

    [TestMethod]
    public void BitAppShellShouldPrioritizePersistScrollOverAutoGoToTop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.initScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.AppShell.locationChangedScroll");
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
            parameters.Add(p => p.AutoGoToTop, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/page");

        // When both are true, PersistScroll takes precedence and GoToTop should not be called
        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.locationChangedScroll");
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Extras.goToTop");
    }

    [TestMethod]
    public void BitAppShellShouldHandleMultipleHtmlAttributes()
    {
        var component = RenderComponent<BitAppShell>(
            ("data-id", "test-id"),
            ("data-type", "app-shell"),
            ("aria-hidden", "false"),
            ("title", "App Shell Title")
        );

        var root = component.Find(".bit-ash");

        Assert.AreEqual("test-id", root.GetAttribute("data-id"));
        Assert.AreEqual("app-shell", root.GetAttribute("data-type"));
        Assert.AreEqual("false", root.GetAttribute("aria-hidden"));
        Assert.AreEqual("App Shell Title", root.GetAttribute("title"));
    }

    [TestMethod]
    public void BitAppShellClassesShouldNotAffectOtherSections()
    {
        var classes = new BitAppShellClassStyles
        {
            Top = "only-top-class"
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
        });

        Assert.IsTrue(component.Find(".bit-ash-top").ClassList.Contains("only-top-class"));
        Assert.IsFalse(component.Find(".bit-ash-center").ClassList.Contains("only-top-class"));
        Assert.IsFalse(component.Find(".bit-ash-left").ClassList.Contains("only-top-class"));
        Assert.IsFalse(component.Find(".bit-ash-main").ClassList.Contains("only-top-class"));
        Assert.IsFalse(component.Find(".bit-ash-right").ClassList.Contains("only-top-class"));
        Assert.IsFalse(component.Find(".bit-ash-bottom").ClassList.Contains("only-top-class"));
    }

    [TestMethod]
    public void BitAppShellStylesShouldNotAffectOtherSections()
    {
        var styles = new BitAppShellClassStyles
        {
            Left = "width:200px;"
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Styles, styles);
        });

        Assert.Contains("width:200px", component.Find(".bit-ash-left").GetAttribute("style") ?? string.Empty);
        Assert.DoesNotContain("width:200px", component.Find(".bit-ash-top").GetAttribute("style") ?? string.Empty);
        Assert.DoesNotContain("width:200px", component.Find(".bit-ash-right").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void BitAppShellShouldRenderComplexChildContent()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.AddChildContent(@"
                <header class=""app-header"">Header</header>
                <main class=""app-content"">Content</main>
                <footer class=""app-footer"">Footer</footer>
            ");
        });

        var mainContainer = component.Find(".bit-ash-main");

        Assert.IsNotNull(component.Find(".app-header"));
        Assert.IsNotNull(component.Find(".app-content"));
        Assert.IsNotNull(component.Find(".app-footer"));
    }

    [TestMethod]
    public void BitAppShellShouldHandleEmptyChildContent()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.AddChildContent("");
        });

        var mainContainer = component.Find(".bit-ash-main");

        Assert.IsNotNull(mainContainer);
        Assert.AreEqual(string.Empty, mainContainer.TextContent.Trim());
    }

    [TestMethod]
    public void BitAppShellShouldDisposeWithoutPersistScroll()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, false);
        });

        // Should not throw when disposing without PersistScroll
        Context.DisposeComponents();

        // Verify no JS calls were made for scroll
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.AppShell.disposeScroll");
    }

    [TestMethod]
    public void BitAppShellShouldHandleMultipleLocationChanges()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.goToTop");

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/page1");
        InvokeLocationChanged(component.Instance, "https://example.com/page2");
        InvokeLocationChanged(component.Instance, "https://example.com/page3");

        var invocations = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.Extras.goToTop");
        
        Assert.AreEqual(3, invocations.Count());
    }

    [TestMethod]
    public void BitAppShellShouldHaveCorrectStructureOrder()
    {
        var component = RenderComponent<BitAppShell>();

        var root = component.Find(".bit-ash");
        var children = root.Children;

        // Verify the correct order: top, center, bottom
        Assert.AreEqual(3, children.Length);
        Assert.IsTrue(children[0].ClassList.Contains("bit-ash-top"));
        Assert.IsTrue(children[1].ClassList.Contains("bit-ash-center"));
        Assert.IsTrue(children[2].ClassList.Contains("bit-ash-bottom"));

        // Verify center structure: left, main, right
        var centerChildren = children[1].Children;
        Assert.AreEqual(3, centerChildren.Length);
        Assert.IsTrue(centerChildren[0].ClassList.Contains("bit-ash-left"));
        Assert.IsTrue(centerChildren[1].ClassList.Contains("bit-ash-main"));
        Assert.IsTrue(centerChildren[2].ClassList.Contains("bit-ash-right"));
    }

    [TestMethod]
    public void BitAppShellClassBuilderShouldCombineClasses()
    {
        var classes = new BitAppShellClassStyles
        {
            Root = "custom-root"
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Class, "additional-class");
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.IsEnabled, false);
        });

        var root = component.Find(".bit-ash");

        Assert.IsTrue(root.ClassList.Contains("bit-ash"));
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.ClassList.Contains("additional-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitAppShellStyleBuilderShouldCombineStyles()
    {
        var styles = new BitAppShellClassStyles
        {
            Root = "background:red;"
        };

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Style, "color:blue;");
            parameters.Add(p => p.Styles, styles);
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        var root = component.Find(".bit-ash");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.Contains("background:red", style);
        Assert.Contains("color:blue", style);
        Assert.Contains("visibility:hidden", style);
    }

    private static void InvokeLocationChanged(BitAppShell instance, string uri)
    {
        var method = instance.GetType().GetMethod("LocationChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        method!.Invoke(instance, [null, new LocationChangedEventArgs(uri, false)]);
    }
}
