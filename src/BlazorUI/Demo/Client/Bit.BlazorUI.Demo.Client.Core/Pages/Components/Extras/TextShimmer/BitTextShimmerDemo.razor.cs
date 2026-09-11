namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TextShimmer;

public partial class BitTextShimmerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alternate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sweeps the band back and forth across the text instead of always in the same direction. Reversed decides which of the two directions comes first.",
        },
        new()
        {
            Name = "Angle",
            Type = "double?",
            DefaultValue = "null",
            Description = "The tilt of the band in degrees, measured from upright. A positive angle leans the top of the band towards the end of the text in its reading direction, so a right-to-left shimmer is mirrored. Keep it within about 45 degrees either way; a value that is not a finite number is ignored.",
        },
        new()
        {
            Name = "BaseColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The resting/dim color of the text. When null, a theme-aware default color is used. It is the color the text is read in most of the time, so it is the one to check for contrast.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to shimmer, which takes precedence over the Text parameter. Its length cannot be measured, so the band is scaled by ContentLength (or sized by SpreadLength) instead. A part that should keep its own colors (an emoji), or that is transformed (a spinning icon), needs a fill of its own.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the band that sweeps across the text, read from the theme. An explicit GradientColor wins over it.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "ContentLength",
            Type = "int",
            DefaultValue = "10",
            Description = "The character count used to scale the shimmer band width when the content is supplied using ChildContent (or when neither ChildContent nor Text is set).",
        },
        new()
        {
            Name = "Delay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The delay before the first shimmer sweep starts in ms. The text rests in its base color until then. A negative value is treated as zero.",
        },
        new()
        {
            Name = "Duration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The animation duration of one full shimmer sweep in ms - the band crossing the text and the rest before it enters again. When null, a two-second sweep is used. A negative value is treated as zero.",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. The default is \"p\"; a value that is not a name a tag can have falls back to it. A void element (such as \"br\" or \"img\") holds no content, so neither Text nor ChildContent is rendered into it.",
        },
        new()
        {
            Name = "GradientColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The bright highlight color that sweeps across the text. When null, a theme-aware default color is used. It wins over Color.",
        },
        new()
        {
            Name = "Iterations",
            Type = "int?",
            DefaultValue = "null",
            Description = "The number of shimmer sweeps to play before the text comes to rest. When null (or below one), the shimmer sweeps forever.",
        },
        new()
        {
            Name = "Paused",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the shimmer where it is instead of sweeping. The band stops wherever it had reached and carries on from there once this is turned off again.",
        },
        new()
        {
            Name = "PauseOnHover",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the shimmer where it is while the pointer is over it or the focus is inside it.",
        },
        new()
        {
            Name = "RepeatDelay",
            Type = "int?",
            DefaultValue = "null",
            Description = "An extra pause between two shimmer sweeps in ms, which adds to the rest between them without changing the speed of the band. Without a Duration it is retuned by the theme along with the sweep. A negative value is treated as zero.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sweeps the band against the reading direction. The band follows the reading direction by default, whether it comes from the Dir parameter or from the page around the shimmer.",
        },
        new()
        {
            Name = "Spread",
            Type = "double",
            DefaultValue = "2",
            Description = "The shimmer band width multiplier. The effective spread of the band (px) - from its brightest point to each of its edges - is Spread times the character count, so longer text gets a proportionally wider shine. SpreadLength wins over it.",
        },
        new()
        {
            Name = "SpreadLength",
            Type = "string?",
            DefaultValue = "null",
            Description = "An explicit CSS length for the spread of the band, which replaces the one computed from Spread and the character count. A font-relative length (em, ch) follows the size of the text without counting its characters.",
        },
        new()
        {
            Name = "Static",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the text at rest in its base color, without the shimmer. Unlike Paused, it takes the band away altogether; turning it off starts the sweeps from the beginning.",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to display, that is also used to scale the shimmer band width based on its character count.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" },
            ]
        },
    ];



    private int replayCount;

    private bool isPaused;

    private bool isStatic;

    private bool isThinking;
    private string thinkingResult = "Ask a question to see the assistant think.";

    private async Task AskAsync()
    {
        isThinking = true;
        StateHasChanged();

        await Task.Delay(4000);

        thinkingResult = "Here is your answer: a text shimmer is a gradient band clipped to the glyphs of the text.";
        isThinking = false;
    }
}
