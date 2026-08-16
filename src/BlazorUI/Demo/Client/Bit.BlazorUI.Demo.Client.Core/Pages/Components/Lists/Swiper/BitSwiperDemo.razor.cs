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
            Description = "The general color of the swiper, applied to the dot of the current page and the next/prev and play/pause buttons.",
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
            Description = "The accessible label of an item of the swiper, as a composite format string whose {0} is the 1 based position of the item and whose {1} is the number of items (\"{0} of {1}\" by default)."
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



    private int number = 1;
    private int currentIndex;
    private BitSwiper swiper = default!;

    private async Task GoNext() => await swiper.GoNext();

    private async Task GoPrev() => await swiper.GoPrev();

    private async Task GoTo() => await swiper.GoTo(number);

    private async Task GoToStart() => await swiper.GoToStart();

    private async Task GoToEnd() => await swiper.GoToEnd();



    private const string itemStyle = @"<style>
    .item {
        width: 250px;
        height: 150px;
        position: relative;
    }

    .number {
        top: 0;
        left: 0;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }
</style>";

    private const string boxStyle = @"<style>
    .box-item {
        display: flex;
        height: 5rem;
        align-items: center;
        justify-content: center;
        border-radius: 0.25rem;
        color: white;
        background-color: #0078d4;
    }
</style>";

    private const string cardStyle = @"<style>
    .card-item {
        display: flex;
        height: 6rem;
        align-items: center;
        justify-content: center;
        border-radius: 0.25rem;
        border: 1px solid #d1d1d1;
        background-color: #f3f2f1;
    }
</style>";


    private readonly string example1RazorCode = itemStyle + @"


<BitSwiper AriaLabel=""Landscape photos"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example2RazorCode = itemStyle + @"


<BitSwiper ScrollItemsCount=""2"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example3RazorCode = itemStyle + @"


<BitSwiper HideNextPrev ScrollItemsCount=""2"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example4RazorCode = boxStyle + @"


<BitSwiper Snap=""BitSwiperSnap.Start"" Gap=""0.5rem"" VisibleItemsCount=""3"">
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper Snap=""BitSwiperSnap.Center"" Gap=""0.5rem"" VisibleItemsCount=""3"">
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper Snap=""BitSwiperSnap.End"" Gap=""0.5rem"" VisibleItemsCount=""3"">
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example5RazorCode = cardStyle + @"


<BitSwiper VisibleItemsCount=""4"" ScrollItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper VisibleItemsCount=""1"" VisibleItemsCountSm=""2"" VisibleItemsCountMd=""3"" VisibleItemsCountLg=""5""
           Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example6RazorCode = boxStyle + @"


<BitSwiper Gap=""1.5rem"" VisibleItemsCount=""4"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example7RazorCode = boxStyle + @"


<BitSwiper Vertical Style=""height: 200px"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"" AriaLabel=""Vertical items"">
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example8RazorCode = boxStyle + @"


<BitSwiper ShowDots Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    <DotTemplate Context=""index""><span>@(index + 1)</span></DotTemplate>
    <ChildContent>
        @for (int i = 1; i <= 16; i++)
        {
            var index = i;
            <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
        }
    </ChildContent>
</BitSwiper>";

    private readonly string example9RazorCode = boxStyle + @"


<BitSwiper AutoPlay AutoPlayInterval=""2000"" Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper AutoPlay AutoPlayInterval=""1500"" ShowDots ShowPlayPause StopOnInteraction
           Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example10RazorCode = boxStyle + @"


<BitSwiper Wheel NoDrag Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowScrollbar HideNextPrev Gap=""0.5rem"" VisibleItemsCount=""4"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example11RazorCode = boxStyle + @"


<BitSwiper AnimationDuration=""1.5"" Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper AnimationDuration=""0"" Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example12RazorCode = boxStyle + @"


<BitSwiper DefaultItem=""7"" Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example13RazorCode = boxStyle + @"


