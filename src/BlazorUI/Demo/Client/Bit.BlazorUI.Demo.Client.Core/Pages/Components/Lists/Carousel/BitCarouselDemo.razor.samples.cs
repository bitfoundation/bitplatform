namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Carousel;

public partial class BitCarouselDemo
{
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

<BitCarousel AriaLabel=""Landscape photos"">
    <BitCarouselItem>
        <div class=""number"">1 / 4</div>
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"">
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">2 / 4</div>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">3 / 4</div>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">4 / 4</div>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
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
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example3RazorCode = @"
<BitCarousel HideDots>
    <BitCarouselItem>
        <div class=""number"">1 / 4</div>
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">2 / 4</div>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">3 / 4</div>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">4 / 4</div>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example4RazorCode = @"
<BitCarousel HideNextPrev @ref=""carousel"" OnChange=""v => currentPage = v"">
    <BitCarouselItem>
        <div class=""number"">1 / 4</div>
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">2 / 4</div>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">3 / 4</div>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">4 / 4</div>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>

<BitButton OnClick=""GoPrev"">&lt; Prev</BitButton>
<BitButton OnClick=""GoNext"">Next &gt;</BitButton>

<BitButton OnClick=""GoTo"">GoTo</BitButton>
<BitNumberField @bind-Value=""number"" Min=""1"" Max=""4"" Mode=""BitSpinButtonMode.Compact"" />

<div>Current page: @currentPage</div>";
    private readonly string example4CsharpCode = @"
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
}";

    private readonly string example5RazorCode = @"
<BitCarousel HideNextPrev InfiniteScrolling AutoPlay AutoPlayInterval=""2500"">
    <BitCarouselItem>
        <div class=""number"">1 / 4</div>
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">2 / 4</div>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">3 / 4</div>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">4 / 4</div>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>

<BitCarousel InfiniteScrolling AutoPlay AutoPlayInterval=""2000"" ShowPlayPause StopOnInteraction>
    <BitCarouselItem>
        <div class=""number"">1 / 4</div>
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">2 / 4</div>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">3 / 4</div>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">4 / 4</div>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
    </BitCarouselItem>
</BitCarousel>

<BitCarousel AutoPlay AutoPlayReverse InfiniteScrolling HideNextPrev Style=""height: 100px"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
</BitCarousel>

<BitCarousel AutoPlay StopOnLastSlide HideNextPrev Style=""height: 100px"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
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

<BitCarousel Style=""height: 100px"" VisibleItemsCount=""3"" ScrollItemsCount=""1"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>5</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>6</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>7</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>8</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>9</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example7RazorCode = @"
<style>
    .gap-item {
        height: 100%;
        box-sizing: border-box;
    }
</style>

<BitCarousel Style=""height: 100px"" VisibleItemsCount=""3"" ScrollItemsCount=""1"" Gap=""1rem"" InfiniteScrolling>
    <BitCarouselItem><div class=""item gap-item"">1</div></BitCarouselItem>
    <BitCarouselItem><div class=""item gap-item"">2</div></BitCarouselItem>
    <BitCarouselItem><div class=""item gap-item"">3</div></BitCarouselItem>
    <BitCarouselItem><div class=""item gap-item"">4</div></BitCarouselItem>
    <BitCarouselItem><div class=""item gap-item"">5</div></BitCarouselItem>
    <BitCarouselItem><div class=""item gap-item"">6</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example8RazorCode = @"
<BitCarousel Vertical Style=""height: 200px"" InfiniteScrolling>
    <BitCarouselItem>
        <div class=""number"">1 / 4</div>
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">2 / 4</div>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">3 / 4</div>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">4 / 4</div>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example9RazorCode = @"
<BitCarousel Fade InfiniteScrolling>
    <BitCarouselItem>
        <div class=""number"">1 / 4</div>
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">2 / 4</div>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">3 / 4</div>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""number"">4 / 4</div>
        <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example10RazorCode = @"
<BitCarousel Wheel NoDrag InfiniteScrolling Style=""height: 100px"" VisibleItemsCount=""3"" ScrollItemsCount=""1"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>5</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>6</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example11RazorCode = @"
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

<BitCarousel Style=""height: 100px"" AnimationDuration=""1.5"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
</BitCarousel>

<BitCarousel Style=""height: 100px"" AnimationDuration=""0"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example12RazorCode = @"
<BitCarousel Style=""height: 100px"" DefaultPage=""3"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>5</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example13RazorCode = @"
<BitCarousel Style=""height: 100px""
             VisibleItemsCount=""4""
             ScrollItemsCount=""4""
             InfiniteScrolling
             ResponsiveOptions=""@([new() { Breakpoint = 640, VisibleItemsCount = 1, ScrollItemsCount = 1 },
                                   new() { Breakpoint = 960, VisibleItemsCount = 2, ScrollItemsCount = 2 },
                                   new() { Breakpoint = 1280, VisibleItemsCount = 3, ScrollItemsCount = 3 }])"">
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>5</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>6</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>7</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>8</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example14RazorCode = @"
<style>
    .thumb-dot {
        width: 3rem;
        height: 2rem;
        padding: 0;
        overflow: hidden;
        border-radius: 0.25rem;
        opacity: 0.5;
    }

    .thumb-dot-current {
        opacity: 1;
        outline: 2px solid var(--bit-clr-pri);
    }

    .thumb {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }
