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
            Description = "The animation the shimmer plays while it stands in for content that has not arrived yet. Duration and Delay retune whichever animation is chosen, and None leaves a static block that neither of them applies to.",
            LinkType = LinkType.Link,
            Href = "#animation-enum"
        },
        new()
        {
            Name = "Background",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The background color of the container of the shimmer, which is the resting color of the placeholder the animation plays over - and the whole of what a placeholder with no animation is painted in.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content that will be shown when the Loaded parameter changes to true."
        },
        new()
        {
            Name = "Circle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the shimmer as circle instead of a rectangle. This is the short spelling of Shape=\"BitShimmerShape.Circle\", which wins over it when both are set."
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
            Description = "The color of the animated part of the shimmer, over the resting Background of the placeholder. A placeholder with no animation has no animated part, so it no longer applies there.",
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
            Description = "The animation delay value in ms, which is the pause before each loop of the animation and not the wait before the placeholder itself appears (that one is ShowDelay).",
        },
        new()
        {
            Name = "Duration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The animation duration value in ms: one full sweep of the wave, or one full breath of the pulse and the fade.",
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "The gap between the lines of a multi-line shimmer, as a CSS length. Only applies while Lines is greater than 1, and defaults to the rhythm unit of the theme.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The shimmer height value. It sizes the placeholder rather than the component, so once Loaded turns true the content decides its own height. With more than one line it is the height of each single line. Left unset, the height comes from Size."
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the shimmer out in the flow of a line of text instead of as a block of its own, taking the width given by Width and falling back to the minimum control width of the theme. A Height of 1em keeps it exactly as tall as the type it sits in."
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text announced by assistive technologies while the shimmer is standing in for content. It is carried by a live region that swaps to LoadedLabel once the content arrives, and it is that swap which gets announced."
        },
        new()
        {
            Name = "LastLineWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the last line of a multi-line shimmer, as a CSS length. Only applies while Lines is greater than 1, and defaults to 60% so a stack of bars reads as a paragraph."
        },
        new()
        {
            Name = "Lines",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of placeholder lines rendered as a stack, which is what a paragraph of text reads as. A circle is a single shape rather than a stack, so it ignores this."
        },
        new()
        {
            Name = "LineWidths",
            Type = "IList<string>?",
            DefaultValue = "null",
            Description = "The width of each line of a multi-line shimmer, as a list of CSS lengths. Only applies while Lines is greater than 1, and it is a prefix rather than a replacement: a line the list does not reach keeps the width it would have had anyway, which is the full measure or the shortened LastLineWidth."
        },
        new()
        {
            Name = "Loaded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Controls when the shimmer is swapped with actual data through an animated transition. The placeholder and the content are never on the page at the same time, and the sizing of the placeholder is dropped with it."
        },
        new()
        {
            Name = "LoadedLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text announced by assistive technologies once the content has replaced the shimmer."
        },
        new()
        {
            Name = "MinShowTime",
            Type = "int?",
            DefaultValue = "null",
            Description = "The shortest time in ms a placeholder that has been seen stays on the page. ShowDelay keeps a fast response from ever showing a placeholder; this keeps a response landing just after one has appeared from taking it away in the same breath, which reads as a flicker rather than as loading. It is measured from the moment the placeholder appears, and nothing is held back for a placeholder that was never shown."
        },
        new()
        {
            Name = "Overlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the placeholder over the content instead of in place of it, so the box keeps the size of the thing it is waiting on and the page never reflows as the placeholder is swapped out. The cover is one box over the whole content, so Lines and Template no longer apply and the size comes from the content rather than from Height."
        },
        new()
        {
            Name = "Politeness",
            Type = "BitPoliteness",
            DefaultValue = "BitPoliteness.Polite",
            Description = "How urgently the live region of the shimmer interrupts a screen reader. Only applies while Label or LoadedLabel is set.",
            LinkType = LinkType.Link,
            Href = "#politeness-enum"
        },
        new()
        {
            Name = "Pulse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Changes the animation type of the shimmer to pulse. This is the short spelling of Animation=\"BitShimmerAnimation.Pulse\", which wins over it when both are set.",
        },
        new()
        {
            Name = "Radius",
            Type = "string?",
            DefaultValue = "null",
            Description = "The corner radius of the placeholder, as a CSS length. Shape already carries the three radii a placeholder usually wants; this is for the corner that has to match a surface of its own, and it wins over the shape wherever both are set. A circle is round by construction, so it ignores this."
        },
        new()
        {
            Name = "Shape",
            Type = "BitShimmerShape?",
            DefaultValue = "null",
            Description = "The shape of the placeholder the shimmer draws: a circle for an avatar, a pill for a button or a tag, a square for an image that meets its container edge to edge.",
            LinkType = LinkType.Link,
            Href = "#shape-enum"
        },
        new()
        {
            Name = "ShowDelay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The wait in ms before the placeholder appears, so a fast response never flashes a placeholder. The wait is held in CSS rather than in a timer, so it costs no render and works under static server-side rendering."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the shimmer, which is the height of a line and the diameter of a circle. An explicit Height or Width always wins over it.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
        },
        new()
        {
            Name = "Stagger",
            Type = "int?",
            DefaultValue = "null",
            Description = "The offset in ms between the animation of one line of a multi-line shimmer and the next, added to Delay rather than replacing it: line n starts at Delay + n * Stagger. Only applies while Lines is greater than 1."
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
            Description = "The custom template to replace the default shimmer container and animation. It replaces the placeholder itself, so Shape, Lines, Animation and the sizing parameters no longer apply, while ShowDelay still holds the whole skeleton back as one."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The shimmer width value. Unlike Height it stays with the component after the swap, so a placeholder and the content that replaces it occupy the same column."
        }
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
                   Description = "Custom CSS classes/styles for the live region of the BitShimmer that carries its Label and LoadedLabel."
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
                    Description="A rectangle with the small corner radius of the theme, which is what a line of text or a block of content reads as.",
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
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the default height of a line and the default diameter of a circle.",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size shimmer.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size shimmer.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size shimmer.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "politeness-enum",
            Name = "BitPoliteness",
            Description = "How urgently a live region interrupts a screen reader, which is what the aria-live attribute carries.",
            Items =
            [
                new()
                {
                    Name= "Off",
                    Description="The region is not a live region: nothing in it is announced as it changes.",
                    Value="0",
                },
                new()
                {
                    Name= "Polite",
                    Description="The change waits its turn and is announced once the screen reader has finished what it was saying.",
                    Value="1",
                },
                new()
                {
                    Name= "Assertive",
                    Description="The change interrupts the screen reader and is announced right away.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
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
