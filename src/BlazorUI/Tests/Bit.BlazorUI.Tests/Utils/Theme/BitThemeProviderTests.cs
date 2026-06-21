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
    public void WrapperIsLayoutNeutralByDefault()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#ABCDEF";

        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.Add(p => p.RootElement, "section");
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        var style = component.Find("section").GetAttribute("style") ?? string.Empty;

        // The wrapper must not introduce a layout box of its own; display:contents keeps it
        // transparent to the surrounding layout while the CSS vars still cascade to descendants.
        StringAssert.StartsWith(style, "display:contents", StringComparison.Ordinal);
        Assert.IsTrue(style.Contains("--bit-clr-pri:#ABCDEF", StringComparison.Ordinal),
            $"CSS vars must still be emitted after the layout-neutral display. Actual: {style}");
    }

    [TestMethod]
    public void UserSuppliedDisplayOverridesLayoutNeutralDefault()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#ABCDEF";

        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.Add(p => p.RootElement, "section");
            parameters.AddUnmatched("style", "display:flex;gap:8px");
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        var style = component.Find("section").GetAttribute("style") ?? string.Empty;

        // The user's style is appended last, so on the conflicting `display` property it wins
        // (last declaration in an inline style block takes effect).
        StringAssert.StartsWith(style, "display:contents", StringComparison.Ordinal);
        StringAssert.EndsWith(style, "display:flex;gap:8px", StringComparison.Ordinal);
        Assert.IsTrue(style.Contains("--bit-clr-pri:#ABCDEF", StringComparison.Ordinal));
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
    public void InnerProviderWithoutThemeOrThemeNameAddsNoWrapperElement()
    {
        const string ParentPrimary = "#AABBCC";

        var parent = new BitTheme();
        parent.Color.Primary.Main = ParentPrimary;

        var component = RenderComponent<BitThemeProviderTestHost>(parameters =>
        {
            parameters.Add(p => p.OuterTheme, parent);
            parameters.Add(p => p.InnerTheme, null);
        });

        // The outer provider applies a Theme so it renders exactly one wrapper element. The inner
        // provider (Theme null, no ThemeName) must NOT add a second wrapper — it relies on the
        // ancestor's existing unnamed cascade — while the probe still resolves the parent theme.
        Assert.AreEqual(1, component.FindAll("div").Count);
        Assert.AreEqual(ParentPrimary, component.Find("span").GetAttribute("data-primary"));
    }

    [TestMethod]
    public void UpdatesCascadeWhenThemeReferenceChanges()
    {
        var first = new BitTheme();
        first.Color.Primary.Main = "#111111";

        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, first);
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        Assert.AreEqual("#111111", component.Find("span").GetAttribute("data-primary"));

        // Assigning a new BitTheme reference recomputes the cascade and propagates the new value
        // to consumers (CascadingValue<BitTheme> change detection is reference-based).
        var second = new BitTheme();
        second.Color.Primary.Main = "#222222";
        component.SetParametersAndRender(parameters => parameters.Add(p => p.Theme, second));

        Assert.AreEqual("#222222", component.Find("span").GetAttribute("data-primary"));
    }

    [TestMethod]
    public void InPlaceThemeMutationUpdatesInlineCssVariablesOnReRender()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#111111";

        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.Add(p => p.RootElement, "section");
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        StringAssert.Contains(component.Find("section").GetAttribute("style"), "--bit-clr-pri:#111111");

        // Mutating the held BitTheme instance in place is reflected in the emitted CSS variables on
        // the next render: the provider rebuilds its inline style from the current token values
        // every parameters update (it does not rely on the Theme reference changing).
        theme.Color.Primary.Main = "#222222";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.Add(p => p.RootElement, "section");
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        StringAssert.Contains(component.Find("section").GetAttribute("style"), "--bit-clr-pri:#222222");
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
        // Parent's Secondary.Main is preserved because the inner theme does not override it.
        Assert.AreEqual(ParentSecondary, probe.GetAttribute("data-secondary"));
    }

    [TestMethod]
    public void FrozenSkipsRebuildButReCascadeStillReflectsReferenceChange()
    {
        var first = new BitTheme();
        first.Color.Primary.Main = "#111111";

        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Frozen, true);
            parameters.Add(p => p.Theme, first);
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        Assert.AreEqual("#111111", component.Find("span").GetAttribute("data-primary"));

        // A new reference is a real change even when Frozen: the provider must rebuild and propagate.
        var second = new BitTheme();
        second.Color.Primary.Main = "#222222";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Frozen, true);
            parameters.Add(p => p.Theme, second);
        });

        Assert.AreEqual("#222222", component.Find("span").GetAttribute("data-primary"));
    }

    [TestMethod]
    public void FrozenIgnoresInPlaceMutationOfTheSameThemeReference()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#111111";

        var component = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Frozen, true);
            parameters.Add(p => p.Theme, theme);
            parameters.Add(p => p.RootElement, "section");
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        StringAssert.Contains(component.Find("section").GetAttribute("style"), "--bit-clr-pri:#111111");

        // With Frozen, mutating the held instance in place is intentionally NOT detected: the
        // reference is unchanged so the cached merge + CSS string are reused. Callers opting into
        // Frozen must assign a new BitTheme instance to apply changes.
        theme.Color.Primary.Main = "#222222";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Frozen, true);
            parameters.Add(p => p.Theme, theme);
            parameters.Add(p => p.RootElement, "section");
            parameters.AddChildContent<ThemeProbeConsumer>();
        });

        var style = component.Find("section").GetAttribute("style") ?? string.Empty;
        StringAssert.Contains(style, "--bit-clr-pri:#111111");
        Assert.IsFalse(style.Contains("--bit-clr-pri:#222222", StringComparison.Ordinal),
            $"Frozen provider should not pick up in-place mutations. Actual: {style}");
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
