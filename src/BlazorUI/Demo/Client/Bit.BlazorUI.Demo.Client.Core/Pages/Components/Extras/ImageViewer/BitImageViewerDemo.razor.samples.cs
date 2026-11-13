namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ImageViewer;

public partial class BitImageViewerDemo
{
    private readonly string example1RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""A mountain lake at sunrise"" />";

    private readonly string example2RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""A forest path"" @bind-Zoom=""boundZoom"" />

<BitText>Zoom: <b>@boundZoom.ToString(""0.##"")</b></BitText>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => boundZoom = 3"">Zoom to 3</BitButton>";
    private readonly string example2CsharpCode = @"
private double boundZoom = 1;";

    private readonly string example3RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg""
                Alt=""A mountain lake at sunrise""
                MinZoom=""0.5""
                MaxZoom=""4""
                ZoomFactor=""2""
                DoubleClickZoom=""4"" />";

    private readonly string example4RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""A forest path"" WheelZoom />";

    private readonly string example5RazorCode = @"
<BitImageViewer @ref=""viewerRef"" Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""A mountain lake at sunrise"" NoToolbar />

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => viewerRef.ZoomIn()"">ZoomIn</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => viewerRef.ZoomOut()"">ZoomOut</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => viewerRef.Reset()"">Reset</BitButton>";
    private readonly string example5CsharpCode = @"
private BitImageViewer viewerRef = default!;";

    private readonly string example6RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""A forest path"" Height=""10rem"" />";

    private readonly string example7RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""A mountain lake at sunrise"" Disabled />";

    private readonly string example8RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""Un sendero en el bosque"" Texts=""spanishTexts"" />";
    private readonly string example8CsharpCode = @"
private static readonly BitImageViewerTexts spanishTexts = new()
{
    RoleDescription = ""visor de imágenes"",
    Instructions = ""Más y menos amplían, las flechas mueven la imagen ampliada y 0 la ajusta de nuevo."",
    ToolbarAriaLabel = ""Zoom"",
    ZoomIn = ""Acercar"",
    ZoomOut = ""Alejar"",
    Reset = ""Ajustar a la vista"",
};";

    private readonly string example9RazorCode = @"
<style>
    .custom-viewport {
        border: 2px dashed var(--bit-clr-brd-pri);
    }

    .custom-button {
        border-radius: 50%;
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg""
                Alt=""A mountain lake at sunrise""
                Style=""--bit-ImageViewer-background: #000; --bit-ImageViewer-radius: 1rem;""
                Styles=""@(new() { Toolbar = ""justify-content: flex-end;"" })"" />

<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg""
                Alt=""A forest path""
                Classes=""@(new() { Viewport = ""custom-viewport"", Button = ""custom-button"" })"" />";

    private readonly string example10RazorCode = @"
<div dir=""rtl"">
    <BitImageViewer Dir=""BitDir.Rtl"" Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""دریاچه‌ای کوهستانی در طلوع آفتاب"" Texts=""persianTexts"" />
</div>";
    private readonly string example10CsharpCode = @"
private static readonly BitImageViewerTexts persianTexts = new()
{
    RoleDescription = ""نمایشگر تصویر"",
    Instructions = ""مثبت و منفی بزرگ‌نمایی می‌کنند، کلیدهای جهت تصویر بزرگ‌شده را جابه‌جا می‌کنند و ۰ آن را دوباره جا می‌دهد."",
    ToolbarAriaLabel = ""بزرگ‌نمایی"",
    ZoomIn = ""بزرگ‌نمایی"",
    ZoomOut = ""کوچک‌نمایی"",
    Reset = ""جا دادن در قاب"",
};";
}