<BitSwiper @ref=""swiper"" HideNextPrev Gap=""0.5rem"" VisibleItemsCount=""4""
           Snap=""BitSwiperSnap.Start"" OnChange=""v => currentIndex = v"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitButton OnClick=""GoPrev"">&lt; Prev</BitButton>
<BitButton OnClick=""GoNext"">Next &gt;</BitButton>

<BitButton OnClick=""GoTo"">GoTo</BitButton>
<BitNumberField @bind-Value=""number"" Min=""1"" Max=""16"" Mode=""BitSpinButtonMode.Compact"" />

<BitButton OnClick=""GoToStart"">Start</BitButton>
<BitButton OnClick=""GoToEnd"">End</BitButton>

<div>Current item: @(currentIndex + 1)</div>";
    private readonly string example13CsharpCode = @"
private int number = 1;
private int currentIndex;
private BitSwiper swiper = default!;

private async Task GoNext() => await swiper.GoNext();

private async Task GoPrev() => await swiper.GoPrev();

private async Task GoTo() => await swiper.GoTo(number);

private async Task GoToStart() => await swiper.GoToStart();

private async Task GoToEnd() => await swiper.GoToEnd();";

    private readonly string example14RazorCode = itemStyle + @"


<BitSwiper AutoPlay
           ShowDots
           ShowPlayPause
           Gap=""0.5rem""
           VisibleItemsCount=""3""
           Snap=""BitSwiperSnap.Start""
           AriaLabel=""Landscape photos""
           ItemAriaLabelFormat=""Photo {0} of {1}""
           DotAriaLabel=""Photo group""
           DotsAriaLabel=""Choose a group of photos to display""
           NextAriaLabel=""Next photo""
           PrevAriaLabel=""Previous photo""
           PlayButtonAriaLabel=""Start the photo slide show""
           PauseButtonAriaLabel=""Stop the photo slide show"">
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem AriaLabel=""@(index == 1 ? ""Aurora over a frozen lake"" : null)"">
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example15RazorCode = cardStyle + @"


<BitSwiper ShowDots Color=""BitColor.Primary"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Color=""BitColor.Success"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Color=""BitColor.Warning"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Color=""BitColor.Error"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Accent=""BitColorKind.Secondary"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Accent=""BitColorKind.Tertiary"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example16RazorCode = itemStyle + @"


<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitSwiper NextIcon=""@BitIconInfo.Fa(""solid chevron-right"")"" PrevIcon=""@BitIconInfo.Fa(""solid chevron-left"")"">
    @for (int i = 1; i <= 8; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSwiper NextIcon=""@BitIconInfo.Bi(""arrow-right"")"" PrevIcon=""@BitIconInfo.Bi(""arrow-left"")"">
    @for (int i = 1; i <= 8; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example17RazorCode = cardStyle + @"


<BitSwiper ShowDots Size=""BitSize.Small"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Size=""BitSize.Medium"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Size=""BitSize.Large"" Gap=""0.5rem"" VisibleItemsCount=""2"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example18RazorCode = boxStyle + @"

<style>
    .custom-item {
        border-radius: 0.5rem;
        outline: 2px solid mediumpurple;
        outline-offset: -2px;
    }
</style>


<BitSwiper ShowDots
           Gap=""0.5rem""
           VisibleItemsCount=""4""
           Snap=""BitSwiperSnap.Start""
           Style=""padding: 0.5rem; border-radius: 0.5rem; background: rgba(128,128,128,0.15)""
           Classes=""@(new() { CurrentItem = ""custom-item"" })""
           Styles=""@(new() { Buttons = ""color: white; background-color: rgba(0,0,0,0.35); width: 2.5rem;"",
                             CurrentDot = ""background-color: mediumpurple;"" })"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example19RazorCode = boxStyle + @"


<BitSwiper Dir=""BitDir.Rtl"" ShowDots Gap=""0.5rem"" VisibleItemsCount=""4"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">مورد @index</div></BitSwiperItem>
    }
</BitSwiper>";
}
