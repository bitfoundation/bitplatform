namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Swiper;

public partial class BitSwiperDemo
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
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items of the swiper."
        },
        new()
        {
            Name = "HideNextPrev",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the Next/Prev buttons of the BitSwiper."
        },
        new()
        {
            Name = "ScrollItemsCount",
            Type = "int",
            DefaultValue = "1",
            Description = "Number of items that is going to be changed on navigation."
        }
    ];



    private readonly string example1RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper>
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">@index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example2RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper ScrollItemsCount=""2"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">@index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example3RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper HideNextPrev ScrollItemsCount=""2"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">@index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example4RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper Dir=""BitDir.Rtl"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">مورد @index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";
}
