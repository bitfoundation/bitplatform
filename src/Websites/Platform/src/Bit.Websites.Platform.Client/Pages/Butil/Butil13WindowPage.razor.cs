namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil13WindowPage
{
    private float innerHeight;

    private float innerWidth;

    private string? origin;

    private float outerHeight;

    private float outerWidth;

    private string? btoaText;
    private string? btoaValue;

    private string? atobText;
    private string? atobValue;


    private async Task GetInnerHeight() => innerHeight = await window.GetInnerHeight();

    private async Task GetInnerWidth() => innerWidth = await window.GetInnerWidth();

    private async Task GetOrigin() => origin = await window.GetOrigin();

    private async Task GetOuterHeight() => outerHeight = await window.GetOuterHeight();

    private async Task GetOuterWidth() => outerWidth = await window.GetOuterWidth();

    private async Task EncodeData() => btoaText = await window.Btoa(btoaValue!);

    private async Task DecodeData() => atobText = await window.Atob(atobValue!);

    private string getInnerHeightExampleCode =
    @"@inject Bit.Butil.Window window

<BitButton OnClick=GetInnerHeight>GetInnerHeight</BitButton>

<div>InnerHeight is: @innerHeight</div>

@code {
    private string? innerHeight;

    private async Task GetInnerHeight() => innerHeight = await window.GetInnerHeight();
}";
    private string getInnerWidthExampleCode =
@"@inject Bit.Butil.Window window

<BitButton OnClick=GetInnerWidth>GetInnerWidth</BitButton>

<div>InnerWidth is: @innerWidth</div>

@code {
    private string? innerWidth;

    private async Task GetInnerWidth() => innerWidth = await window.GetInnerWidth();
}";
    private string getOriginExampleCode =
    @"@inject Bit.Butil.Window window

<BitButton OnClick=GetOrigin>GetOrigin</BitButton>

<div>Origin is: @origin</div>

@code {
    private string? origin;

    private async Task GetOrigin() => origin = await window.GetOrigin();
}";
    private string getOuterHeightExampleCode =
    @"@inject Bit.Butil.Window window

<BitButton OnClick=GetOuterHeight>GetOuterHeight</BitButton>

<div>OuterHeight is: @outerHeight</div>

@code {
    private float outerHeight;

    private async Task GetOuterHeight() => outerHeight = await window.GetOuterHeight();
}";
    private string getOuterWidthExampleCode =
    @"@inject Bit.Butil.Window window

<BitButton OnClick=GetOuterWidth>GetOuterWidth</BitButton>

<div>OuterWidth is: @outerWidth</div>

@code {
    private float outerWidth;

    private async Task GetOuterWidth() => outerWidth = await window.GetOuterWidth();
}";
    private string btoaExampleCode =
    @"@inject Bit.Butil.Window window

<BitTextField @bind-Value=""btoaValue"" />

<BitButton OnClick=EncodeData>EncodeData</BitButton>

<div>Encoded data is: @btoaText</div>

@code {
    private string? btoaText;
    private string? btoaValue;

    private async Task EncodeData() => btoaText = await window.Btoa(btoaValue!);
}";
    private string atobExampleCode =
    @"@inject Bit.Butil.Window window

<BitTextField @bind-Value=""atobValue"" />

<BitButton OnClick=DecodeData>DecodeData</BitButton>

<div>Decoded data is: @atobText</div>

@code {
    private string? atobText;
    private string? atobValue;

    private async Task DecodeData() => atobText = await window.Atob(atobValue!);
}";
    private string alertExampleCode =
    @"@inject Bit.Butil.Window window

<BitButton OnClick=""@(() => window.Alert(""Alert from C#""))"">ShowAlert</BitButton>";
    private string confirmExampleCode =
    @"@inject Bit.Butil.Window window

<BitButton OnClick=""@(() => window.Confirm(""Confirm from C#""))"">ShowConfirm</BitButton>";
}