</style>

<BitCarousel Style=""height: 100px"" InfiniteScrolling>
    <DotTemplate Context=""index""><span>@(index + 1)</span></DotTemplate>
    <ChildContent>
        <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
        <BitCarouselItem Class=""item""><div>4</div></BitCarouselItem>
    </ChildContent>
</BitCarousel>

<BitCarousel InfiniteScrolling
             Classes=""@(new() { Dots = ""thumb-dot"", CurrentDot = ""thumb-dot-current"" })"">
    <DotTemplate Context=""index"">
        <img class=""thumb"" alt="""" src=""@($""img{index + 1}.jpg"")"" />
    </DotTemplate>
    <ChildContent>
        <BitCarouselItem>
            <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img class=""image"" alt=""Road among the mountains"" src=""img4.jpg"" />
        </BitCarouselItem>
    </ChildContent>
</BitCarousel>";

    private readonly string example15RazorCode = @"
<BitCarousel AutoPlay
             ShowPlayPause
             InfiniteScrolling
             AriaLabel=""Landscape photos""
             ItemAriaLabelFormat=""Photo {0} of {1}""
             DotAriaLabel=""Photo""
             DotsAriaLabel=""Choose a photo to display""
             GoLeftAriaLabel=""Next photo""
             GoRightAriaLabel=""Previous photo""
             PlayButtonAriaLabel=""Start the photo slide show""
             PauseButtonAriaLabel=""Stop the photo slide show"">
    <BitCarouselItem AriaLabel=""Aurora over a frozen lake"">
        <img class=""image"" alt=""Aurora"" src=""img1.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Beautiful mountain"" src=""img2.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Forest in the valley"" src=""img3.jpg"" />
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example16RazorCode = @"
<BitCarousel Color=""BitColor.Primary"" Style=""height: 72px"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
</BitCarousel>

<BitCarousel Color=""BitColor.Success"" Style=""height: 72px"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
</BitCarousel>

<BitCarousel Color=""BitColor.Warning"" Style=""height: 72px"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
</BitCarousel>

<BitCarousel Color=""BitColor.Error"" Style=""height: 72px"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<div>FontAwesome (circle-arrow icons):</div>
<BitCarousel InfiniteScrolling
             GoLeftIcon=""@BitIconInfo.Fa(""solid circle-arrow-right"")""
             GoRightIcon=""@BitIconInfo.Fa(""solid circle-arrow-left"")"">
    <BitCarouselItem>
        <img class=""image"" alt=""Aurora"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Beautiful mountain"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Forest in the valley"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Road among the mountains"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<div>Bootstrap Icons (arrow-left-circle / arrow-right-circle):</div>
<BitCarousel InfiniteScrolling
             GoLeftIcon=""@BitIconInfo.Bi(""arrow-right-circle"")""
             GoRightIcon=""@BitIconInfo.Bi(""arrow-left-circle"")"">
    <BitCarouselItem>
        <img class=""image"" alt=""Aurora"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Beautiful mountain"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Forest in the valley"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" alt=""Road among the mountains"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example18RazorCode = @"
<BitCarousel Size=""BitSize.Small"" Style=""height: 72px"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
</BitCarousel>

<BitCarousel Size=""BitSize.Medium"" Style=""height: 72px"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
</BitCarousel>

<BitCarousel Size=""BitSize.Large"" Style=""height: 72px"" InfiniteScrolling>
    <BitCarouselItem Class=""item""><div>1</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>2</div></BitCarouselItem>
    <BitCarouselItem Class=""item""><div>3</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example19RazorCode = @"
<style>
    .custom-item {
        color: white;
        display: grid;
        place-items: center;
        background: linear-gradient(135deg, #5843be, #9787e5);
    }
</style>

<BitCarousel Style=""height: 100px; border-radius: 0.5rem; overflow: hidden""
             InfiniteScrolling
             Styles=""@(new() { Buttons = ""color: white; background-color: rgba(0,0,0,0.35); width: 2.5rem;"",
                               CurrentDot = ""background-color: mediumpurple;"" })""
             Classes=""@(new() { Item = ""custom-item"" })"">
    <BitCarouselItem><div>1</div></BitCarouselItem>
    <BitCarouselItem><div>2</div></BitCarouselItem>
    <BitCarouselItem><div>3</div></BitCarouselItem>
    <BitCarouselItem><div>4</div></BitCarouselItem>
</BitCarousel>";

    private readonly string example20RazorCode = @"
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
</BitCarousel>";
}
