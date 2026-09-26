namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Swiper;

public partial class BitSwiperDemo
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
            Description = "Sets the duration of the scrolling animation in seconds (the default value is 0.5). A value of 0 moves the items at once."
        },
        new()
        {
            Name = "AutoPlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables/disables the auto scrolling of the items."
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
            Description = "Plays the auto scrolling backwards, from the last item towards the first one."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items of the swiper."
        },
        new()
        {
            Name = "Classes",
            Type = "BitSwiperClassStyles?",
            DefaultValue = "null",
            Description = "The custom CSS classes for the different parts of the swiper.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the swiper, applied to the dot of the current page, the next/prev and play/pause buttons and the focus indicators.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultItem",
            Type = "int",
            DefaultValue = "1",
            Description = "The item (1 based, like GoTo) the swiper starts on when it first renders. Values outside of the range of the swiper are clamped to its first or last item."
        },
        new()
        {
            Name = "DotAriaLabel",
            Type = "string",
            DefaultValue = "Slide",
            Description = "The accessible label of a dot of the swiper, followed by the number of the page it navigates to."
        },
        new()
        {
            Name = "DotsAriaLabel",
            Type = "string",
            DefaultValue = "Choose slide to display",
            Description = "The accessible label of the dots container of the swiper."
        },
        new()
        {
            Name = "DotTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "The custom content of a dot of the swiper, receiving the zero based index of the page the dot navigates to."
        },
        new()
        {
            Name = "DragThreshold",
            Type = "int",
            DefaultValue = "5",
            Description = "The distance (in pixels) the pointer has to travel over the swiper before it starts dragging it, instead of the press staying a click."
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "The space between the items of the swiper (any CSS length, for example 1rem), which VisibleItemsCount takes into account."
        },
        new()
        {
            Name = "HideNextPrev",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the Next/Prev buttons of the BitSwiper. Each button also hides itself at the end it cannot move any further towards."
        },
        new()
        {
            Name = "ItemAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of an item of the swiper, as a composite format string whose {0} is the 1 based position of the item and whose {1} is the number of items (\"{0} of {1}\" by default). Used for items without their own AriaLabel, and announced when the swiper moves to the item."
        },
        new()
        {
            Name = "NextAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of the next button of the swiper (the default value is \"Next slide\")."
        },
        new()
        {
            Name = "NextIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display in the next navigation button using custom CSS classes for external icon libraries. Takes precedence over NextIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NextIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display in the next navigation button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "NoDrag",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables dragging the swiper with the mouse. A touch swipe is the browser's own scrolling of the region and is not taken away."
        },
        new()
        {
            Name = "NoKeyboard",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the swiper from the tab sequence and turns off its keyboard navigation."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<int>",
            DefaultValue = "",
            Description = "The event that will be called with the zero based index of the item the swiper came to stand on, however it was moved."
        },
        new()
        {
            Name = "OnReachEnd",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event that will be called each time the swiper arrives at its end (never for a swiper everything fits in), which is where more items are loaded."
        },
        new()
        {
            Name = "OnReachStart",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event that will be called each time the swiper arrives back at its start (not for the start it is first laid out on)."
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
            Description = "Gets or sets the icon of the play/pause button while the auto scrolling is running, using custom CSS classes for external icon libraries.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PauseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon of the play/pause button while the auto scrolling is running, from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "PauseOnFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Pauses the auto scrolling while the keyboard focus is inside the swiper."
        },
        new()
        {
            Name = "PauseOnHover",
            Type = "bool",
            DefaultValue = "true",
            Description = "Pauses the auto scrolling while the pointer is over the swiper."
        },
        new()
        {
            Name = "Peek",
            Type = "string?",
            DefaultValue = "null",
            Description = "The room (any CSS length, for example 2rem) kept at both ends of the swiper, which the neighboring items peek into. VisibleItemsCount fits its items between the two, and the items settle against it."
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
            Description = "Gets or sets the icon of the play/pause button while the auto scrolling is paused, using custom CSS classes for external icon libraries.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PlayIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon of the play/pause button while the auto scrolling is paused, from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "PrevAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of the previous button of the swiper (the default value is \"Previous slide\")."
        },
        new()
        {
            Name = "PrevIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display in the previous navigation button using custom CSS classes for external icon libraries. Takes precedence over PrevIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PrevIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display in the previous navigation button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "Rewind",
            Type = "bool",
            DefaultValue = "false",
            Description = "Wraps the manual navigation around: moving on from the end goes back to the start and the other way around. It covers the next/prev buttons (which then stay visible at both ends), the arrow keys and GoNext/GoPrev; the wheel and dragging still stop at the ends."
        },
        new()
        {
            Name = "ScrollItemsCount",
            Type = "int",
            DefaultValue = "1",
            Description = "Number of items that is going to be changed on navigation, which is the step of the buttons, the arrow keys and the mouse wheel."
        },
        new()
        {
            Name = "ShowDots",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the navigation dots below the items of the swiper, one per screenful of it."
        },
        new()
        {
            Name = "ShowPlayPause",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a play/pause button next to the dots, so the auto scrolling can be stopped and started again."
        },
        new()
        {
            Name = "ShowScrollbar",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the scrollbar of the swiper visible, which is hidden by default."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the dots and of the next/prev buttons of the swiper.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Snap",
            Type = "BitSwiperSnap?",
            DefaultValue = "null",
            Description = "Settles the swiper on an item instead of leaving it wherever the scrolling ran out, and chooses where that item comes to rest.",
            LinkType = LinkType.Link,
            Href = "#snap-enum",
        },
        new()
        {
            Name = "StopOnInteraction",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the auto scrolling as soon as the swiper is navigated by hand."
        },
        new()
        {
            Name = "StopOnLastSlide",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the auto scrolling at the end of the swiper instead of rewinding to its start."
        },
        new()
        {
            Name = "Styles",
            Type = "BitSwiperClassStyles?",
            DefaultValue = "null",
            Description = "The custom CSS styles for the different parts of the swiper.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stacks the items vertically, so the swiper scrolls up and down instead of left and right."
        },
        new()
        {
            Name = "VisibleItemsCount",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of items that is visible in the swiper, which sizes the items accordingly. Without it the items keep whatever size they were given."
        },
        new()
        {
            Name = "VisibleItemsCountXs",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of visible items in the extra small breakpoint (from 0 up)."
        },
        new()
        {
            Name = "VisibleItemsCountSm",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of visible items in the small breakpoint (from 600px up)."
        },
        new()
        {
            Name = "VisibleItemsCountMd",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of visible items in the medium breakpoint (from 960px up)."
        },
        new()
        {
            Name = "VisibleItemsCountLg",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of visible items in the large breakpoint (from 1280px up)."
        },
        new()
        {
            Name = "VisibleItemsCountXl",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of visible items in the extra large breakpoint (from 1920px up)."
        },
        new()
        {
            Name = "VisibleItemsCountXxl",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of visible items in the extra extra large breakpoint (from 2560px up)."
        },
        new()
        {
            Name = "Wheel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Navigates the swiper with the wheel of the mouse (or with a two finger scroll on a trackpad)."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitSwiperClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitSwiper." },
                new() { Name = "Container", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the scrolling container of the BitSwiper." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the items of the BitSwiper." },
                new() { Name = "CurrentItem", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the item the swiper is currently standing on." },
                new() { Name = "Buttons", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the next/prev buttons of the BitSwiper." },
                new() { Name = "ButtonIcons", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icons of the next/prev buttons of the BitSwiper." },
                new() { Name = "NextButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the next button of the BitSwiper." },
                new() { Name = "NextButtonIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the next button of the BitSwiper." },
                new() { Name = "PrevButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the previous button of the BitSwiper." },
                new() { Name = "PrevButtonIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the previous button of the BitSwiper." },
                new() { Name = "DotsContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the dots container of the BitSwiper." },
                new() { Name = "Dots", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the dot elements of the BitSwiper." },
                new() { Name = "CurrentDot", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the current dot element of the BitSwiper." },
                new() { Name = "PlayPauseButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the play/pause button of the BitSwiper." },
                new() { Name = "PlayPauseButtonIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the play/pause button of the BitSwiper." },
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
            Id = "snap-enum",
            Name = "BitSwiperSnap",
            Description = "The place an item of a BitSwiper comes to rest at when the swiper snaps.",
            Items =
            [
                new() { Name = "Start", Description = "The item settles with its leading edge at the start of the swiper.", Value = "0" },
                new() { Name = "Center", Description = "The item settles in the middle of the swiper.", Value = "1" },
                new() { Name = "End", Description = "The item settles with its trailing edge at the end of the swiper.", Value = "2" },
            ]
        },
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
            Name = "CurrentIndex",
            Type = "int",
            Description = "The zero based index of the item the swiper is currently standing on.",
        },
        new()
        {
            Name = "CurrentPage",
            Type = "int",
            Description = "The zero based index of the screenful (page) the swiper is currently showing.",
        },
        new()
        {
            Name = "IsAtStart",
            Type = "bool",
            Description = "Whether the swiper is scrolled all the way to its start.",
        },
        new()
        {
            Name = "IsAtEnd",
            Type = "bool",
            Description = "Whether the swiper is scrolled all the way to its end.",
        },
        new()
        {
            Name = "IsPaused",
            Type = "bool",
            Description = "Whether the auto scrolling has been paused through Pause or the play/pause button.",
        },
        new()
        {
            Name = "IsPlaying",
            Type = "bool",
            Description = "Whether the auto scrolling is currently running.",
        },
        new()
        {
            Name = "ItemsCount",
            Type = "int",
            Description = "The number of items of the swiper.",
        },
        new()
        {
            Name = "PagesCount",
            Type = "int",
            Description = "The number of screenfuls (pages) the items of the swiper take up.",
        },
        new()
        {
            Name = "GoNext",
            Type = "Task",
            Description = "Navigates to the next swiper item.",
        },
        new()
        {
            Name = "GoPrev",
            Type = "Task",
            Description = "Navigates to the previous swiper item.",
        },
        new()
        {
            Name = "GoTo",
            Type = "Task",
            Description = "Navigates to the given swiper item number (1 based).",
        },
        new()
        {
            Name = "GoToPage",
            Type = "Task",
            Description = "Navigates to the given swiper page number (1 based), a page being one screenful of the swiper.",
        },
        new()
        {
            Name = "GoToStart",
            Type = "Task",
            Description = "Navigates to the start of the swiper.",
        },
        new()
        {
            Name = "GoToEnd",
            Type = "Task",
            Description = "Navigates to the end of the swiper.",
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
            Description = "Measures the swiper again and reports where it stands.",
        }
    ];



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-Swiper-gap", DefaultValue = "0", Description = "Space between the items. The Gap parameter wins over it, and VisibleItemsCount takes it into account." },
        new() { Name = "--bit-Swiper-peek", DefaultValue = "0", Description = "Room at both ends of the swiper the neighboring items peek into. The Peek parameter wins over it." },
        new() { Name = "--bit-Swiper-vertical-height", DefaultValue = "25 spacing units", Description = "Height of a vertical swiper that was not given one through Style or a class." },
        new() { Name = "--bit-Swiper-focus-color", DefaultValue = "The primary focus color / the Color's focus color", Description = "Color of every keyboard focus indicator: the root, the buttons and the dots." },
        new() { Name = "--bit-Swiper-button-color", DefaultValue = "The primary foreground / the Color's main color", Description = "Glyph color of the next/prev and play/pause buttons, and the text of templated dots." },
        new() { Name = "--bit-Swiper-button-hover-color", DefaultValue = "The primary foreground hover / the Color's hover color", Description = "Glyph color of those buttons on hover." },
        new() { Name = "--bit-Swiper-button-background", DefaultValue = "transparent", Description = "Background of the next/prev strips, for buttons that have to stand out over busy items." },
        new() { Name = "--bit-Swiper-button-hover-background", DefaultValue = "The rest background", Description = "Background of the next/prev strips on hover." },
        new() { Name = "--bit-Swiper-button-opacity", DefaultValue = "0.7", Description = "Opacity of the buttons at rest; hover and focus bring them to 1. Keep it high enough for a 3:1 contrast." },
        new() { Name = "--bit-Swiper-button-width", DefaultValue = "10%", Description = "Width of the next/prev strips (their height on a vertical swiper), never below the 24px pointer target." },
        new() { Name = "--bit-Swiper-button-size", DefaultValue = "Per Size (3 spacing units at Medium)", Description = "Glyph size of the next/prev buttons." },
        new() { Name = "--bit-Swiper-dot-size", DefaultValue = "Per Size (1.25 spacing units at Medium)", Description = "Diameter of a dot. Its hit area never drops below the 24px WCAG target." },
        new() { Name = "--bit-Swiper-dot-current-width", DefaultValue = "The dot size", Description = "Width of the current dot; a larger value turns it into a pill." },
        new() { Name = "--bit-Swiper-dot-radius", DefaultValue = "The full radius", Description = "Corner radius of a dot." },
        new() { Name = "--bit-Swiper-dot-gap", DefaultValue = "What keeps a 24px target per dot", Description = "Space between the dots. A smaller value lets their hit areas overlap." },
        new() { Name = "--bit-Swiper-dot-color", DefaultValue = "The primary border color", Description = "Fill of a dot, which keeps a 3:1 contrast against the background by default." },
        new() { Name = "--bit-Swiper-dot-hover-color", DefaultValue = "The primary border hover color", Description = "Fill of a dot on hover." },
        new() { Name = "--bit-Swiper-dot-current-color", DefaultValue = "The Accent / the Color's main color", Description = "Fill of the current dot (text color of a templated one)." },
        new() { Name = "--bit-Swiper-dot-current-hover-color", DefaultValue = "The Accent / the Color's hover color", Description = "Fill of the current dot on hover." },
        new() { Name = "--bit-Swiper-dots-margin", DefaultValue = "1.25 spacing units", Description = "Space between the items and the row of dots." },
    ];



    private readonly BitSwiperParams[] swiperParams =
    [
        new()
        {
            ShowDots = true,
            Rewind = true,
            Gap = "0.5rem",
            VisibleItemsCount = 4,
            Snap = BitSwiperSnap.Start,
            ScrollItemsCount = 2,
        }
    ];

    private BitSwiperSnap snap = BitSwiperSnap.Center;
    private int number = 1;
    private int loadedCount = 8;
    private int currentIndex;
    private BitSwiper swiper = default!;

    private async Task GoNext() => await swiper.GoNext();

    private async Task GoPrev() => await swiper.GoPrev();

    private async Task GoTo() => await swiper.GoTo(number);

    private async Task GoToStart() => await swiper.GoToStart();

    private async Task GoToEnd() => await swiper.GoToEnd();

    private async Task LoadMore()
    {
        if (loadedCount >= 40) return;

        await Task.Delay(300); // fetching the next page of items

        loadedCount += 8;
    }
}
