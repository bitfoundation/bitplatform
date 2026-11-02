using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.OptionsProvider;

[TestClass]
public class BitAiOptionsProviderTests : BitAiTestBase
{
    private sealed class GermanStrings : BitAiStrings
    {
        public override string Thinking => "Denke nach";
    }

    private sealed class FrenchStrings : BitAiStrings
    {
        public override string Thinking => "Réflexion";
    }

    private static readonly BitAiUiOptions German = BitAiUiOptions.Default with { Strings = new GermanStrings() };

    private static readonly BitAiUiOptions French = BitAiUiOptions.Default with { Strings = new FrenchStrings() };

    // An AI component that shows which options reached it.
    private sealed class OptionsProbe : BitAiComponentBase
    {
        protected override string RootElementClass => "probe";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "probe");
            builder.AddContent(2, Options.SubmitOnEnter ? "enter" : "ctrl-enter");
            builder.CloseElement();
        }
    }

    [TestMethod]
    public void TheOptionsReachTheAiComponentsInside()
    {
        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, German)
            .AddChildContent<BitAiThinkingIndicator>());

        Assert.AreEqual("Denke nach", provider.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void ANewOptionsRecordReachesTheComponentsThatReadIt()
    {
        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, BitAiUiOptions.Default with { SubmitOnEnter = true })
            .AddChildContent<OptionsProbe>());

        Assert.AreEqual("enter", provider.Find(".probe").TextContent);

        provider.Render(p => p.Add(c => c.UiOptions, BitAiUiOptions.Default with { SubmitOnEnter = false }));

        Assert.AreEqual("ctrl-enter", provider.Find(".probe").TextContent);
    }

    [TestMethod]
    public void AnInnerProviderOverridesTheOuterOneForItsOwnContentOnly()
    {
        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, German)
            .AddChildContent(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "outer");
                builder.OpenComponent<BitAiThinkingIndicator>(2);
                builder.CloseComponent();
                builder.CloseElement();

                builder.OpenElement(3, "div");
                builder.AddAttribute(4, "class", "inner");
                builder.OpenComponent<BitAiOptionsProvider>(5);
                builder.AddAttribute(6, nameof(BitAiOptionsProvider.UiOptions), French);
                builder.AddAttribute(7, nameof(BitAiOptionsProvider.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitAiThinkingIndicator>(0);
                    b.CloseComponent();
                }));
                builder.CloseComponent();
                builder.CloseElement();
            }));

        Assert.AreEqual("Denke nach", provider.Find(".outer [role=status]").TextContent.Trim());
        Assert.AreEqual("Réflexion", provider.Find(".inner [role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void AProviderWithoutOptionsHandsOnTheOuterOnes()
    {
        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, German)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiOptionsProvider>(0);
                builder.AddAttribute(1, nameof(BitAiOptionsProvider.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitAiThinkingIndicator>(0);
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }));

        Assert.AreEqual("Denke nach", provider.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void AProviderWithoutOptionsAndNoneAroundItCascadesTheRegisteredOnes()
    {
        Services.AddSingleton(French);

        var provider = RenderComponent<BitAiOptionsProvider>(p => p.AddChildContent<BitAiThinkingIndicator>());

        Assert.AreEqual("Réflexion", provider.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void WithoutAProviderTheDefaultsApply()
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>();

        Assert.AreEqual("Thinking", indicator.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void TheProviderRendersNoElementOfItsOwn()
    {
        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, German)
            .AddChildContent("<p class='content'>hello</p>"));

        provider.MarkupMatches("<p class='content'>hello</p>");
    }
}
