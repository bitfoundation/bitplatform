using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Carousel;

public partial class BitCarouselDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "The aria-label of the control for the benefit of screen readers."
        },
        new()
        {
            Name = "Class",
            Type = "string",
            DefaultValue = "",
            Description = "Custom CSS class for the root element of the component."
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
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Items of the carousel."
        },
        new()
        {
            Name = "SelectedKey",
            Type = "string",
            DefaultValue = "",
            Description = "The Key of the current item of the carousel."
        },
        new()
        {
            Name = "ShowDots",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the Dots indicator at the bottom of the BitCarousel."
        },
        new()
        {
            Name = "ShowNextPrev",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the Next/Prev buttons of the BitCarousel."
        },
        new()
        {
            Name = "AutoPlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables/disables the auto scrolling of the slides."
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"
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
        <img class=""image"" src=""/images/carousel/img1.jpg"">
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example2HTMLCode = @"
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

<BitCarousel InfiniteScrolling=""true"">
    <BitCarouselItem>
        <img class=""image"" src=""/images/carousel/img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example3HTMLCode = @"
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

<BitCarousel ShowDots=""false"">
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example4HTMLCode = @"
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

    ::deep .goto-button {
        margin-right: rem(10px);
    }
</style>

<div>
    <BitCarousel ShowNextPrev=""false"" @ref=""carousel"">
        <BitCarouselItem>
            <div class=""numbertext"">1 / 4</div>
            <img class=""image"" src=""/images/carousel/img1.jpg"" />
            <div class=""text-title"">Aurora</div>
            <div class=""text-description"">This is Aurora and it's fantastic</div>
        </BitCarouselItem>
        <BitCarouselItem>
            <div class=""numbertext"">2 / 4</div>
            <img class=""image"" src=""/images/carousel/img2.jpg"" />
            <div class=""text-title"">Beautiful Mountain</div>
            <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
        </BitCarouselItem>
        <BitCarouselItem>
            <div class=""numbertext"">3 / 4</div>
            <img class=""image"" src=""/images/carousel/img3.jpg"" />
            <div class=""text-title"">Forest In The Valley</div>
            <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
        </BitCarouselItem>
        <BitCarouselItem>
            <div class=""numbertext"">4 / 4</div>
            <img class=""image"" src=""/images/carousel/img4.jpg"" />
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
        <BitNumericTextField @bind-Value=""index""></BitNumericTextField>
    </div>
</div>";

    private readonly string example4CsCode = @"
private int index;
private BitCarousel carousel;

private void GoNext()
{
    carousel.GoNext();
}

private void GoPrev()
{
    carousel.GoPrev();
}

private void GoTo()
{
    carousel.GoTo(index);
}";

    private readonly string example5HTMLCode = @"
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

    ::deep .goto-button {
        margin-right: rem(10px);
    }
</style>

<BitCarousel ShowNextPrev=""false"" @ref=""carousel"">
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img class=""image"" src=""/images/carousel/img1.jpg"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img class=""image"" src=""/images/carousel/img2.jpg"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img class=""image"" src=""/images/carousel/img3.jpg"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img class=""image"" src=""/images/carousel/img4.jpg"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example6HTMLCode = @"
<style>
    ::deep .item {
        text-align: center;
        color: black;
        border: 1px solid blue;
        background-color: lightblue;
    }

    ::deep .item div {
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
}
