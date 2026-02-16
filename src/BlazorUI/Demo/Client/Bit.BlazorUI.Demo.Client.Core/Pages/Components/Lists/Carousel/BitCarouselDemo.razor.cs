namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Carousel;

public partial class BitCarouselDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Description = "Hides the Dots indicator at the bottom of the BitCarousel."
        },
        new()
        {
            Name = "HideNextPrev",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the Next/Prev buttons of the BitCarousel."
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
            Name = "OnChange",
            Type = "EventCallback<int>",
            Description = "The event that will be called on carousel page navigation."
        },
        new()
        {
            Name = "ScrollItemsCount",
            Type = "int",
            DefaultValue = "1",
            Description = "Number of items that is going to be changed on navigation."
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
            Name = "VisibleItemsCount",
            Type = "int",
            DefaultValue = "1",
            Description = "Number of items that is visible in the carousel."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitTimelineClassStyles",
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
                   Name = "Buttons",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the next/prev buttons of the BitCarousel."
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
                   Name = "CurrectDot",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the current dot element of the BitCarousel."
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

    private readonly List<ComponentParameter> componentPublicMembers =
    [
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
            Description = "Navigates to the given carousel item number.",
        },
        new()
        {
            Name = "Pause",
            Type = "Task",
            Description = "Pauses the AutoPlay if enabled.",
        },
        new()
        {
            Name = "Resume",
            Type = "Task",
            Description = "Resumes the AutoPlay if enabled.",
        }
    ];



    private int number = 1;
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



    private readonly string example1RazorCode = @"
<style>
    .number {
        position: absolute;
        top: 0.75rem;
        padding: 0.75rem;
        font-size: 0.75rem;
        color: #D7D7D7;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>

<BitCarousel>
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img class=""image"" src=""img1.jpg"">
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img class=""image"" src=""img2.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img class=""image"" src=""img3.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img class=""image"" src=""img4.jpg"" />
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example2RazorCode = @"
<style>
    .image {
        width: 100%;
        height: 100%;
    }

    .text-title {
        position: absolute;
        bottom: 3.4375rem;
        width: 100%;
        font-size: 0.9375rem;
        text-align: center;
        color: #FFFFFF;
    }

    .text-description {
        position: absolute;
        bottom: 1.875rem;
        width: 100%;
        font-size: 0.6875rem;
        text-align: center;
        color: #FFFFFF;
    }
</style>

<BitCarousel InfiniteScrolling>
    <BitCarouselItem>
        <img class=""image"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example3RazorCode = @"
<style>
    .image {
        width: 100%;
        height: 100%;
    }

    .text-title {
        position: absolute;
        bottom: 3.4375rem;
        width: 100%;
        font-size: 0.9375rem;
        text-align: center;
        color: #FFFFFF;
    }

    .text-description {
        position: absolute;
        bottom: 1.875rem;
        width: 100%;
        font-size: 0.6875rem;
        text-align: center;
        color: #FFFFFF;
    }
</style>

<BitCarousel HideDots>
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img class=""image"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img class=""image"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img class=""image"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img class=""image"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example4RazorCode = @"
<style>
    .image {
        width: 100%;
        height: 100%;
    }

    .text-title {
        position: absolute;
        bottom: 3.4375rem;
        width: 100%;
        font-size: 0.9375rem;
        text-align: center;
        color: #FFFFFF;
    }

    .text-description {
        position: absolute;
        bottom: 1.875rem;
        width: 100%;
        font-size: 0.6875rem;
        text-align: center;
        color: #FFFFFF;
    }
</style>

<BitCarousel HideNextPrev @ref=""carousel"">
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img class=""image"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img class=""image"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img class=""image"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img class=""image"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>

<BitButton OnClick=""GoPrev"">&lt; Prev</BitButton>
<BitButton OnClick=""GoNext"">Next &gt;</BitButton>

<BitButton OnClick=""GoTo"">GoTo</BitButton>
<BitNumberField @bind-Value=""number"" Min=""1"" Max=""4"" Mode=""BitSpinButtonMode.Compact"" />";
    private readonly string example4CsharpCode = @"
private int number = 1;
private BitCarousel carousel;

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
}";

    private readonly string example5RazorCode = @"
<style>
    .image {
        width: 100%;
        height: 100%;
    }

    .text-title {
        position: absolute;
        bottom: 3.4375rem;
        width: 100%;
        font-size: 0.9375rem;
        text-align: center;
        color: #FFFFFF;
    }

    .text-description {
        position: absolute;
        bottom: 1.875rem;
        width: 100%;
        font-size: 0.6875rem;
        text-align: center;
        color: #FFFFFF;
    }
</style>

<BitCarousel HideNextPrev InfiniteScrolling AutoPlay AutoPlayInterval=""2500"">
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img class=""image"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img class=""image"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img class=""image"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img class=""image"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example6RazorCode = @"
<style>
    .item {
        text-align: center;
        color: black;
        border: 1px solid blue;
        background-color: lightblue;
    }

    .item div {
        transform: translate(0, 80%);
    }
</style>

<div>
    <BitCarousel Style=""height: 100px"" VisibleItemsCount=""3"" ScrollItemsCount=""3"">
        <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>5</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>6</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>7</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>8</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>9</div></BitCarouselItem>
    </BitCarousel>
</div>";

    private readonly string example7RazorCode = @"
<style>
    .item {
        text-align: center;
        color: black;
        border: 1px solid blue;
        background-color: lightblue;
    }

    .item div {
        transform: translate(0, 80%);
    }
</style>

<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitCarousel Style=""height: 100px"" InfiniteScrolling
             GoLeftIcon=""@BitIconInfo.Fa(""solid chevron-left"")""
             GoRightIcon=""@BitIconInfo.Fa(""solid chevron-right"")"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>5</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>6</div></BitCarouselItem>
</BitCarousel>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitCarousel Style=""height: 100px"" InfiniteScrolling
             GoLeftIcon=""@BitIconInfo.Bi(""chevron-left"")""
             GoRightIcon=""@BitIconInfo.Bi(""chevron-right"")"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>5</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>6</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example8RazorCode = @"
<style>
    .item {
        text-align: center;
        color: black;
        border: 1px solid blue;
        background-color: lightblue;
    }

    .item div {
        transform: translate(0, 80%);
    }
</style>

<div>
    <BitCarousel Style=""height: 100px"" Dir=""BitDir.Rtl"" VisibleItemsCount=""3"" ScrollItemsCount=""1"" InfiniteScrolling>
        <BitCarouselItem Class=""item""><div>یک</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>دو</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>سه</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>چهار</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>پنج</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>شیش</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>هفت</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>هشت</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>نه</div></BitCarouselItem>
    </BitCarousel>
</div>";
}
