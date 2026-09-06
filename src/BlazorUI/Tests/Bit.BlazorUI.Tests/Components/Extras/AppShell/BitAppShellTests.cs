using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
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
                    <div id=""BitAppShell-container"" class=""bit-ash-main bit-ash-smt""></div>
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

        component.Render(parameters =>
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

        component.Render(parameters =>
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

        component.Render(parameters =>
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

        component.Render(parameters =>
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

        component.Render(parameters =>
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
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitAppShell>(0);
            builder.AddAttribute(1, name, value);
            builder.CloseComponent();
        });

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

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        root = component.Find(".bit-ash");

        Assert.Contains("visibility:hidden", root.GetAttribute("style") ?? string.Empty);

        component.Render(parameters =>
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

        // Dispose component through bUnit's DisposeComponentsAsync to trigger proper lifecycle
        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

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
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitAppShell>(0);
            builder.AddAttribute(1, "data-id", "test-id");
            builder.AddAttribute(2, "data-type", "app-shell");
            builder.AddAttribute(3, "aria-hidden", "false");
            builder.AddAttribute(4, "title", "App Shell Title");
            builder.CloseComponent();
        });

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
        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

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


    // ---------------------------------------------------------------------------------------------
    //  Container id
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitAppShellMainContainerShouldUseTheWellKnownIdByDefault()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.AreEqual(BitAppShell.ContainerId, component.Instance.MainContainerId);
        Assert.AreEqual(BitAppShell.ContainerId, component.Find(".bit-ash-main").GetAttribute("id"));
    }

    [TestMethod]
    public void BitAppShellMainContainerIdShouldBeDerivedFromTheIdOfTheShell()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Id, "second-shell");
        });

        Assert.AreEqual("second-shell-container", component.Instance.MainContainerId);
        Assert.AreEqual("second-shell-container", component.Find(".bit-ash-main").GetAttribute("id"));
    }

    [TestMethod]
    public void BitAppShellTwoShellsWithIdsShouldNotShareTheirContainerId()
    {
        var first = RenderComponent<BitAppShell>(parameters => parameters.Add(p => p.Id, "shell-a"));
        var second = RenderComponent<BitAppShell>(parameters => parameters.Add(p => p.Id, "shell-b"));

        Assert.AreNotEqual(first.Instance.MainContainerId, second.Instance.MainContainerId);
    }

    // ---------------------------------------------------------------------------------------------
    //  NoInsets / NoScroll / ScrollBehavior / Overscroll
    // ---------------------------------------------------------------------------------------------

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAppShellShouldRespectNoInsets(bool noInsets)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.NoInsets, noInsets);
        });

        Assert.AreEqual(noInsets, component.Find(".bit-ash").ClassList.Contains("bit-ash-nin"));
    }

    [TestMethod]
    public void BitAppShellShouldRespectNoInsetsChangingAfterRender()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.IsFalse(component.Find(".bit-ash").ClassList.Contains("bit-ash-nin"));

        component.Render(parameters => parameters.Add(p => p.NoInsets, true));

        Assert.IsTrue(component.Find(".bit-ash").ClassList.Contains("bit-ash-nin"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAppShellShouldRespectNoScroll(bool noScroll)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.NoScroll, noScroll);
        });

        Assert.AreEqual(noScroll, component.Find(".bit-ash-main").ClassList.Contains("bit-ash-nsc"));
    }

    [TestMethod]
    public void BitAppShellNoScrollShouldSetUpTheBrowserSide()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.NoScroll, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.setup");

        Assert.IsTrue(component.Find(".bit-ash-main").ClassList.Contains("bit-ash-nsc"));
    }

    [TestMethod,
        DataRow(null, true),
        DataRow(BitScrollBehavior.Smooth, true),
        DataRow(BitScrollBehavior.Instant, false),
        DataRow(BitScrollBehavior.Auto, false)
    ]
    public void BitAppShellShouldRespectScrollBehavior(BitScrollBehavior? behavior, bool smooth)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.ScrollBehavior, behavior);
        });

        Assert.AreEqual(smooth, component.Find(".bit-ash-main").ClassList.Contains("bit-ash-smt"));
    }

    [TestMethod]
    public void BitAppShellShouldRespectScrollBehaviorChangingAfterRender()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.IsTrue(component.Find(".bit-ash-main").ClassList.Contains("bit-ash-smt"));

        component.Render(parameters => parameters.Add(p => p.ScrollBehavior, BitScrollBehavior.Instant));

        Assert.IsFalse(component.Find(".bit-ash-main").ClassList.Contains("bit-ash-smt"));
    }

    [TestMethod,
        DataRow(BitOverscroll.Auto, "overscroll-behavior:auto"),
        DataRow(BitOverscroll.Contain, "overscroll-behavior:contain"),
        DataRow(BitOverscroll.None, "overscroll-behavior:none")
    ]
    public void BitAppShellShouldRespectOverscroll(BitOverscroll overscroll, string expected)
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Overscroll, overscroll);
        });

        Assert.Contains(expected, component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void BitAppShellShouldNotWriteAnOverscrollStyleByDefault()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.IsFalse((component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty).Contains("overscroll-behavior"));
    }

    [TestMethod]
    public void BitAppShellOverscrollShouldBeAppendedToTheMainStyles()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.Overscroll, BitOverscroll.Contain);
            parameters.Add(p => p.Styles, new BitAppShellClassStyles { Main = "padding:1rem" });
        });

        var style = component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty;

        Assert.Contains("padding:1rem", style);
        Assert.Contains("overscroll-behavior:contain", style);
    }

    [TestMethod]
    public void BitAppShellMainShouldKeepItsClassesAlongsideTheClassesParameter()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.NoScroll, true);
            parameters.Add(p => p.Classes, new BitAppShellClassStyles { Main = "custom-main" });
        });

        var main = component.Find(".bit-ash-main");

        Assert.IsTrue(main.ClassList.Contains("bit-ash-main"));
        Assert.IsTrue(main.ClassList.Contains("bit-ash-smt"));
        Assert.IsTrue(main.ClassList.Contains("bit-ash-nsc"));
        Assert.IsTrue(main.ClassList.Contains("custom-main"));
    }

    // ---------------------------------------------------------------------------------------------
    //  The scrolling API
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task BitAppShellShouldCallGoToBottom()
    {
        var component = RenderComponent<BitAppShell>();

        await component.Instance.GoToBottom();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.goToBottom");
    }

    [TestMethod,
        DataRow(BitScrollBehavior.Auto, "auto"),
        DataRow(BitScrollBehavior.Instant, "instant"),
        DataRow(BitScrollBehavior.Smooth, "smooth"),
        DataRow(null, null)
    ]
    public async Task BitAppShellShouldPassTheBehaviorOfGoToBottom(BitScrollBehavior? behavior, string expected)
    {
        var component = RenderComponent<BitAppShell>();

        await component.Instance.GoToBottom(behavior);

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Extras.goToBottom"].Single();

        Assert.AreEqual(expected, invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task BitAppShellShouldCallScrollTo()
    {
        var component = RenderComponent<BitAppShell>();

        await component.Instance.ScrollTo(null, 240);

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Extras.scrollTo"].Single();

        Assert.IsNull(invocation.Arguments[1]);
        Assert.AreEqual(240d, invocation.Arguments[2]);
    }

    [TestMethod]
    public async Task BitAppShellShouldCallScrollBy()
    {
        var component = RenderComponent<BitAppShell>();

        await component.Instance.ScrollBy(10, 20, BitScrollBehavior.Instant);

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Extras.scrollBy"].Single();

        Assert.AreEqual(10m, invocation.Arguments[1]);
        Assert.AreEqual(20m, invocation.Arguments[2]);
        Assert.AreEqual("instant", invocation.Arguments[3]);
    }

    [TestMethod]
    public async Task BitAppShellShouldCallScrollToElement()
    {
        var component = RenderComponent<BitAppShell>();

        await component.Instance.ScrollToElement("row-9", 12, false, BitScrollAlignment.Center);

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.ScrollablePane.scrollToElement"].Single();

        Assert.AreEqual("row-9", invocation.Arguments[1]);
        Assert.AreEqual(12d, invocation.Arguments[2]);
        Assert.AreEqual(false, invocation.Arguments[3]);
        Assert.AreEqual("center", invocation.Arguments[4]);
    }

    [TestMethod]
    public async Task BitAppShellShouldNotCallScrollToElementWithoutAnElementId()
    {
        var component = RenderComponent<BitAppShell>();

        await component.Instance.ScrollToElement(" ");

        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.ScrollablePane.scrollToElement"));
    }

    [TestMethod]
    public async Task BitAppShellShouldReadTheScrollOffset()
    {
        var expected = new BitScrollOffset { Top = 120, ScrollHeight = 1000, ClientHeight = 400 };

        Context.JSInterop.Setup<BitScrollOffset?>("BitBlazorUI.ScrollablePane.getOffset", _ => true).SetResult(expected);

        var component = RenderComponent<BitAppShell>();

        var offset = await component.Instance.GetScrollOffset();

        Assert.IsNotNull(offset);
        Assert.AreEqual(120d, offset!.Top);
        Assert.AreEqual(600d, offset.MaxTop);
        Assert.IsFalse(offset.AtTop);
    }

    [TestMethod]
    public async Task BitAppShellShouldClearThePersistedScroll()
    {
        var component = RenderComponent<BitAppShell>();

        await component.Instance.ClearPersistedScroll();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.clearScrolls");
    }

    // ---------------------------------------------------------------------------------------------
    //  Scroll reporting
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitAppShellShouldNotSetUpTheBrowserSideWithoutAnyScrollCallback()
    {
        RenderComponent<BitAppShell>();

        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.ScrollablePane.setup"));
    }

    [TestMethod]
    public void BitAppShellShouldSetUpTheBrowserSideWhenAScrollCallbackIsHandled()
    {
        RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScroll, EventCallback.Factory.Create<BitScrollOffset>(this, _ => { }));
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.setup");
    }

    [TestMethod]
    public void BitAppShellShouldSetUpTheBrowserSideForEachOfTheScrollCallbacks()
    {
        RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScrollStart, EventCallback.Factory.Create<BitScrollOffset>(this, _ => { }));
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.setup");
    }

    [TestMethod]
    public void BitAppShellShouldSetUpTheBrowserSideForTheReachedCallbacks()
    {
        RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnReachedBottom, EventCallback.Factory.Create(this, () => { }));
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.setup");
    }

    [TestMethod]
    public void BitAppShellShouldSetUpTheBrowserSideOnlyOnceForTheSameOptions()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScroll, EventCallback.Factory.Create<BitScrollOffset>(this, _ => { }));
        });

        component.Render();
        component.Render();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.ScrollablePane.setup"].Count);
        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.ScrollablePane.update"));
    }

    [TestMethod]
    public void BitAppShellShouldUpdateTheBrowserSideWhenTheOptionsChange()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScroll, EventCallback.Factory.Create<BitScrollOffset>(this, _ => { }));
            parameters.Add(p => p.ReachOffset, 0);
        });

        component.Render(parameters => parameters.Add(p => p.ReachOffset, 32));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.update");
    }

    [TestMethod]
    public void BitAppShellShouldDisposeTheBrowserSideWhenTheLastScrollCallbackIsTakenAway()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScroll, EventCallback.Factory.Create<BitScrollOffset>(this, _ => { }));
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.setup");

        component.Render(parameters => parameters.Add(p => p.OnScroll, default(EventCallback<BitScrollOffset>)));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.dispose");
    }

    [TestMethod]
    public async Task BitAppShellShouldRaiseOnScrollFromTheBrowserSide()
    {
        BitScrollOffset? reported = null;

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScroll, EventCallback.Factory.Create<BitScrollOffset>(this, o => reported = o));
        });

        await component.Instance._OnScroll(new BitScrollOffset { Top = 42 });

        Assert.IsNotNull(reported);
        Assert.AreEqual(42d, reported!.Top);
    }

    [TestMethod]
    public async Task BitAppShellShouldRaiseOnScrollStartAndOnScrollEndFromTheBrowserSide()
    {
        var started = false;
        var ended = false;

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScrollStart, EventCallback.Factory.Create<BitScrollOffset>(this, _ => started = true));
            parameters.Add(p => p.OnScrollEnd, EventCallback.Factory.Create<BitScrollOffset>(this, _ => ended = true));
        });

        await component.Instance._OnScrollStart(new BitScrollOffset());
        await component.Instance._OnScrollEnd(new BitScrollOffset());

        Assert.IsTrue(started);
        Assert.IsTrue(ended);
    }

    [TestMethod]
    public async Task BitAppShellShouldIgnoreANullOffsetFromTheBrowserSide()
    {
        var raised = false;

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScroll, EventCallback.Factory.Create<BitScrollOffset>(this, _ => raised = true));
        });

        await component.Instance._OnScroll(null!);

        Assert.IsFalse(raised);
    }

    [TestMethod,
        DataRow("top"),
        DataRow("bottom")
    ]
    public async Task BitAppShellShouldRouteTheReachedEdgeToItsOwnCallback(string edge)
    {
        var reached = string.Empty;

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnReachedTop, EventCallback.Factory.Create(this, () => reached = "top"));
            parameters.Add(p => p.OnReachedBottom, EventCallback.Factory.Create(this, () => reached = "bottom"));
        });

        await component.Instance._OnReached(edge);

        Assert.AreEqual(edge, reached);
    }

    [TestMethod]
    public async Task BitAppShellShouldIgnoreAnUnknownReachedEdge()
    {
        var reached = false;

        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnReachedTop, EventCallback.Factory.Create(this, () => reached = true));
        });

        await component.Instance._OnReached("left");

        Assert.IsFalse(reached);
    }

    [TestMethod]
    public void BitAppShellShouldDisposeTheBrowserSideOfTheScrollReporting()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.OnScroll, EventCallback.Factory.Create<BitScrollOffset>(this, _ => { }));
        });

        component.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.dispose");
    }

    // ---------------------------------------------------------------------------------------------
    //  Navigation
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitAppShellShouldNotGoToTopOnAFragmentOnlyNavigation()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/page");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.goToTop");

        var before = Context.JSInterop.Invocations["BitBlazorUI.Extras.goToTop"].Count;

        InvokeLocationChanged(component.Instance, "https://example.com/page#section-2");

        Assert.AreEqual(before, Context.JSInterop.Invocations["BitBlazorUI.Extras.goToTop"].Count);
    }

    [TestMethod]
    public void BitAppShellShouldGoToTopWhenTheFragmentIsLeftBehind()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/page");
        InvokeLocationChanged(component.Instance, "https://example.com/page#section-2");
        InvokeLocationChanged(component.Instance, "https://example.com/other");

        // The first and the last are real navigations; the fragment-only one in between is not.
        Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Extras.goToTop"].Count);
    }

    [TestMethod]
    public void BitAppShellShouldNotPersistScrollOnAFragmentOnlyNavigation()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/page");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.locationChangedScroll");

        InvokeLocationChanged(component.Instance, "https://example.com/page#section-2");

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.AppShell.locationChangedScroll"].Count);
    }

    [TestMethod]
    public void BitAppShellShouldGoToTopWithTheScrollBehaviorItWasGiven()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
            parameters.Add(p => p.ScrollBehavior, BitScrollBehavior.Smooth);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/other");

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Extras.goToTop"].Single();

        Assert.AreEqual("smooth", invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitAppShellAutoGoToTopShouldDefaultToAnInstantMove()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
        });

        InvokeLocationChanged(component.Instance, "https://example.com/other");

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Extras.goToTop"].Single();

        Assert.AreEqual("instant", invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitAppShellShouldSubscribeWhenAutoGoToTopIsTurnedOnAfterRender()
    {
        var component = RenderComponent<BitAppShell>();

        InvokeLocationChanged(component.Instance, "https://example.com/one");

        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.Extras.goToTop"));

        component.Render(parameters => parameters.Add(p => p.AutoGoToTop, true));

        InvokeLocationChanged(component.Instance, "https://example.com/two");

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Extras.goToTop");
    }

    [TestMethod]
    public void BitAppShellShouldUnsubscribeWhenBothNavigationFeaturesAreTurnedOffAfterRender()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AutoGoToTop, true);
        });

        component.Render(parameters => parameters.Add(p => p.AutoGoToTop, false));

        InvokeLocationChanged(component.Instance, "https://example.com/two");

        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.Extras.goToTop"));
    }

    [TestMethod]
    public void BitAppShellShouldInitScrollWhenPersistScrollIsTurnedOnAfterRender()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.AppShell.initScroll"));

        component.Render(parameters => parameters.Add(p => p.PersistScroll, true));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.initScroll");
    }

    [TestMethod]
    public void BitAppShellShouldInitScrollOnlyOnce()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        component.Render();
        component.Render();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.AppShell.initScroll"].Count);
    }

    // ---------------------------------------------------------------------------------------------
    //  Keyboard inset
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitAppShellShouldNotTrackTheKeyboardByDefault()
    {
        RenderComponent<BitAppShell>();

        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.AppShell.setupKeyboard"));
    }

    [TestMethod]
    public void BitAppShellShouldTrackTheKeyboardWhenAvoidKeyboardIsSet()
    {
        RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AvoidKeyboard, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.setupKeyboard");
    }

    [TestMethod]
    public void BitAppShellShouldTrackTheKeyboardOnlyOnce()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AvoidKeyboard, true);
        });

        component.Render();
        component.Render();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.AppShell.setupKeyboard"].Count);
    }

    [TestMethod]
    public void BitAppShellShouldStartTrackingTheKeyboardWhenAvoidKeyboardIsTurnedOnAfterRender()
    {
        var component = RenderComponent<BitAppShell>();

        Assert.IsFalse(Context.JSInterop.Invocations.Identifiers.Contains("BitBlazorUI.AppShell.setupKeyboard"));

        component.Render(parameters => parameters.Add(p => p.AvoidKeyboard, true));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.setupKeyboard");
    }

    [TestMethod]
    public void BitAppShellShouldStopTrackingTheKeyboardWhenAvoidKeyboardIsTurnedOffAfterRender()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AvoidKeyboard, true);
        });

        component.Render(parameters => parameters.Add(p => p.AvoidKeyboard, false));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.disposeKeyboard");
    }

    [TestMethod]
    public void BitAppShellShouldStopTrackingTheKeyboardOnDispose()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.AvoidKeyboard, true);
        });

        component.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.disposeKeyboard");
    }

    [TestMethod]
    public void BitAppShellShouldStopPersistingScrollWhenPersistScrollIsTurnedOffAfterRender()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.initScroll");

        component.Render(parameters => parameters.Add(p => p.PersistScroll, false));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AppShell.disposeScroll");
    }

    [TestMethod]
    public void BitAppShellShouldPersistScrollAgainAfterItWasTurnedOffAndOn()
    {
        var component = RenderComponent<BitAppShell>(parameters =>
        {
            parameters.Add(p => p.PersistScroll, true);
        });

        component.Render(parameters => parameters.Add(p => p.PersistScroll, false));
        component.Render(parameters => parameters.Add(p => p.PersistScroll, true));

        Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.AppShell.initScroll"].Count);
    }

    private static void InvokeLocationChanged(BitAppShell instance, string uri)
    {
        var method = instance.GetType().GetMethod("LocationChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        method!.Invoke(instance, [null, new LocationChangedEventArgs(uri, false)]);
    }
}
