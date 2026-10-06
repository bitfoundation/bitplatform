namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Loading;

public partial class BitLoadingDemo
{
    private bool isPaused;
    private bool isWorking;
    private bool isRefreshing;

    private async Task StartWork()
    {
        isWorking = true;
        await Task.Delay(1500);
        isWorking = false;
    }

    private async Task Refresh()
    {
        isRefreshing = true;
        await Task.Delay(2000);
        isRefreshing = false;
    }

    private readonly BitLoadingParams[] loadingParams =
    [
        new()
        {
            Thickness = 3,
            Speed = 1.5,
            LabelPosition = BitLabelPosition.End
        }
    ];



    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaLive",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-live politeness of the root live region. Falls back to \"polite\" for the default status role and to the role's own politeness otherwise; ignored while the loader is decorative.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitLoadingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the loading component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The theme color of the drawing.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "CustomColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "Any CSS color for the drawing, currentColor included. Only applies while Color is unset.",
        },
        new()
        {
            Name = "CustomSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of the drawing in px; the label scales along within a readable range. Only applies while Size is unset. Zero and negative values are ignored.",
        },
        new()
        {
            Name = "Delay",
            Type = "int",
            DefaultValue = "0",
            Description = "How long, in ms, the loader waits before showing anything, so quick work never flashes it. Changing the value restarts the wait.",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the loader out on the current line of text, a button or a table cell, at the size of the text (1em) unless Size or CustomSize is set.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The status text shown beside the drawing and announced by screen readers.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "The side of the drawing the label sits on: Top by default, End for an Inline loader. Start and End follow the writing direction.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom content for the label. Takes the place of Label.",
        },
        new()
        {
            Name = "Paused",
            Type = "bool",
            DefaultValue = "false",
            Description = "Freezes the animation on its current frame, keeping the layout and the live region as they are.",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA role of the root. Falls back to \"status\", a live region; \"progressbar\" makes an indeterminate progress bar named by AriaLabel, Label or \"Loading\"; \"none\" makes the loader decorative.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the loader: 40px, 64px or 88px, with the label on the matching step of the type ramp.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Speed",
            Type = "double?",
            DefaultValue = "null",
            Description = "A multiplier of the animation speed: 2 is twice as fast. Composes with reduced motion. Zero and negative values are ignored.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitLoadingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the loading component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Thickness",
            Type = "int?",
            DefaultValue = "null",
            Description = "The stroke width in px of the Ring, DualRing, Ripple, Xbox and Spinner loaders. Does not scale with the size. Zero and negative values are ignored.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitLoadingClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitLoading components."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the child container of the BitLoading components."
                },
                new()
                {
                    Name = "Child",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the child element(s) of the BitLoading components."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the BitLoading components."
                },
                new()
                {
                    Name = "ScreenReaderText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the visually hidden text a labelless BitLoading component announces to assistive technology."
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitLabelPosition("Defines where the label of a loading component sits relative to its animation."),
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size, which renders a 40px loading component.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size, which renders a 64px loading component.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size, which renders an 88px loading component.",
                    Value="2",
                }
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Loading-color",
            DefaultValue = "Color / CustomColor, or the primary color",
            Description = "Color of the drawing.",
        },
        new()
        {
            Name = "--bit-Loading-track-color",
            DefaultValue = "transparent",
            Description = "The full circle under the moving arcs of the Ring, DualRing and Xbox loaders.",
        },
        new()
        {
            Name = "--bit-Loading-size",
            DefaultValue = "Size / CustomSize, or 64px (1em when Inline)",
            Description = "Width and height of the drawing, which is laid out from it. Any CSS length, em and rem included.",
        },
        new()
        {
            Name = "--bit-Loading-thickness",
            DefaultValue = "Thickness, or the width each drawing was made with",
            Description = "Stroke width of the Ring, DualRing, Ripple, Xbox and Spinner loaders.",
        },
        new()
        {
            Name = "--bit-Loading-speed",
            DefaultValue = "Speed, or 1",
            Description = "Multiplier of the animation speed; a positive number.",
        },
        new()
        {
            Name = "--bit-Loading-gap",
            DefaultValue = "spacing(1)",
            Description = "Room between the drawing and the label.",
        },
        new()
        {
            Name = "--bit-Loading-label-color",
            DefaultValue = "The surrounding text color",
            Description = "Color of the label.",
        },
        new()
        {
            Name = "--bit-Loading-label-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of the label.",
        },
        new()
        {
            Name = "--bit-Loading-label-font-weight",
            DefaultValue = "$tg-fw-regular",
            Description = "Text weight of the label.",
        }
    ];
}
