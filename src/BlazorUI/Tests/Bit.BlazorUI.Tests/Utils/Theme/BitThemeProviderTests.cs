using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeProviderTests : BunitTestContext
{
    [TestMethod]
    public void RendersChildContentDirectlyWhenNoThemeAndNoParent()
    {
        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.AddChildContent("<p>hi</p>");
        });

        // No Theme + no ParentTheme: the provider should not introduce any wrapping element.
        component.MarkupMatches("<p>hi</p>");
    }

    [TestMethod]
    public void EmitsRootElementAndStyleWhenThemeProvided()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#ABCDEF";

        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.Add(p => p.RootElement, "section");
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        var root = component.Find("section");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-clr-pri:#ABCDEF", StringComparison.Ordinal),
            $"Inline style missing primary token. Actual: {style}");

        var probe = component.Find("span");
        Assert.AreEqual("#ABCDEF", probe.GetAttribute("data-primary"));
    }

    [TestMethod]
    public void SplatsAdditionalAttributesOntoRootElement()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#111";

        var component = RenderComponent<BitThemeProviderHtmlAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
        });

        var root = component.Find("div");
        Assert.AreEqual("branded", root.GetAttribute("class"));
        Assert.AreEqual("bit", root.GetAttribute("data-val-test"));
        // Provider's own inline style is still emitted alongside the splatted attributes.
        Assert.IsTrue((root.GetAttribute("style") ?? string.Empty).Contains("--bit-clr-pri", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ReCascadesParentThemeWhenLocalThemeIsNull()
    {
        // Render <Outer Theme=parent> -> <Inner Theme=null> -> probe.
        // Previously, Inner with Theme=null broke the cascade entirely; the probe should still
        // see the parent theme.
        const string ParentPrimary = "#AABBCC";

        var parent = new BitTheme();
        parent.Color.Primary.Main = ParentPrimary;

        var component = RenderComponent<BitThemeProviderTestHost>(parameters =>
        {
            parameters.Add(p => p.OuterTheme, parent);
            parameters.Add(p => p.InnerTheme, null);
        });

        var probe = component.Find("span");
        Assert.AreEqual(ParentPrimary, probe.GetAttribute("data-primary"));
    }

    [TestMethod]
    public void InnerThemeMergesOverParentTheme()
    {
        const string ParentPrimary = "#AABBCC";
        const string ParentSecondary = "#DDEEFF";
        const string InnerPrimary = "#112233";

        var parent = new BitTheme();
        parent.Color.Primary.Main = ParentPrimary;
        parent.Color.Secondary.Main = ParentSecondary;

        var inner = new BitTheme();
        inner.Color.Primary.Main = InnerPrimary;

        var component = RenderComponent<BitThemeProviderTestHost>(parameters =>
        {
            parameters.Add(p => p.OuterTheme, parent);
            parameters.Add(p => p.InnerTheme, inner);
        });

        var probe = component.Find("span");
        // Inner Theme's Primary.Main should win.
        Assert.AreEqual(InnerPrimary, probe.GetAttribute("data-primary"));
    }

    /// <summary>
    /// Test host that nests two providers without depending on a shared bunit fixture builder for
    /// CascadingParameter wiring. The outer provider casts its theme; the inner provider receives
    /// it via [CascadingParameter] and either layers an InnerTheme or re-cascades the parent.
    /// </summary>
    public sealed class BitThemeProviderTestHost : ComponentBase
    {
        [Parameter] public BitTheme? OuterTheme { get; set; }
        [Parameter] public BitTheme? InnerTheme { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitThemeProvider>(0);
            builder.AddAttribute(1, nameof(BitThemeProvider.Theme), OuterTheme);
            builder.AddAttribute(2, nameof(BitThemeProvider.ChildContent), (RenderFragment)(b1 =>
            {
                b1.OpenComponent<BitThemeProvider>(0);
                b1.AddAttribute(1, nameof(BitThemeProvider.Theme), InnerTheme);
                b1.AddAttribute(2, nameof(BitThemeProvider.ChildContent), (RenderFragment)(b2 =>
                {
                    b2.OpenComponent<ThemeProbeConsumer>(0);
                    b2.CloseComponent();
                }));
                b1.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
