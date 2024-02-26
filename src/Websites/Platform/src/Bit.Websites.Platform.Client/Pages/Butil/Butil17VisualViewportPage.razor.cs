namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil17VisualViewportPage
{
    private double offsetLeft;

    private async Task GetOffsetLeft()
    {
        offsetLeft = await visualViewport.GetOffsetLeft();
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
}
