using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil21UserAgentPage
{
    private UserAgentProperties? props;

    private async Task Extract()
    {
        props = await userAgent.Extract();
    }


    private string extractExampleCode =
@"@inject Bit.Butil.UserAgent userAgent

<BitButton OnClick=""@Extract"">Extract</BitButton>

<div>Name: @props?.Name</div>
<div>Version: @props?.Version</div>
<div>Prerelease: @props?.Prerelease</div>
<div>Layout: @props?.Layout</div>
<div>Manufacturer: @props?.Manufacturer</div>
<div>Product: @props?.Product</div>
<div>OsName: @props?.OsName</div>
<div>OsVersion: @props?.OsVersion</div>
<div>OsArchitecture: @props?.OsArchitecture</div>
<div>Description: @props?.Description</div>
<div>UserAgentValue: @props?.UserAgentValue</div>

@code {
    private Bit.Butil.UserAgentProperties? props;

    private async Task Extract()
    {
        props = await userAgent.Extract();
    }
}";
}
