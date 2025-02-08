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
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom icon name for the go to left button at the right side of the carousel."
        },
        new()
        {
            Name = "GoRightIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom icon name for the go to right button at the left side of the carousel."
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
        }
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
            Description = "Navigates to the given carousel item index.",
        }
    ];



    private int index;
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
        await carousel.GoTo(index);
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

    .buttons-container {
        display: flex;
        justify-content: space-between;
    }

    .goto-container {
        display: flex;
        justify-content: space-around;
    }

    .goto-button {
        margin-right: rem(10px);
    }
</style>

<div>
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
</div>

<div class=""buttons-container"">
    <div>
        <BitButton OnClick=""GoPrev"">&lt; Prev</BitButton>
        <BitButton OnClick=""GoNext"">Next &gt;</BitButton>
    </div>
    <div class=""goto-container"">
        <BitButton Class=""goto-button"" OnClick=""GoTo"">GoTo</BitButton>
        <BitNumberField @bind-Value=""index"" />
    </div>
</div>";
    private readonly string example4CsCode = @"
private int index;
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
    await carousel.GoTo(index);
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

    .buttons-container {
        display: flex;
        justify-content: space-between;
    }

    .goto-container {
        display: flex;
        justify-content: space-around;
    }

    .goto-button {
        margin-right: rem(10px);
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
