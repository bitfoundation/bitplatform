namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TextShimmer;

public partial class BitTextShimmerDemo
{
    private bool forceAnimation;

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "BaseColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The resting/dim color of the text. When null, a theme-aware default color is used.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to shimmer, which takes precedence over the Text parameter.",
        },
        new()
        {
            Name = "ContentLength",
            Type = "int",
            DefaultValue = "10",
            Description = "The character count used to scale the shimmer band width when the content is supplied using ChildContent.",
        },
        new()
        {
            Name = "Duration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The animation duration of one full shimmer sweep in ms.",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node.",
        },
        new()
        {
            Name = "ForceAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the shimmer animating even if the user has requested reduced motion (prefers-reduced-motion).",
        },
        new()
        {
            Name = "GradientColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The bright highlight color that sweeps across the text. When null, a theme-aware default color is used.",
        },
        new()
        {
            Name = "Spread",
            Type = "double",
            DefaultValue = "2",
            Description = "The shimmer band width multiplier. The effective band width (px) is Spread times the character count, so longer text gets a proportionally wider shine.",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to display, that is also used to scale the shimmer band width based on its character count.",
        },
    ];




    private readonly string example1RazorCode = @"
<BitTextShimmer Text=""Thinking about your question..."" />

<BitTextShimmer ForceAnimation Text=""This shimmer keeps animating even in reduced motion mode"" />";

    private readonly string example2RazorCode = @"
<BitTextShimmer Element=""h1"" Text=""A shimmering heading"" />

<div>An <BitTextShimmer Element=""span"" Text=""inline text shimmer"" /> in the middle of a sentence.</div>";

    private readonly string example3RazorCode = @"
<BitTextShimmer Duration=""4000"" Text=""Slow and calm shimmer (4 seconds)"" />

<BitTextShimmer Duration=""750"" Text=""Fast and urgent shimmer (750 milliseconds)"" />

<BitTextShimmer Spread=""5"" Text=""A wide shimmer band"" />

<BitTextShimmer Spread=""0.5"" Text=""A narrow shimmer band"" />";

    private readonly string example4RazorCode = @"
<BitTextShimmer BaseColor=""#3f3f46"" GradientColor=""#22d3ee"" Text=""An ocean colored shimmer"" />

<BitTextShimmer BaseColor=""#92400e"" GradientColor=""#fbbf24"" Text=""A golden colored shimmer"" />";

    private readonly string example5RazorCode = @"
<BitTextShimmer ContentLength=""30"">
    Thinking <strong>really</strong> hard about it...
</BitTextShimmer>";

    private readonly string example6RazorCode = @"
<style>
    .custom-class {
        font-size: 1.5rem;
        font-style: italic;
    }
</style>

<BitTextShimmer Style=""font-size:2rem;font-weight:bold"" Text=""A styled text shimmer"" />

<BitTextShimmer Class=""custom-class"" Text=""A classy text shimmer"" />";

    private readonly string example7RazorCode = @"
<div dir=""rtl"">
    <BitTextShimmer Dir=""BitDir.Rtl"" Text=""در حال فکر کردن به سوال شما..."" />
</div>";
}
