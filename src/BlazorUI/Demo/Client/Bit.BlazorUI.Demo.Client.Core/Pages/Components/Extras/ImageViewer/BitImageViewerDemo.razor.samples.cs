namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ImageViewer;

public partial class BitImageViewerDemo
{
    private readonly string example1RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""A granite cliff above a misty autumn forest"" />";

    private readonly string example2RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""A mountain road above a lake between hills"" @bind-Zoom=""boundZoom"" />

<BitText>Zoom: <b>@boundZoom.ToString(""0.##"")</b></BitText>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => boundZoom = 3"">Zoom to 3</BitButton>";
    private readonly string example2CsharpCode = @"
private double boundZoom = 1;";

    private readonly string example3RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg""
                Alt=""A granite cliff above a misty autumn forest""
                MinZoom=""0.5""
                MaxZoom=""4""
                ZoomFactor=""2""
                DoubleClickZoom=""4"" />";

    private readonly string example4RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""A mountain road above a lake between hills"" WheelZoom />";

    private readonly string example5RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg""
                Alt=""A mountain road above a lake between hills""
                ShowFullScreenButton
                @bind-FullScreen=""isFullScreen"" />

<BitText>FullScreen: <b>@isFullScreen</b></BitText>";
    private readonly string example5CsharpCode = @"
private bool isFullScreen;";

    private readonly string example6RazorCode = @"
<style>
    .lightbox-thumb {
        padding: 0;
        width: 8rem;
        cursor: zoom-in;
        overflow: hidden;
        border-radius: 0.5rem;
        border: 1px solid var(--bit-clr-brd-sec);
    }

    .lightbox-thumb img {
        width: 100%;
        display: block;
    }
</style>

@foreach (var photo in photos)
{
    <button type=""button"" class=""lightbox-thumb"" @onclick=""() => OpenLightbox(photo)"">
        <img src=""@photo.Src"" alt=""@photo.Alt"" />
    </button>
}

<BitImageViewer Src=""@lightboxPhoto?.Src""
                Alt=""@lightboxPhoto?.Alt""
                FullScreenOnly
                @bind-FullScreen=""isLightboxOpen"" />";
    private readonly string example6CsharpCode = @"
private bool isLightboxOpen;
private Photo? lightboxPhoto;

private static readonly Photo[] photos =
[
    new(""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"", ""Northern lights over snowy hills""),
    new(""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg"", ""Snow-capped mountains under a blue sky""),
    new(""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"", ""A granite cliff above a misty autumn forest""),
    new(""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"", ""A mountain road above a lake between hills""),
];

private void OpenLightbox(Photo photo)
{
    lightboxPhoto = photo;
    isLightboxOpen = true;
}

private record Photo(string Src, string Alt);";

    private readonly string example7RazorCode = @"
<BitImageViewer @ref=""viewerRef"" Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""A granite cliff above a misty autumn forest"" NoToolbar />

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => viewerRef.ZoomIn()"">ZoomIn</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => viewerRef.ZoomOut()"">ZoomOut</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => viewerRef.Reset()"">Reset</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => viewerRef.EnterFullScreen()"">EnterFullScreen</BitButton>";
    private readonly string example7CsharpCode = @"
private BitImageViewer viewerRef = default!;";

    private readonly string example8RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""A mountain road above a lake between hills"" Height=""10rem"" />";

    private readonly string example9RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""A granite cliff above a misty autumn forest"" Disabled />";

    private readonly string example10RazorCode = @"
<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" Alt=""Una carretera de montaña sobre un lago entre colinas"" Texts=""spanishTexts"" />";
    private readonly string example10CsharpCode = @"
private static readonly BitImageViewerTexts spanishTexts = new()
{
    RoleDescription = ""visor de imágenes"",
    Instructions = ""Más y menos amplían, las flechas mueven la imagen ampliada y 0 la ajusta de nuevo."",
    ToolbarAriaLabel = ""Zoom"",
    ZoomIn = ""Acercar"",
    ZoomOut = ""Alejar"",
    Reset = ""Ajustar a la vista"",
};";

    private readonly string example11RazorCode = @"
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
                Alt=""A granite cliff above a misty autumn forest""
                Style=""--bit-ImageViewer-background: #000; --bit-ImageViewer-radius: 1rem;""
                Styles=""@(new() { Toolbar = ""justify-content: flex-end;"" })"" />

<BitImageViewer Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg""
                Alt=""A mountain road above a lake between hills""
                Classes=""@(new() { Viewport = ""custom-viewport"", Button = ""custom-button"" })"" />";

    private readonly string example12RazorCode = @"
<div dir=""rtl"">
    <BitImageViewer Dir=""BitDir.Rtl"" Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" Alt=""صخره‌ای گرانیتی بالای جنگلی مه‌آلود در پاییز"" Texts=""persianTexts"" />
</div>";
    private readonly string example12CsharpCode = @"
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
