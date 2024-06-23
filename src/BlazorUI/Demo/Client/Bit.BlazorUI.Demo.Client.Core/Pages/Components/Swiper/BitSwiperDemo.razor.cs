namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Swiper;

public partial class BitSwiperDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items of the swiper."
        },
        new()
        {
            Name = "ShowNextPrev",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the Next/Prev buttons of the BitSwiper."
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
            Name = "AnimationDuration",
            Type = "double",
            DefaultValue = "0.5",
            Description = "Sets the duration of the scrolling animation in seconds (the default value is 0.5)."
        }
    };



    private readonly string example1RazorCode = @"
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
        <img class=""image"" src=""img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">2</div>
        <img class=""image"" src=""img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">3</div>
        <img class=""image"" src=""img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">4</div>
        <img class=""image"" src=""img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">5</div>
        <img class=""image"" src=""img5.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">6</div>
        <img class=""image"" src=""img6.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">7</div>
        <img class=""image"" src=""img7.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">8</div>
        <img class=""image"" src=""img8.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">9</div>
        <img class=""image"" src=""img9.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">10</div>
        <img class=""image"" src=""img10.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">11</div>
        <img class=""image"" src=""img11.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">12</div>
        <img class=""image"" src=""img12.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">13</div>
        <img class=""image"" src=""img13.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">14</div>
        <img class=""image"" src=""img14.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">15</div>
        <img class=""image"" src=""img15.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">16</div>
        <img class=""image"" src=""img16.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">17</div>
        <img class=""image"" src=""img17.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">18</div>
        <img class=""image"" src=""img18.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">19</div>
        <img class=""image"" src=""img19.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">20</div>
        <img class=""image"" src=""img20.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">21</div>
        <img class=""image"" src=""img21.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">22</div>
        <img class=""image"" src=""img22.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">23</div>
        <img class=""image"" src=""img23.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">24</div>
        <img class=""image"" src=""img24.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">25</div>
        <img class=""image"" src=""img25.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">26</div>
        <img class=""image"" src=""img26.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">27</div>
        <img class=""image"" src=""img27.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">28</div>
        <img class=""image"" src=""img28.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">29</div>
        <img class=""image"" src=""img29.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">30</div>
        <img class=""image"" src=""img30.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">31</div>
        <img class=""image"" src=""img31.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">32</div>
        <img class=""image"" src=""img32.jpg"" />
    </BitSwiperItem>
</BitSwiper>";

    private readonly string example2RazorCode = @"
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

<BitSwiper Dir=""BitDir.Rtl"">
    <BitSwiperItem Class=""item"">
        <div class=""number"">یک</div>
        <img class=""image"" src=""img1.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">دو</div>
        <img class=""image"" src=""img2.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">سه</div>
        <img class=""image"" src=""img3.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">چهار</div>
        <img class=""image"" src=""img4.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">پنج</div>
        <img class=""image"" src=""img5.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">شش</div>
        <img class=""image"" src=""img6.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">هفت</div>
        <img class=""image"" src=""img7.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">هشت</div>
        <img class=""image"" src=""img8.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">نه</div>
        <img class=""image"" src=""img9.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">ده</div>
        <img class=""image"" src=""img10.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">11</div>
        <img class=""image"" src=""img11.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">12</div>
        <img class=""image"" src=""img12.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">13</div>
        <img class=""image"" src=""img13.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">14</div>
        <img class=""image"" src=""img14.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">15</div>
        <img class=""image"" src=""img15.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">16</div>
        <img class=""image"" src=""img16.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">17</div>
        <img class=""image"" src=""img17.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">18</div>
        <img class=""image"" src=""img18.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">19</div>
        <img class=""image"" src=""img19.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">20</div>
        <img class=""image"" src=""img20.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">21</div>
        <img class=""image"" src=""img21.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">22</div>
        <img class=""image"" src=""img22.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">23</div>
        <img class=""image"" src=""img23.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">24</div>
        <img class=""image"" src=""img24.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">25</div>
        <img class=""image"" src=""img25.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">26</div>
        <img class=""image"" src=""img26.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">27</div>
        <img class=""image"" src=""img27.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">28</div>
        <img class=""image"" src=""img28.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">29</div>
        <img class=""image"" src=""img29.jpg"">
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">30</div>
        <img class=""image"" src=""img30.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">31</div>
        <img class=""image"" src=""img31.jpg"" />
    </BitSwiperItem>
    <BitSwiperItem Class=""item"">
        <div class=""number"">32</div>
        <img class=""image"" src=""img32.jpg"" />
    </BitSwiperItem>
</BitSwiper>";
}
