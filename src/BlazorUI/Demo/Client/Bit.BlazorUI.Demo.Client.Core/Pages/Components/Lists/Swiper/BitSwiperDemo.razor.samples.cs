namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Swiper;

public partial class BitSwiperDemo
{
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


<BitSwiper ScrollItemsCount=""2"" Rewind>
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>

<BitSwiper HideNextPrev>
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example3RazorCode = cardStyle + @"


<BitSwiper VisibleItemsCount=""4"" Gap=""1rem"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper VisibleItemsCount=""1"" VisibleItemsCountSm=""2"" VisibleItemsCountMd=""3"" VisibleItemsCountLg=""5"" Gap=""0.5rem"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example4RazorCode = boxStyle + @"


<BitChoiceGroup @bind-Value=""snap"" Horizontal TItem=""BitChoiceGroupOption<BitSwiperSnap>"" TValue=""BitSwiperSnap"" Label=""Snap"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitSwiperSnap.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitSwiperSnap.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitSwiperSnap.End"" />
</BitChoiceGroup>

<BitSwiper Snap=""snap"" VisibleItemsCount=""3"" Gap=""0.5rem"">
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";
    private readonly string example4CsharpCode = @"
private BitSwiperSnap snap = BitSwiperSnap.Center;";

    private readonly string example5RazorCode = boxStyle + @"


<BitSwiper Vertical Style=""height: 200px"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"" AriaLabel=""Vertical items"">
    @for (int i = 1; i <= 12; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example6RazorCode = boxStyle + @"


<BitSwiper ShowDots VisibleItemsCount=""4"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots VisibleItemsCount=""4"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    <DotTemplate Context=""index""><span>@(index + 1)</span></DotTemplate>
    <ChildContent>
        @for (int i = 1; i <= 16; i++)
        {
            var index = i;
            <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
        }
    </ChildContent>
</BitSwiper>";

    private readonly string example7RazorCode = boxStyle + @"


<BitSwiper AutoPlay AutoPlayInterval=""1500"" ShowDots ShowPlayPause StopOnInteraction
           VisibleItemsCount=""4"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example8RazorCode = boxStyle + @"


<BitSwiper Wheel NoDrag AnimationDuration=""1.5"" VisibleItemsCount=""4"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowScrollbar HideNextPrev AnimationDuration=""0"" VisibleItemsCount=""4"" Gap=""0.5rem"">
    @for (int i = 1; i <= 16; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""box-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example9RazorCode = boxStyle + @"


<BitSwiper @ref=""swiper"" DefaultItem=""5"" HideNextPrev VisibleItemsCount=""4"" Gap=""0.5rem""
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
<BitNumberField @bind-Value=""number"" Min=""1"" Max=""16"" Mode=""BitSpinButtonMode.Compact"" AriaLabel=""Item number"" />

<BitButton OnClick=""GoToStart"">Start</BitButton>
<BitButton OnClick=""GoToEnd"">End</BitButton>

<div>Current item: @(currentIndex + 1)</div>";
    private readonly string example9CsharpCode = @"
private int number = 1;
private int currentIndex;
private BitSwiper swiper = default!;

private async Task GoNext() => await swiper.GoNext();

private async Task GoPrev() => await swiper.GoPrev();

private async Task GoTo() => await swiper.GoTo(number);

private async Task GoToStart() => await swiper.GoToStart();

private async Task GoToEnd() => await swiper.GoToEnd();";

    private readonly string example10RazorCode = itemStyle + @"


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

    private readonly string example11RazorCode = cardStyle + @"


<BitParams Parameters=""@swiperParams"">
    <BitSwiper>
        @for (int i = 1; i <= 12; i++)
        {
            var index = i;
            <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
        }
    </BitSwiper>

    <BitSwiper VisibleItemsCount=""2"" ShowDots=""false"">
        @for (int i = 1; i <= 12; i++)
        {
            var index = i;
            <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
        }
    </BitSwiper>
</BitParams>";
    private readonly string example11CsharpCode = @"
private readonly BitSwiperParams[] swiperParams =
[
    new()
    {
        ShowDots = true,
        Rewind = true,
        Gap = ""0.5rem"",
        VisibleItemsCount = 4,
        Snap = BitSwiperSnap.Start,
        ScrollItemsCount = 2,
    }
];";

    private readonly string example12RazorCode = cardStyle + @"


<BitSwiper ShowDots Color=""BitColor.Primary"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Color=""BitColor.Success"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Color=""BitColor.Warning"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Color=""BitColor.Error"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Accent=""BitColorKind.Secondary"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Accent=""BitColorKind.Tertiary"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example13RazorCode = itemStyle + @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />


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

    private readonly string example14RazorCode = cardStyle + @"


<BitSwiper ShowDots Size=""BitSize.Small"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Size=""BitSize.Medium"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>

<BitSwiper ShowDots Size=""BitSize.Large"" VisibleItemsCount=""2"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
    @for (int i = 1; i <= 6; i++)
    {
        var index = i;
        <BitSwiperItem><div class=""card-item"">@index</div></BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example15RazorCode = boxStyle + @"
<style>
    .custom-item {
        border-radius: 0.5rem;
        outline: 2px solid mediumpurple;
        outline-offset: -2px;
    }

    /* The public variables are never declared by the swiper, so they inherit from any ancestor. */
    .themed-swiper {
        --bit-Swiper-dot-current-width: 1.5rem;
        --bit-Swiper-dot-current-color: rebeccapurple;
        --bit-Swiper-dot-current-hover-color: indigo;
        --bit-Swiper-focus-color: rebeccapurple;
        --bit-Swiper-button-width: 2.5rem;
        --bit-Swiper-button-opacity: 1;
        --bit-Swiper-button-color: white;
        --bit-Swiper-button-hover-color: white;
        --bit-Swiper-button-background: rgba(0, 0, 0, 0.35);
        --bit-Swiper-button-hover-background: rgba(0, 0, 0, 0.6);
    }

    .themed-swiper .image {
        width: 100%;
        height: 8rem;
        display: block;
        object-fit: cover;
        border-radius: 0.5rem;
    }
</style>


<BitSwiper ShowDots
           VisibleItemsCount=""4""
           Gap=""0.5rem""
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
</BitSwiper>

<div class=""themed-swiper"">
    <BitSwiper ShowDots VisibleItemsCount=""3"" Gap=""0.75rem"" Snap=""BitSwiperSnap.Start"">
        @for (int i = 1; i <= 12; i++)
        {
            var index = i;
            var imageIndex = (index - 1) % 4 + 1;
            <BitSwiperItem>
                <img class=""image"" alt=""Landscape @index"" src=""img@(imageIndex).jpg"" />
            </BitSwiperItem>
        }
    </BitSwiper>
</div>";

    private readonly string example16RazorCode = boxStyle + @"


<div dir=""rtl"">
    <BitSwiper Dir=""BitDir.Rtl"" ShowDots VisibleItemsCount=""4"" Gap=""0.5rem"" Snap=""BitSwiperSnap.Start"">
        @for (int i = 1; i <= 16; i++)
        {
            var index = i;
            <BitSwiperItem><div class=""box-item"">مورد @index</div></BitSwiperItem>
        }
    </BitSwiper>
</div>";
}
