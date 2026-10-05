using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

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
    public void BitPanelStylesheetShouldOnlyReadItsPublicVariables()
    {
        var stylesheet = ReadStylesheet();

        // A public variable declared by the component would stop the value set on an ancestor from inheriting.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"--bit-Panel-[\w-]+\s*:"), "A public variable is declared by the stylesheet.");

        var documented = Regex.Matches(stylesheet, @"^//   (--bit-Panel-[\w-]+)", RegexOptions.Multiline)
                              .Select(m => m.Groups[1].Value)
                              .ToArray();

        Assert.IsTrue(documented.Length > 0);

        foreach (var name in documented)
        {
            Assert.IsTrue(Regex.IsMatch(stylesheet, $@"var\({Regex.Escape(name)}\)|var\({Regex.Escape(name)},"),
                          $"{name} is documented but never read.");
        }

        var read = Regex.Matches(stylesheet, @"var\((--bit-Panel-[\w-]+)[,)]").Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented.");
        }
    }

    [TestMethod]
    public void BitPanelOpenStateShouldCarryNoTransform()
    {
        var stylesheet = ReadStylesheet();
        var start = stylesheet.IndexOf("\n.bit-pnl-opn {", StringComparison.Ordinal);
        var open = stylesheet[start..stylesheet.IndexOf("\n}", start + 1, StringComparison.Ordinal)];

        // Any transform - an identity one included - makes the open panel the containing block of its fixed
        // descendants, which lays a panel or a dialog opened from inside it out against the panel.
        StringAssert.Contains(open, "transform: none;");
    }

    [TestMethod]
    [DataRow(".bit-pnl-start", "top,bottom,left,right")]
    [DataRow(".bit-pnl-end", "top,bottom,left,right")]
    [DataRow(".bit-pnl-left", "top,bottom,left,right")]
    [DataRow(".bit-pnl-right", "top,bottom,left,right")]
    [DataRow(".bit-pnl-top", "top,left,right")]
    [DataRow(".bit-pnl-bottom", "bottom,left,right")]
    [DataRow(".bit-pnl-fsz", "top,right,bottom,left")]
    public void BitPanelEdgesShouldMakeRoomForTheSafeAreaTheyTouch(string edge, string insets)
    {
        var stylesheet = ReadStylesheet();
        var start = stylesheet.IndexOf($"\n{edge} {{", StringComparison.Ordinal);
        var rule = stylesheet[start..stylesheet.IndexOf("\n}", start + 1, StringComparison.Ordinal)];

        foreach (var inset in insets.Split(','))
        {
            StringAssert.Contains(rule, $"var(--bit-pnl-sa-{inset})", $"{edge} does not make room for the {inset} inset.");

            StringAssert.Contains(stylesheet, $"    --bit-pnl-sa-{inset}: env(safe-area-inset-{inset}, 0px);");

            // A panel laid out inside a box of the page touches no device edge.
            StringAssert.Contains(RuleOf(stylesheet, ".bit-pnl-abs"), $"    --bit-pnl-sa-{inset}: 0px;");
        }
    }

    // The inset is taken away through the variables rather than by a padding on the container, which would
    // outrank one the consumer gives the container through Classes.
    [TestMethod]
    public void BitPanelLaidOutInABoxShouldLeaveThePaddingOfTheContainerToTheConsumer()
    {
        StringAssert.DoesNotMatch(ReadStylesheet(), new Regex(@"\.bit-pnl-cnt\s*\{\s*padding"));
    }

    // The direction is read off the layout as well as off the class Dir renders: the one so a panel given no
    // direction of its own on a right-to-left page still slides out towards the edge it came from, the other
    // so an explicit right-to-left still does on an engine without :dir(), each in a rule of its own.
    [TestMethod]
    public void BitPanelShouldSlideTheWayThePageItIsInReads()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "    &:dir(rtl) {\n        --bit-pnl-transform-factor: -1;\n    }");
        StringAssert.Contains(stylesheet, "    &.bit-rtl {\n        --bit-pnl-transform-factor: -1;\n    }");
    }

    private static string RuleOf(string stylesheet, string selector)
    {
        var start = stylesheet.IndexOf($"\n{selector} {{", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"Missing {selector}.");

        return stylesheet[start..stylesheet.IndexOf("\n}", start + 1, StringComparison.Ordinal)];
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Surfaces", "Panel", "BitPanel.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
