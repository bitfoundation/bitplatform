namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil19ScreenOrientationPage
{
    private ushort angle;
    
    private string? orientationType;

    private async Task GetOrientationType()
    {
        var result = await screenOrientation.GetOrientationType();
        orientationType = result.ToString();
    }

    private async Task GetAngle()
    {
        angle = await screenOrientation.GetAngle();
    }


    private string getOrientationTypeExampleCode =
@"@inject Bit.Butil.ScreenOrientation screenOrientation

<BitButton OnClick=""@GetOrientationType"">GetOrientationType</BitButton>

<div>Orientation type: @orientationType</div>

@code {
    private string? orientationType;

    private async Task GetOrientationType()
    {
        var result = await screenOrientation.GetOrientationType();
        orientationType = result.ToString();
    }
}";
    private string getAngleExampleCode =
@"@inject Bit.Butil.ScreenOrientation screenOrientation

<BitButton OnClick=""@GetAngle"">GetAngle</BitButton>

<div>Angle: @angle</div>

@code {
    private ushort angle;

    private async Task GetAngle()
    {
        angle = await screenOrientation.GetAngle();
    }
}";
}
