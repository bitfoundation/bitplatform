namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil18ScreenOrientationPage
{
    private ushort angle;

    private async Task GetAngle()
    {
        angle = await screenOrientation.GetAngle();
    }


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
