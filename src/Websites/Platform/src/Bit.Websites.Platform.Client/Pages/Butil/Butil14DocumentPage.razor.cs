using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil14DocumentPage
{
    private bool isDesignModeOn;


    private async Task SetDesignMode()
    {
        await document.SetDesignMode(isDesignModeOn ? DesignMode.On : DesignMode.Off);
    }


    private string designModeExampleCode =
@"@inject Bit.Butil.Document document

<BitCheckbox @bind-Value=""isDesignModeOn"" Label=""@(isDesignModeOn ? ""On"" : ""Off"")"" />

<BitButton OnClick=SetDesignMode>SetDesignMode</BitButton>

@code {
    private bool isDesignModeOn;

    private async Task SetDesignMode()
    {
        await document.SetDesignMode(isDesignModeOn ? DesignMode.On : DesignMode.Off);
    }
}";
}
