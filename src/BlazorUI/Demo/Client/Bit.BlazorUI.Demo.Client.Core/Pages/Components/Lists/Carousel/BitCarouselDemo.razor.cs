namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Carousel;

public partial class BitCarouselDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "Specifies the accent color kind of the component, which colors the dot of the current page. Color takes precedence over it when both are set.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "AnimationDuration",
            Type = "double",
            DefaultValue = "0.5",
            Description = "Sets the duration of the scrolling animation in seconds (the default value is 0.5)."
        },
        new()
        {
            Name = "AutoPlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables/disables the auto scrolling of the slides."
        },
        new()
        {
            Name = "AutoPlayInterval",
            Type = "double",
            DefaultValue = "2000",
            Description = "Sets the interval of the auto scrolling in milliseconds (the default value is 2000)."
        },
        new()
        {
            Name = "AutoPlayReverse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Plays the auto scrolling backwards, from the last slide towards the first one."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items of the carousel."
        },
        new()
        {
            Name = "Classes",
            Type = "BitCarouselClassStyles?",
            DefaultValue = "null",
            Description = "The custom CSS classes for the different parts of the carousel.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the carousel, applied to the dot of the current page and the next/prev and play/pause buttons.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultPage",
            Type = "int",
            DefaultValue = "1",
            Description = "The page (1 based, like GoTo) the carousel shows when it first renders."
        },
        new()
        {
            Name = "DotAriaLabel",
            Type = "string",
            DefaultValue = "Slide",
            Description = "The accessible label of a dot of the carousel, followed by the number of the page it navigates to."
        },
        new()
        {
            Name = "DotsAriaLabel",
            Type = "string",
            DefaultValue = "Choose slide to display",
            Description = "The accessible label of the dots container of the carousel."
        },
        new()
        {
            Name = "DragThreshold",
            Type = "int",
            DefaultValue = "20",
            Description = "The distance (in pixels) the pointer has to travel over the carousel before it moves to another page."
        },
        new()
        {
            Name = "Fade",
            Type = "bool",
            DefaultValue = "false",
            Description = "Cross-fades the slides in place instead of sliding them. A fading carousel shows exactly one slide at a time."
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "The space between the slides of the carousel (any CSS length, for example 1rem)."
        },
        new()
        {
            Name = "GoLeftAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of the go to left button. When not set, the button is labelled after what it does (next or previous slide, depending on the direction)."
        },
        new()
        {
            Name = "GoLeftIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the go to left button using custom CSS classes for external icon libraries. Takes precedence over GoLeftIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "GoLeftIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon for the go to left button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "GoRightAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of the go to right button. When not set, the button is labelled after what it does (previous or next slide, depending on the direction)."
        },
        new()
        {
            Name = "GoRightIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the go to right button using custom CSS classes for external icon libraries. Takes precedence over GoRightIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "GoRightIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon for the go to right button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "HideDots",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the Dots indicator at the bottom of the BitCarousel. The dots are also left out when everything fits on a single page."
        },
        new()
        {
            Name = "HideNextPrev",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the Next/Prev buttons of the BitCarousel. Each button also hides itself at the end it cannot move any further towards, unless InfiniteScrolling is enabled."
        },
        new()
        {
            Name = "InfiniteScrolling",
            Type = "bool",
            DefaultValue = "false",
            Description = "If enabled the carousel items will navigate in an infinite loop (first item comes after last item and last item comes before first item)."
        },
        new()
        {
            Name = "NoDrag",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables dragging the carousel with the pointer."
        },
        new()
        {
            Name = "NoKeyboard",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the carousel from the tab sequence and turns off its keyboard navigation (arrow keys, Home and End)."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<int>",
            Description = "The event that will be called on carousel page navigation. The provided value is the zero based index of the page the carousel moved to."
        },
        new()
        {
            Name = "PauseButtonAriaLabel",
            Type = "string",
            DefaultValue = "Stop automatic slide show",
            Description = "The accessible label of the play/pause button while the auto scrolling is running."
        },
        new()
        {
            Name = "PauseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the play/pause button while the auto scrolling is running, using custom CSS classes for external icon libraries. Takes precedence over PauseIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PauseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the play/pause button while the auto scrolling is running, from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "PauseOnFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Pauses the auto scrolling while the keyboard focus is inside the carousel."
        },
        new()
        {
            Name = "PauseOnHover",
            Type = "bool",
            DefaultValue = "true",
            Description = "Pauses the auto scrolling while the pointer is over the carousel."
        },
        new()
        {
            Name = "PlayButtonAriaLabel",
            Type = "string",
            DefaultValue = "Start automatic slide show",
            Description = "The accessible label of the play/pause button while the auto scrolling is paused."
        },
        new()
        {
            Name = "PlayIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the play/pause button while the auto scrolling is paused, using custom CSS classes for external icon libraries. Takes precedence over PlayIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PlayIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the play/pause button while the auto scrolling is paused, from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "ScrollItemsCount",
            Type = "int",
            DefaultValue = "1",
            Description = "Number of items that is going to be changed on navigation. It is clamped to VisibleItemsCount."
        },
        new()
        {
            Name = "ShowPlayPause",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a play/pause button next to the dots, so the auto scrolling can be stopped and started again. Only rendered while AutoPlay is enabled."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the dots and of the next/prev buttons of the carousel.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StopOnInteraction",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the auto scrolling as soon as the carousel is navigated by hand. Once stopped this way the rotation only comes back through Resume or the play/pause button."
        },
        new()
        {
            Name = "StopOnLastSlide",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the auto scrolling on the last page instead of rewinding to the first one. It has no effect while InfiniteScrolling is enabled."
        },
        new()
        {
            Name = "Styles",
            Type = "BitCarouselClassStyles?",
            DefaultValue = "null",
            Description = "The custom CSS styles for the different parts of the carousel.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stacks the slides vertically, so the carousel scrolls up and down instead of left and right."
        },
        new()
        {
            Name = "VisibleItemsCount",
            Type = "int",
            DefaultValue = "1",
            Description = "Number of items that is visible in the carousel."
        },
        new()
        {
            Name = "Wheel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Navigates the carousel with the wheel of the mouse (or with a two finger scroll on a trackpad)."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "carousel-item",
            Title = "BitCarouselItem",
            Description = "A single slide of the BitCarousel.",
            Parameters =
            [
               new()
               {
                   Name = "ChildContent",
                   Type = "RenderFragment?",
                   DefaultValue = "null",
                   Description = "The content of the carousel item (slide)."
               },
               new()
               {
                   Name = "Index",
                   Type = "int",
                   DefaultValue = "0",
                   Description = "The zero based position of this item among the items of its carousel (read-only, assigned by the carousel)."
               },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitCarouselClassStyles",
            Description = "The custom CSS classes and styles of the different parts of the BitCarousel.",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitCarousel."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitCarousel."
               },
               new()
               {
                   Name = "Item",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the items (slides) of the BitCarousel."
               },
               new()
               {
                   Name = "Buttons",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the next/prev buttons of the BitCarousel."
               },
               new()
               {
                   Name = "ButtonIcons",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icons of the next/prev buttons of the BitCarousel."
               },
               new()
               {
                   Name = "GoLeftButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the go to left button of the BitCarousel."
               },
               new()
               {
                   Name = "GoLeftButtonIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the go to left button of the BitCarousel."
               },
               new()
               {
                   Name = "GoRightButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the go to right button of the BitCarousel."
               },
               new()
               {
                   Name = "GoRightButtonIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the go to right button of the BitCarousel."
               },
               new()
               {
                   Name = "DotsContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dots container of the BitCarousel."
               },
               new()
               {
                   Name = "Dots",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dot elements of the BitCarousel."
               },
               new()
               {
                   Name = "CurrentDot",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the current dot element of the BitCarousel."
               },
               new()
               {
                   Name = "PlayPauseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the play/pause button of the BitCarousel."
               },
               new()
               {
                   Name = "PlayPauseButtonIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the play/pause button of the BitCarousel."
               }
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "The primary color kind.", Value = "0" },
                new() { Name = "Secondary", Description = "The secondary color kind.", Value = "1" },
                new() { Name = "Tertiary", Description = "The tertiary color kind.", Value = "2" },
                new() { Name = "Transparent", Description = "The transparent color kind.", Value = "3" },
            ]
        },
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
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" },
            ]
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "CurrentPage",
            Type = "int",
            Description = "The zero based index of the page the carousel is currently showing.",
        },
        new()
        {
            Name = "ItemsCount",
            Type = "int",
            Description = "The number of items (slides) of the carousel.",
        },
        new()
        {
            Name = "PagesCount",
            Type = "int",
            Description = "The number of pages of the carousel.",
        },
        new()
        {
            Name = "IsPlaying",
            Type = "bool",
            Description = "Whether the auto scrolling is currently running.",
        },
        new()
        {
            Name = "IsPaused",
            Type = "bool",
            Description = "Whether the auto scrolling has been paused through Pause or the play/pause button.",
        },
        new()
        {
            Name = "GoNext",
            Type = "Task",
            Description = "Navigates to the next carousel item.",
        },
        new()
        {
            Name = "GoPrev",
            Type = "Task",
            Description = "Navigates to the previous carousel item.",
        },
        new()
        {
            Name = "GoTo",
            Type = "Task",
            Description = "Navigates to the given carousel page number (1 based).",
        },
        new()
        {
            Name = "Pause",
            Type = "void",
            Description = "Pauses the AutoPlay if enabled.",
        },
        new()
        {
            Name = "Resume",
            Type = "void",
            Description = "Resumes the AutoPlay if enabled.",
        },
        new()
        {
            Name = "TogglePlay",
            Type = "void",
            Description = "Pauses the AutoPlay when it is running, and resumes it when it is paused.",
        },
        new()
        {
            Name = "Refresh",
            Type = "Task",
            Description = "Measures the carousel again and lays its slides out accordingly.",
        }
    ];



    private int number = 1;
    private int currentPage;
    private BitCarousel carousel = default!;

    private async Task GoNext()
    {
        await carousel.GoNext();
    }

    private async Task GoPrev()
    {
        await carousel.GoPrev();
    }

    private async Task GoTo()
    {
        await carousel.GoTo(number);
    }
}
