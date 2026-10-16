using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

/// <summary>
/// Covers the BitParams cascade of the Panel: what a BitPanelParams fills in, what it leaves alone because the panel
/// wrote it for itself, that every plain parameter has a counterpart on the params object and every property of it
/// reaches the panel, and that what the panel decides from its parameters - its chrome, its dismissals, the geometry
/// of its swipe gesture - follows the cascaded values.
/// </summary>
[TestClass]
public class BitPanelParamsTests : BunitTestContext
{
    [TestMethod]
    public void BitPanelParamsShouldHaveCorrectParamName()
    {
        var @params = new BitPanelParams();

        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitPanel)}", BitPanelParams.ParamName);
        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitPanelParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitPanelShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitPanelParams
                {
                    Placement = BitPlacement.Start,
                    Size = 420,
                    ModeFull = true,
                    ShowCloseButton = true,
                    CloseButtonTitle = "Dismiss",
                    AbsolutePosition = true,
                    ZIndex = 1500,
                    Classes = new() { Root = "cascaded-root", Container = "cascaded-container" },
                    Styles = new() { Root = "margin: 1px;" },
                }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPanel>(0);
                builder.AddAttribute(1, nameof(BitPanel.IsOpen), true);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-pnl");
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-pnl-abs"));

        var rootStyle = root.GetAttribute("style")!;
        StringAssert.Contains(rootStyle, "margin: 1px;");
        StringAssert.Contains(rootStyle, "--bit-pnl-zin-ovl:1500");

        var container = component.Find(".bit-pnl-cnt");
        Assert.IsTrue(container.ClassList.Contains("bit-pnl-start"));
        Assert.IsTrue(container.ClassList.Contains("cascaded-container"));
        StringAssert.Contains(container.GetAttribute("style"), "width:420px");

        Assert.IsTrue(component.Find(".bit-pnl-ovl").ClassList.Contains("bit-pnl-ovl-mfl"));
        Assert.AreEqual("Dismiss", component.Find(".bit-pnl-cls").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPanelOwnParametersShouldWinOverCascadingParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitPanelParams { Placement = BitPlacement.Start, ModeFull = true, Modeless = false, CloseButtonTitle = "Dismiss", ShowCloseButton = true }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPanel>(0);
                builder.AddAttribute(1, nameof(BitPanel.IsOpen), true);
                builder.AddAttribute(2, nameof(BitPanel.Placement), BitPlacement.Bottom);
                builder.AddAttribute(3, nameof(BitPanel.ModeFull), false);
                builder.AddAttribute(4, nameof(BitPanel.CloseButtonTitle), "Close it");
                builder.CloseComponent();
            });
        });

        var container = component.Find(".bit-pnl-cnt");
        Assert.IsTrue(container.ClassList.Contains("bit-pnl-bottom"));
        Assert.IsFalse(container.ClassList.Contains("bit-pnl-start"));

        // A false the panel wrote for itself is a value it set, not one it left to the cascade.
        Assert.IsFalse(component.Find(".bit-pnl-ovl").ClassList.Contains("bit-pnl-ovl-mfl"));

        // What it left unset still comes from the cascade.
        Assert.AreEqual("Close it", component.Find(".bit-pnl-cls").GetAttribute("aria-label"));
    }

    // A cascaded Modeless is read by everything the panel decides from it, not only by the markup: the overlay
    // is gone and the dialog is no longer reported as modal.
    [TestMethod]
    public void BitPanelShouldTakeCascadedBehaviorParams()
    {
        var component = RenderComponent<CascadingValue<BitPanelParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitPanelParams.ParamName);
            parameters.Add(p => p.Value, new BitPanelParams { Modeless = true, Role = "complementary" });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitPanel>(0);
                builder.AddAttribute(1, nameof(BitPanel.IsOpen), true);
                builder.CloseComponent();
            }));
        });

        Assert.AreEqual(0, component.FindAll(".bit-pnl-ovl").Count);

        var container = component.Find(".bit-pnl-cnt");
        Assert.AreEqual("complementary", container.GetAttribute("role"));
        Assert.IsFalse(container.HasAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitPanelParamsShouldCarryEveryPlainParameterOfTheComponent()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks and templates are deliberately left out, and so is what belongs to one panel alone:
        // whether it is open, the texts of its header and footer, and the ids of what names and describes it.
        var paramsProperties = typeof(BitPanelParams).GetProperties().Select(p => p.Name).ToHashSet();
        paramsProperties.UnionWith(
        [
            nameof(BitPanel.IsOpen),
            nameof(BitPanel.HeaderText),
            nameof(BitPanel.FooterText),
            nameof(BitPanel.TitleAriaId),
            nameof(BitPanel.SubtitleAriaId),
        ]);

        var missing = typeof(BitPanel).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                      .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                      .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                      .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                      .Select(p => p.Name)
                                      .Where(n => paramsProperties.Contains(n) is false)
                                      .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }

    [TestMethod]
    public void BitPanelParamsShouldApplyEveryPropertyItCarries()
    {
        // The other direction: a property added here but forgotten in UpdateParameters is accepted by the cascade
        // and then never reaches the panel.
        var panelParams = new BitPanelParams();
        var panel = new BitPanel();

        var properties = typeof(BitPanelParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                               .Where(p => p.CanWrite)
                                               .ToArray();

        foreach (var property in properties)
        {
            var target = typeof(BitPanel).GetProperty(property.Name)!;

            property.SetValue(panelParams, SampleValue(property.PropertyType, target.GetValue(panel)));
        }

        panelParams.UpdateParameters(panel);

        foreach (var property in properties)
        {
            var target = typeof(BitPanel).GetProperty(property.Name)!;

            Assert.AreEqual(property.GetValue(panelParams), target.GetValue(panel), $"{property.Name} is not applied.");
        }

        // A value unlike the component's default, so the assertion above cannot pass by coincidence.
        static object SampleValue(Type type, object? current)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool)) return current is true ? false : true;
            if (underlying == typeof(int)) return 7;
            if (underlying == typeof(double)) return 7d;
            if (underlying == typeof(decimal)) return 0.5m;
            if (underlying == typeof(string)) return "sample";
            if (underlying == typeof(ElementReference)) return new ElementReference("sample");
            if (underlying == typeof(BitIconInfo)) return BitIconInfo.Css("sample");
            if (underlying == typeof(BitPanelClassStyles)) return new BitPanelClassStyles();
            if (underlying.IsEnum)
            {
                var values = Enum.GetValues(underlying);
                return values.GetValue(values.Length - 1)!;
            }

            throw new NotSupportedException($"Add a sample value for {type}.");
        }
    }

    [TestMethod]
    public void BitPanelShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitPanelParams { Dir = BitDir.Rtl, Disabled = true, Class = "cascaded" });

        var root = component.Find(".bit-pnl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
    }

    // A cascaded dismissal setting is read by the handler that decides on it, not only by the markup.
    [TestMethod]
    public void BitPanelShouldHonourACascadedNoDismissOnEscapeAndBlocking()
    {
        var isOpen = true;

        var component = RenderWithParams(new BitPanelParams { NoDismissOnEscape = true, Blocking = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPanel.IsOpen), isOpen);
            builder.AddAttribute(2, nameof(BitPanel.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => isOpen = v));
        });

        component.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.IsTrue(isOpen);

        component.Find(".bit-pnl-ovl").Click();
        Assert.IsTrue(isOpen);
    }

    // The geometry the swipe gesture is registered with is decided from parameters the cascade may be giving.
    [TestMethod]
    public void BitPanelShouldRegisterTheSwipeGestureWithTheCascadedGeometry()
    {
        var component = RenderWithParams(new BitPanelParams { Placement = BitPlacement.Top, SwipeTrigger = 0.6m, Dir = BitDir.Rtl });

        component.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count));

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments;

        Assert.AreEqual(0.6m, arguments[1]);
        Assert.AreEqual("top", arguments[2]);
        Assert.AreEqual(true, arguments[3]);
        Assert.AreEqual(BitSwipeOrientation.Vertical, arguments[4]);
    }

    [TestMethod]
    public void BitPanelShouldRegisterNoSwipeGestureWhenTheCascadeTurnsItOff()
    {
        RenderWithParams(new BitPanelParams { NoSwipe = true });

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count);
    }

    [TestMethod]
    public async Task BitPanelShouldApplyTheCascadedChromeParameters()
    {
        var component = RenderWithParams(new BitPanelParams
        {
            ShowCloseButton = true,
            CloseIconName = "ChromeClose",
            FullSize = true,
            IsAlert = true,
            KeepMounted = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPanel.IsOpen), true);
            // Bound, so the panel is free to close itself below (a one-way IsOpen is left as it was given).
            builder.AddAttribute(2, nameof(BitPanel.IsOpenChanged), EventCallback.Factory.Create<bool>(this, _ => { }));
            builder.AddAttribute(3, nameof(BitPanel.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"kept\">Kept</div>")));
        });

        var container = component.Find(".bit-pnl-cnt");

        Assert.AreEqual("alertdialog", container.GetAttribute("role"));
        Assert.IsTrue(container.ClassList.Contains("bit-pnl-fsz"));
        Assert.IsTrue(component.Find(".bit-pnl-cli").ClassList.Contains("bit-icon--ChromeClose"));

        // KeepMounted is read once the panel has slid away, so the cascaded value has to reach that decision too.
        var panel = component.FindComponent<BitPanel>();

        await panel.InvokeAsync(() => panel.Instance.Close());
        await panel.InvokeAsync(() => panel.Instance._OnTransitionEnd());

        Assert.IsFalse(panel.Instance.IsOpen);
        Assert.AreEqual(1, component.FindAll(".kept").Count);
    }

    [TestMethod]
    public void BitPanelShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitPanelParams(), builder =>
        {
            builder.AddAttribute(1, nameof(BitPanel.IsOpen), true);
        });

        var root = component.Find(".bit-pnl");
        var container = component.Find(".bit-pnl-cnt");

        Assert.AreEqual("bit-pnl", root.ClassName);
        Assert.IsTrue(string.IsNullOrEmpty(root.GetAttribute("style")));
        Assert.AreEqual("bit-pnl-cnt bit-pnl-end bit-pnl-opn", container.ClassName);
        Assert.AreEqual("dialog", container.GetAttribute("role"));
        Assert.AreEqual(0, component.FindAll(".bit-pnl-cls").Count);
        Assert.IsFalse(component.Find(".bit-pnl-ovl").ClassList.Contains("bit-pnl-ovl-mfl"));
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitPanelParams panelParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { panelParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPanel>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }
}
