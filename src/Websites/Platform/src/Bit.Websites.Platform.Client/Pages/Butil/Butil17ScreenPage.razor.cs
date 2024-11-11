namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil17ScreenPage
{
    private string? availableHeight;

    private string? availableWidth;

    private string? colorDepth;

    private string? height;

    private string? isExtended;

    private string? pixelDepth;

    private string? width;


    private async Task GetAvailableHeight()
    {
        var result = await screen.GetAvailableHeight();
        availableHeight = result.ToString();
    }

    private async Task GetAvailableWidth()
    {
        var result = await screen.GetAvailableWidth();
        availableWidth = result.ToString();
    }

    private async Task GetColorDepth()
    {
        var result = await screen.GetColorDepth();
        colorDepth = result.ToString();
    }

    private async Task GetHeight()
    {
        var result = await screen.GetHeight();
        height = result.ToString();
    }

    private async Task GetIsExtended()
    {
        var result = await screen.IsExtended();
        isExtended = result.ToString();
    }

    private async Task GetPixelDepth()
    {
        var result = await screen.GetPixelDepth();
        pixelDepth = result.ToString();
    }

    private async Task GetWidth()
    {
        var result = await screen.GetWidth();
        width = result.ToString();
    }


    private string getAvailableHeightExampleCode =
@"@inject Bit.Butil.Screen screen

<BitButton OnClick=""@GetAvailableHeight"">GetAvailableHeight</BitButton>

<div>Available height: @availableHeight</div>

@code {
    private string? availableHeight;

    private async Task GetAvailableHeight()
    {
        var result = await screen.GetAvailableHeight();
        availableHeight = result.ToString();
    }
}";
    private string getAvailableWidthExampleCode =
@"@inject Bit.Butil.Screen screen

<BitButton OnClick=""@GetAvailableWidth"">GetAvailableWidth</BitButton>

<div>Available width: @availableWidth</div>

@code {
    private string? availableWidth;

    private async Task GetAvailableWidth()
    {
        var result = await screen.GetAvailableWidth();
        availableWidth = result.ToString();
    }
}";
    private string getColorDepthExampleCode =
@"@inject Bit.Butil.Screen screen

<BitButton OnClick=""@GetColorDepth"">GetColorDepth</BitButton>

<div>Color depth: @colorDepth</div>

@code {
    private string? colorDepth;

    private async Task GetColorDepth()
    {
        var result = await screen.GetColorDepth();
        colorDepth = result.ToString();
    }
}";
    private string getHeightExampleCode =
@"@inject Bit.Butil.Screen screen

<BitButton OnClick=""@GetHeight"">GetHeight</BitButton>

<div>Height: @height</div>

@code {
    private string? height;

    private async Task GetHeight()
    {
        var result = await screen.GetHeight();
        height = result.ToString();
    }
}";
    private string getIsExtendedExampleCode =
@"@inject Bit.Butil.Screen screen

<BitButton OnClick=""@GetIsExtended"">GetIsExtended</BitButton>

<div>Is extended: @IsExtended</div>

@code {
    private string? isExtended;

    private async Task GetIsExtended()
    {
        var result = await screen.IsExtended();
        isExtended = result.ToString();
    }
}";
    private string getPixelDepthExampleCode =
@"@inject Bit.Butil.Screen screen

<BitButton OnClick=""@GetPixelDepth"">GetPixelDepth</BitButton>

<div>Pixel depth: @PixelDepth</div>

@code {
    private string? pixelDepth;

    private async Task GetPixelDepth()
    {
        var result = await screen.GetPixelDepth();
        pixelDepth = result.ToString();
    }
}";
    private string getWidthExampleCode =
@"@inject Bit.Butil.Screen screen

<BitButton OnClick=""@GetWidth"">GetWidth</BitButton>

<div>Width: @width</div>

@code {
    private string? width;

    private async Task GetWidth()
    {
        var result = await screen.GetWidth();
        width = result.ToString();
    }
}";
}
