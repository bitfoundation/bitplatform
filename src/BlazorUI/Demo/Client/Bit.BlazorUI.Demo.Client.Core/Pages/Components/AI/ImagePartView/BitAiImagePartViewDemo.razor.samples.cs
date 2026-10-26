namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ImagePartView;

public partial class BitAiImagePartViewDemo
{
    private readonly string example1RazorCode = @"
<BitAiImagePartView Part=""auroraImage"" />";
    private readonly string example1CsharpCode = @"
private readonly BitAiImagePart auroraImage = new(""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"")
{
    Alt = ""Green northern lights over a snowy ridge at night"",
    Width = 1000,
    Height = 350,
};";

    private readonly string example2RazorCode = @"
<BitAiImagePartView Part=""mountainImage"" />";
    private readonly string example2CsharpCode = @"
private readonly BitAiImagePart mountainImage = new(""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg"")
{
    Alt = ""A range of snow-covered peaks under a blue sky"",
    Caption = ""Generated for the prompt \""a winter landscape in the Alps\""."",
    Width = 1000,
    Height = 350,
};";

    private readonly string example3RazorCode = @"
<BitAiImagePartView Part=""decorativeImage"" />";
    private readonly string example3CsharpCode = @"
private readonly BitAiImagePart decorativeImage = new(""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"");";

    private readonly string example4RazorCode = @"
<BitAiImagePartView Part=""chartImage"" />";
    private readonly string example4CsharpCode = @"
private readonly BitAiImagePart chartImage = new(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='320' height='120' viewBox='0 0 320 120'%3E%3Crect width='320' height='120' rx='12' fill='%232563eb'/%3E%3Crect x='30' y='70' width='40' height='30' fill='%23bfdbfe'/%3E%3Crect x='90' y='45' width='40' height='55' fill='%23bfdbfe'/%3E%3Crect x='150' y='30' width='40' height='70' fill='%23bfdbfe'/%3E%3Crect x='210' y='55' width='40' height='45' fill='%23bfdbfe'/%3E%3Crect x='270' y='20' width='20' height='80' fill='%23ffffff'/%3E%3C/svg%3E"")
{
    Alt = ""A bar chart of five values, the last one the highest"",
    Caption = ""Drawn by a tool and returned inline."",
    Width = 320,
    Height = 120,
};";

    private readonly string example5RazorCode = @"
<BitAiOptionsProvider UiOptions=""clickToLoadOptions"">
    <BitAiImagePartView Part=""policyImage"" />
</BitAiOptionsProvider>

<BitAiOptionsProvider UiOptions=""blockOptions"">
    <BitAiImagePartView Part=""policyImage"" />
</BitAiOptionsProvider>";
    private readonly string example5CsharpCode = @"
private readonly BitAiImagePart policyImage = new(""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg"")
{
    Alt = ""A range of snow-covered peaks under a blue sky"",
    Caption = ""Shown only as the image policy allows."",
    Width = 1000,
    Height = 350,
};

// An app sets the policy once, for every view (AddBitBlazorUIAIServices(ai => ai.ConfigureUi(...)) or a provider
// around the whole chat); here each provider sets its own.
private readonly BitAiUiOptions clickToLoadOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
private readonly BitAiUiOptions blockOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block };";

    private readonly string example6RazorCode = @"
<style>
    .custom-class {
        max-width: 18rem;
        padding: 0.5rem;
        border-radius: 1rem;
        background-color: #1e293b;
        --bit-AiImagePartView-caption-color: #cbd5e1;
    }

    .custom-root {
        max-width: 18rem;
    }

    .custom-image {
        border-radius: 50% / 20%;
    }

    .custom-caption {
        color: tomato;
        font-style: italic;
    }
</style>

<BitAiImagePartView Part=""styledImage"" Style=""max-width: 18rem; padding: 0.5rem; border: 1px dashed dodgerblue;"" />

<BitAiImagePartView Part=""styledImage"" Class=""custom-class"" />

<BitAiImagePartView Part=""styledImage""
                    Styles=""@(new() { Root = ""max-width: 18rem;"",
                                      Image = ""filter: grayscale(1);"",
                                      Caption = ""text-align: center; font-weight: bold;"" })"" />

<BitAiImagePartView Part=""styledImage""
                    Classes=""@(new() { Root = ""custom-root"", Image = ""custom-image"", Caption = ""custom-caption"" })"" />

<div style=""max-width: 18rem; --bit-AiImagePartView-radius: 2rem; --bit-AiImagePartView-caption-color: mediumseagreen; --bit-AiImagePartView-caption-font-size: 1rem;"">
    <BitAiImagePartView Part=""styledImage"" />
</div>";
    private readonly string example6CsharpCode = @"
private readonly BitAiImagePart styledImage = new(""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"")
{
    Alt = ""A winding road above a lake between hills, snowy mountains behind"",
    Caption = ""A caption"",
    Width = 1000,
    Height = 350,
};";

    private readonly string example7RazorCode = @"
<BitAiImagePartView Dir=""BitDir.Rtl"" Part=""rtlImage"" />

<div dir=""rtl"">
    <BitAiImagePartView Part=""rtlImage"" />
</div>";
    private readonly string example7CsharpCode = @"
private readonly BitAiImagePart rtlImage = new(""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"")
{
    Alt = ""شفق قطبی سبز بر فراز تپه‌ای برفی در شب"",
    Caption = ""تصویری که برای درخواست «شفق قطبی» ساخته شد."",
    Width = 1000,
    Height = 350,
};";
}
