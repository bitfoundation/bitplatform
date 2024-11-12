namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil18VisualViewportPage
{
    private double offsetLeft;

    private double offsetTop;
    
    private double pageLeft;

    private double pageTop;
    
    private double width;
    
    private double height;
    
    private double scale;


    private async Task GetOffsetLeft()
    {
        offsetLeft = await visualViewport.GetOffsetLeft();
    }
    private async Task GetOffsetTop()
    {
        offsetTop = await visualViewport.GetOffsetTop();
    }

    private async Task GetPageLeft()
    {
        pageLeft = await visualViewport.GetPageLeft();
    }

    private async Task GetPageTop()
    {
        pageTop = await visualViewport.GetPageTop();
    }

    private async Task GetWidth()
    {
        width = await visualViewport.GetWidth();
    }

    private async Task GetHeight()
    {
        height = await visualViewport.GetHeight();
    }

    private async Task GetScale()
    {
        scale = await visualViewport.GetScale();
    }


    private string getOffsetLeftExampleCode =
@"@inject Bit.Butil.VisualViewport visualViewport

<BitButton OnClick=""@GetOffsetLeft"">GetOffsetLeft</BitButton>

<div>OffsetLeft: @offsetLeft</div>

@code {
    private double offsetLeft;

    private async Task GetOffsetLeft()
    {
        offsetLeft = await visualViewport.GetOffsetLeft();
    }
}";
    private string getOffsetTopExampleCode =
@"@inject Bit.Butil.VisualViewport visualViewport

<BitButton OnClick=""@GetOffsetTop"">GetOffsetTop</BitButton>

<div>OffsetTop: @offsetTop</div>

@code {
    private double offsetTop;

    private async Task GetOffsetTop()
    {
        offsetTop = await visualViewport.GetOffsetTop();
    }
}";
    private string getPageLeftExampleCode =
@"@inject Bit.Butil.VisualViewport visualViewport

<BitButton OnClick=""@GetPageLeft"">GetPageLeft</BitButton>

<div>pageLeft: @pageLeft</div>

@code {
    private double pageLeft;

    private async Task GetPageLeft()
    {
        pageLeft = await visualViewport.GetPageLeft();
    }
}";
    private string getPageTopExampleCode =
@"@inject Bit.Butil.VisualViewport visualViewport

<BitButton OnClick=""@GetPageTop"">GetPageTop</BitButton>

<div>pageTop: @pageTop</div>

@code {
    private double pageTop;

    private async Task GetPageTop()
    {
        pageTop = await visualViewport.GetPageTop();
    }
}";
    private string getWidthExampleCode =
@"@inject Bit.Butil.VisualViewport visualViewport

<BitButton OnClick=""@GetWidth"">GetWidth</BitButton>

<div>Width: @width</div>

@code {
    private double width;

    private async Task GetWidth()
    {
        width = await visualViewport.GetWidth();
    }
}";
    private string getHeightExampleCode =
@"@inject Bit.Butil.VisualViewport visualViewport

<BitButton OnClick=""@GetHeight"">GetHeight</BitButton>

<div>Height: @height</div>

@code {
    private double height;

    private async Task GetHeight()
    {
        height = await visualViewport.GetHeight();
    }
}";
    private string getScaleExampleCode =
@"@inject Bit.Butil.VisualViewport visualViewport

<BitButton OnClick=""@GetScale"">GetScale</BitButton>

<div>Height: @height</div>

@code {
    private double height;

    private async Task GetScale()
    {
        height = await visualViewport.GetScale();
    }
}";
}
