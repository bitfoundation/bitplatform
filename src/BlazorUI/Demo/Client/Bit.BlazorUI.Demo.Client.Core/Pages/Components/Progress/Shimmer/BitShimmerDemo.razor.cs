namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Shimmer;

public partial class BitShimmerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Animation",
            Type = "BitShimmerAnimation?",
            DefaultValue = "null",
            Description = "The animation the shimmer plays while it waits: Wave, Pulse, Fade or None.",
            LinkType = LinkType.Link,
            Href = "#animation-enum"
        },
        new()
        {
            Name = "Background",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The resting color of the placeholder, which the animation plays over. It is all a None placeholder shows.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content that replaces the shimmer once Loaded is true."
        },
        new()
        {
            Name = "Circle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the shimmer as a circle. Short form of Shape=\"BitShimmerShape.Circle\", which wins over it."
        },
        new()
        {
            Name = "Classes",
            Type = "BitShimmerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitShimmer.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the animated part of the shimmer. A None placeholder has no animated part.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent."
        },
        new()
        {
            Name = "Delay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The pause in ms before the first loop of the animation. Not the wait before the placeholder appears (see ShowDelay).",
        },
        new()
        {
            Name = "Duration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The length in ms of one loop of the animation.",
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "The gap between the lines of a multi-line shimmer, as a CSS length.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the placeholder, or of each line of a multi-line one. Dropped once loaded, so the content sizes itself. Defaults to the Size."
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the shimmer out in a line of text, rendered as a span. Without a Width it takes the theme's minimum control width."
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Screen-reader text announced while the shimmer waits, in a live region that switches to LoadedLabel when the content arrives. The region is rendered right after the root, so a sibling selector sees it too, and it follows the root's Visibility, hidden, inert, aria-hidden, Dir and lang but not a stylesheet that hides it."
        },
        new()
        {
            Name = "LastLineWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the last line of a multi-line shimmer, as a CSS length. Defaults to 60%."
        },
        new()
        {
            Name = "Lines",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of lines stacked as a paragraph. A circle and an overlay ignore it."
        },
        new()
        {
            Name = "LineWidths",
            Type = "IList<string>?",
            DefaultValue = "null",
            Description = "The width of each line of a multi-line shimmer, from the first line on. Lines past the end of the list keep their default width."
        },
        new()
        {
            Name = "Loaded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Swaps the placeholder for the content, which fades in if a placeholder was seen."
        },
        new()
        {
            Name = "LoadedLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Screen-reader text announced once the content has replaced the shimmer."
        },
        new()
        {
            Name = "MinShowTime",
            Type = "int?",
            DefaultValue = "null",
            Description = "The shortest time in ms a placeholder that has appeared stays on the page, so a response landing just after it never flickers."
        },
        new()
        {
            Name = "Overlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the placeholder over the content instead of in place of it, so the layout never moves and the content keeps its state. Lines and Template do not apply. Covered content leaves the tab order, so keep the control that starts a refresh outside it."
        },
        new()
        {
            Name = "Politeness",
            Type = "BitPoliteness",
            DefaultValue = "BitPoliteness.Polite",
            Description = "How urgently the live region interrupts a screen reader. Only applies with Label or LoadedLabel.",
            LinkType = LinkType.Link,
            Href = "#politeness-enum"
        },
        new()
        {
            Name = "Pulse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Changes the animation to pulse. Short form of Animation=\"BitShimmerAnimation.Pulse\", which wins over it.",
        },
        new()
        {
            Name = "Radius",
            Type = "string?",
            DefaultValue = "null",
            Description = "The corner radius of the placeholder, as a CSS length. Wins over the Shape; a circle ignores it."
        },
        new()
        {
            Name = "Shape",
            Type = "BitShimmerShape?",
            DefaultValue = "null",
            Description = "The shape of the placeholder: Rounded, Square, Pill or Circle.",
            LinkType = LinkType.Link,
            Href = "#shape-enum"
        },
        new()
        {
            Name = "ShowDelay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The wait in ms before the placeholder appears, so a fast response never flashes one. Pure CSS, so it also works under static SSR."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The default line height and circle diameter. An explicit Height or Width wins over it.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
        },
        new()
        {
            Name = "Stagger",
            Type = "int?",
            DefaultValue = "null",
            Description = "The offset in ms between the animations of consecutive lines: line n starts at Delay + n * Stagger."
        },
        new()
        {
            Name = "Styles",
            Type = "BitShimmerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitShimmer.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom skeleton built from shimmers of its own, replacing the default placeholder. ShowDelay still holds it back as one."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the shimmer. Unlike Height it stays after the swap, so the placeholder and the content share a column."
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Shimmer-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Resting color of the placeholder, which the animation plays over. The Background parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-color",
            DefaultValue = "--bit-clr-bg-ter",
            Description = "Color of the animated part: the wave band, or the block the pulse and the fade play on. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-height",
            DefaultValue = "spacing(4)",
            Description = "Height of a line. The Height and Size parameters win over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-circle-size",
            DefaultValue = "--bit-siz-ctrl-md",
            Description = "Diameter of a circle. The Height and Size parameters win over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-radius",
            DefaultValue = "--bit-shp-radius-surface",
            Description = "Corner of a Rounded placeholder. The Radius parameter and the Square and Pill shapes win over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-gap",
            DefaultValue = "spacing(1)",
            Description = "Room between the lines of a stack. The Gap parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-last-line-width",
            DefaultValue = "60%",
            Description = "Width of the last line of a stack. The LastLineWidth and LineWidths parameters win over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-animation-duration",
            DefaultValue = "1.6s x --bit-mot-loop-factor",
            Description = "One loop of the wave, the pulse or the fade. The Duration parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Shimmer-animation-delay",
            DefaultValue = "0.5s x --bit-mot-loop-factor",
            Description = "Pause before the first loop. The Delay and Stagger parameters win over it.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitShimmerClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitShimmer."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitShimmer. The same box holds the content an Overlay covers."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the live region of the BitShimmer that carries its Label and LoadedLabel. It is rendered right after the root, not inside it, so a shimmer hidden by a stylesheet hides the region through these as well."
               },
               new()
               {
                   Name = "ShimmerWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the shimmer wrapper of the BitShimmer. A multi-line shimmer draws one wrapper per line, so these are applied to each of them."
               },
               new()
               {
                   Name = "Shimmer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the shimmer of the BitShimmer, which is the animated part inside each wrapper and is not drawn at all when the animation is None."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "animation-enum",
            Name = "BitShimmerAnimation",
            Description = "Determines the animation the BitShimmer plays while it stands in for content that has not arrived yet.",
            Items =
            [
                new()
                {
                    Name= "Wave",
                    Description="A highlight band sweeps across the placeholder from one side to the other, reversing with the direction of the page.",
                    Value="0",
                },
                new()
                {
                    Name= "Pulse",
                    Description="The placeholder breathes between full and reduced opacity, which is cheaper to paint than the wave and calmer on a page full of placeholders.",
                    Value="1",
                },
                new()
                {
                    Name= "Fade",
                    Description="The placeholder fades all the way out and back in, a heavier version of the pulse for a single placeholder that has to be noticed.",
                    Value="2",
                },
                new()
                {
                    Name= "None",
                    Description="No animation at all: the placeholder is a static block of its Background, with no animated part left for Color to paint.",
                    Value="3",
                }
            ]
        },
        new()
        {
            Id = "shape-enum",
            Name = "BitShimmerShape",
            Description = "Determines the shape of the placeholder the BitShimmer draws.",
            Items =
            [
                new()
                {
                    Name= "Rounded",
                    Description="A rectangle with the surface corner radius of the theme, which is what a line of text or a block of content reads as.",
                    Value="0",
                },
                new()
                {
                    Name= "Square",
                    Description="A rectangle with no corner radius at all, for content that meets its container edge to edge.",
                    Value="1",
                },
                new()
                {
                    Name= "Pill",
                    Description="A rectangle with fully rounded ends, which is what a button, a tag or a chip reads as.",
                    Value="2",
                },
                new()
                {
                    Name= "Circle",
                    Description="A circle, which is what an avatar or a round icon reads as. It takes its diameter from whichever of the height and the width is set, and ignores Lines.",
                    Value="3",
                }
            ]
        },
        DemoSharedEnums.BitSize(description: "Determines the default height of a line and the default diameter of a circle."),
        DemoSharedEnums.BitPoliteness(),
        DemoSharedEnums.BitColor(),
    ];



    private readonly BitShimmerParams[] shimmerParams =
    [
        new()
        {
            Animation = BitShimmerAnimation.Pulse,
            Height = "0.75rem",
            LastLineWidth = "40%",
            Stagger = 150,
        }
    ];

    private bool isDataLoaded;

    private bool isContentLoaded;

    private bool isOverlayLoaded;

    private bool isAccessibleLoaded;

    private bool isDelayLoaded = true;

    // Each click restarts the wait, so the delay of the click before it must not be allowed to land and
    // report the component as loaded while the newer one is still running.
    private CancellationTokenSource? delayCts;

    private async Task SimulateLoading(int duration)
    {
        delayCts?.Cancel();
        delayCts?.Dispose();
        var cts = delayCts = new CancellationTokenSource();

        isDelayLoaded = false;
        StateHasChanged();

        try
        {
            await Task.Delay(duration, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // A delay that finished in the moment before a newer click cancelled it comes out of the await without
        // ever being cancelled, so the token alone does not say whether this is still the current wait.
        if (ReferenceEquals(cts, delayCts) is false) return;

        isDelayLoaded = true;
    }

}
