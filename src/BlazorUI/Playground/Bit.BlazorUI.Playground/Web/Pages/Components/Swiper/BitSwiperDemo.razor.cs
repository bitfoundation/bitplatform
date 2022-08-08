using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Swiper;

public partial class BitSwiperDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        //new()
        //{
        //    Name = "InfiniteScrolling",
        //    Type = "bool",
        //    DefaultValue = "false",
        //    Description = "If enabled the swiper items will navigate in an infinite loop."
        //},
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Items of the swiper."
        },
        //new()
        //{
        //    Name = "ShowDots",
        //    Type = "bool",
        //    DefaultValue = "true",
        //    Description = "Shows or hides the Dots indicator at the bottom of the BitSwiper."
        //},
        new()
        {
            Name = "ShowNextPrev",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the Next/Prev buttons of the BitSwiper."
        },
        //new()
        //{
        //    Name = "AutoPlay",
        //    Type = "bool",
        //    DefaultValue = "false",
        //    Description = "Enables/disables the auto scrolling of the slides."
        //},
        //new()
        //{
        //    Name = "AutoPlayInterval",
        //    Type = "double",
        //    DefaultValue = "2000",
        //    Description = "Sets the interval of the auto scrolling in milliseconds (the default value is 2000)."
        //},
        //new()
        //{
        //    Name = "AnimationDuration",
        //    Type = "double",
        //    DefaultValue = "0.5",
        //    Description = "Sets the duration of the scrolling animation in seconds (the default value is 0.5)."
        //},
        new()
        {
            Name = "Direction",
            Type = "Direction",
            DefaultValue = "Direction.LeftToRight",
            Description = "Sets the direction of the scrolling (the default value is LeftToRight)."
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "direction-enum",
            Title = "BitDirection Enum",
            Description = "Describes the render direction",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "LeftToRight",
                    Description="Renders content from left to right.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "RightToLeft",
                    Description="Renders content from right to left.",
                    Value="1",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }
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

<BitSwiper ScrollItemsCount=""2"">
    <BitSwiperItem Class=""item"">
        <div class=""number"">1</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">2</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">3</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">4</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">5</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">6</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">7</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">8</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">9</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">10</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">11</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">12</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">13</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">14</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">15</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">16</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">17</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">18</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">19</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">20</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">21</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">22</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">23</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">24</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">25</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">26</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">27</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">28</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">29</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">30</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">31</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">32</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
</BitSwiper>";

    private readonly string example2HTMLCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }
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

<BitSwiper>
    <BitSwiperItem Class=""item"">
        <div class=""number"">یک</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">دو</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">سه</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">چهار</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">پنج</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">شش</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">هفت</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">هشت</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">نه</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">ده</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">11</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">12</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">13</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">14</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">15</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">16</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">17</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">18</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">19</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">20</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">21</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">22</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">23</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">24</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">25</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">26</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">27</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">28</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">29</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">30</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">31</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">32</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitSwiperItem>
</BitSwiper>";
}
