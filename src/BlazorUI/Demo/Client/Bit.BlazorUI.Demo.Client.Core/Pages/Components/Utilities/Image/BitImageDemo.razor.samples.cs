namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Image;

public partial class BitImageDemo
{
    private readonly string example1RazorCode = @"
<BitImage Alt=""The bit platform logo""
          Title=""The bit platform logo""
          Src=""images/bit-logo-blue.png"" />

<div>Disabled</div>
<BitImage Alt=""The bit platform logo"" IsEnabled=""false"" Src=""images/bit-logo-blue.png"" />";

    private readonly string example2RazorCode = @"
<style>
    .framed {
        background-color: #00ffff17;
    }

    .narrow-column {
        width: 12rem;
        border: 1px dashed gray;
    }
</style>

<div>Width=""9rem""</div>
<BitImage Width=""9rem"" Class=""framed"" Alt=""The bit platform logo"" Src=""images/bit-logo-blue.png"" />

<div>Height=""80""</div>
<BitImage Height=""80"" Class=""framed"" Alt=""The bit platform logo"" Src=""images/bit-logo-blue.png"" />

<div>Width=""256px"" Height=""128px""</div>
<BitImage Width=""256px"" Height=""128px"" Class=""framed"" Alt=""The bit platform logo"" Src=""images/bit-logo-blue.png"" />

<div>Width=""16rem"" AspectRatio=""1""</div>
<BitImage Width=""16rem"" AspectRatio=""1"" Class=""framed"" Alt=""A landscape photograph"" Src=""images/carousel/img1.jpg"" />

<div>Fluid, in a 12rem column</div>
<div class=""narrow-column"">
    <BitImage Fluid Alt=""A landscape photograph, scaled down to its column"" Src=""images/carousel/img1.jpg"" />
</div>";

    private readonly string example3RazorCode = @"
<style>
    .framed {
        background-color: #00ffff17;
    }
</style>

@foreach (var fit in Enum.GetValues<BitImageFit>())
{
    <div>
        <div>@fit</div>
        <BitImage Width=""160"" Height=""96"" ImageFit=""fit"" Class=""framed"" Alt=""The bit platform logo"" Src=""images/bit-logo-blue.png"" />
    </div>
}

<div>CenterCover, Cover=""Landscape""</div>
<BitImage Height=""96"" ImageFit=""BitImageFit.CenterCover"" Cover=""BitImageCover.Landscape"" Class=""framed"" Alt=""The bit platform logo"" Src=""images/bit-logo-blue.png"" />

<div>CenterCover, Cover=""Portrait""</div>
<BitImage Width=""96"" Height=""144"" ImageFit=""BitImageFit.CenterCover"" Cover=""BitImageCover.Portrait"" Class=""framed"" Alt=""The bit platform logo"" Src=""images/bit-logo-blue.png"" />";

    private readonly string example4RazorCode = @"
@foreach (var position in new[] { ""top"", ""center"", ""bottom"" })
{
    <div>
        <div>@position</div>
        <BitImage Width=""10rem""
                  AspectRatio=""1""
                  ImagePosition=""@position""
                  ImageFit=""BitImageFit.Cover""
                  Alt=""@($""A landscape photograph cropped to its {position}"")""
                  Src=""images/carousel/img2.jpg"" />
    </div>
}";

    private readonly string example5RazorCode = @"
<style>
    .max-frame-host {
        width: 14rem;
        height: 9rem;
        border-radius: 0.25rem;
        border: 1px solid gray;
    }
</style>

<div class=""max-frame-host"">
    <BitImage MaximizeFrame Alt=""A landscape photograph"" Src=""images/carousel/img3.jpg"" />
</div>

<div class=""max-frame-host"">
    <BitImage MaximizeFrame ImageFit=""BitImageFit.Contain"" Alt=""A landscape photograph"" Src=""images/carousel/img3.jpg"" />
</div>";

    private readonly string example6RazorCode = @"
<BitImage Rounded Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Cover"" Alt=""Rounded"" Src=""images/carousel/img4.jpg"" />
<BitImage Circular Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Cover"" Alt=""Circular"" Src=""images/carousel/img4.jpg"" />
<BitImage Bordered Rounded Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Cover"" Alt=""Bordered and rounded"" Src=""images/carousel/img4.jpg"" />
<BitImage Shadow Rounded Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Cover"" Alt=""Raised and rounded"" Src=""images/carousel/img4.jpg"" />";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => loadSlow = true"">Load a slow image</BitButton>
<BitButton OnClick=""() => loadBroken = true"">Load a broken image</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ReloadImages"">Reload both</BitButton>
<div>State: <b>@slowImageState</b></div>

@if (loadSlow)
{
    <BitImage @ref=""slowImage""
              Width=""200px""
              Alt=""An image served with a delay""
              Src=""/api/Image/GetImage""
              OnLoadingStateChange=""s => slowImageState = s"">
        <LoadingTemplate>
            <div style=""display:flex;align-items:center;gap:0.5rem"">
                <BitSpinnerLoading CustomSize=""24"" />
                <span>loading...</span>
            </div>
        </LoadingTemplate>
    </BitImage>
}

@if (loadBroken)
{
    <BitImage @ref=""brokenImage""
              Width=""200px""
              Alt=""An image whose source fails""
              Src=""/api/Image/GetImageError"">
        <LoadingTemplate><span>loading...</span></LoadingTemplate>
        <ErrorTemplate>
            <BitMessage Color=""BitColor.Error"">The image could not be loaded.</BitMessage>
        </ErrorTemplate>
    </BitImage>
}";
    private readonly string example7CsharpCode = @"
private bool loadSlow;
private bool loadBroken;
private BitImageState slowImageState;
private BitImage? slowImage;
private BitImage? brokenImage;

private async Task ReloadImages()
{
    if (slowImage is not null)
    {
        await slowImage.ReloadAsync();
    }

    if (brokenImage is not null)
    {
        await brokenImage.ReloadAsync();
    }
}";

    private readonly string example8RazorCode = @"
<style>
    .avatar-initials {
        width: 9rem;
        height: 9rem;
        display: flex;
        font-size: 2rem;
        border-radius: 50%;
        align-items: center;
        justify-content: center;
        color: var(--bit-clr-pri-text);
        background-color: var(--bit-clr-pri);
    }
</style>

<div>A broken Src</div>
<BitImage Width=""9rem""
          Alt=""The bit platform logo""
          Src=""images/no-such-image.png""
          FallbackSrc=""images/bit-logo-blue.png"" />

<div>No Src</div>
<BitImage Width=""9rem""
          Alt=""The bit platform logo""
          FallbackSrc=""images/bit-logo-blue.png"" />

<div>No source at all</div>
<BitImage Width=""9rem"" Alt=""A user's avatar"" Src=""@missingAvatarUrl"">
    <ErrorTemplate>
        <div class=""avatar-initials"">JD</div>
    </ErrorTemplate>
</BitImage>";
    private readonly string example8CsharpCode = @"
private readonly string? missingAvatarUrl = null;";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""() => progressiveKey++"">Load again</BitButton>

<div>FadeIn</div>
<BitImage @key=""@($""fade-{progressiveKey}"")""
          FadeIn
          Width=""200px""
          Alt=""An image served with a delay""
          Src=""@($""/api/Image/GetImage?v={progressiveKey}"")"" />

<div>StartVisible</div>
<BitImage @key=""@($""start-{progressiveKey}"")""
          StartVisible
          Width=""200px""
          Alt=""An image served with a delay""
          Src=""@($""/api/Image/GetImage?v={progressiveKey}"")"" />

<div>PlaceholderSrc + FadeIn</div>
<BitImage @key=""@($""placeholder-{progressiveKey}"")""
          FadeIn
          Rounded
          Width=""16rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""An image served with a delay""
          Src=""@($""/api/Image/GetImage?v={progressiveKey}"")""
          PlaceholderSrc=""@placeholderDataUri"" />";
    private readonly string example9CsharpCode = @"
private int progressiveKey;

// A tiny stand-in for the real picture, normally generated at build time.
private const string placeholderDataUri = ""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 9'%3E%3Cdefs%3E%3ClinearGradient id='g' x1='0' y1='0' x2='1' y2='1'%3E%3Cstop offset='0' stop-color='%23335c81'/%3E%3Cstop offset='1' stop-color='%23c9d6df'/%3E%3C/linearGradient%3E%3C/defs%3E%3Crect width='16' height='9' fill='url(%23g)'/%3E%3C/svg%3E"";";

    private readonly string example10RazorCode = @"
<BitImage Width=""12rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Loading=""BitImageLoading.Lazy""
          Decoding=""BitImageDecoding.Async""
          FetchPriority=""BitImageFetchPriority.Low""
          Alt=""A landscape photograph, loaded lazily""
          Src=""images/carousel/img1.jpg"" />

<BitImage Width=""12rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          FetchPriority=""BitImageFetchPriority.High""
          ReferrerPolicy=""BitImageReferrerPolicy.NoReferrer""
          ImageAttributes=""@(new() { { ""elementtiming"", ""hero"" } })""
          Alt=""A landscape photograph, fetched first""
          Src=""images/carousel/img2.jpg"" />";

    private readonly string example11RazorCode = @"
<div>Srcset & Sizes</div>
<BitImage Rounded
          Width=""100%""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph served at the size the viewport needs""
          Sizes=""(max-width: 600px) 100vw, 32rem""
          Srcset=""images/carousel/img1.jpg 1200w, images/carousel/img2.jpg 600w""
          Src=""images/carousel/img1.jpg"" />

<div>Sources: resize the window across 600px to swap the crop</div>
<BitImage Rounded
          Width=""100%""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph, cropped differently on a narrow viewport""
          Src=""images/carousel/img1.jpg""
          Sources=""@(new BitImageSource[]
                     {
                         new() { Media = ""(max-width: 600px)"", Srcset = ""images/carousel/img4.jpg"" },
                         new() { Srcset = ""images/carousel/img1.jpg"" }
                     })"" />";

    private readonly string example12RazorCode = @"
<BitImage Rounded
          Width=""8rem""
          AspectRatio=""1""
          Draggable=""false""
          ImageFit=""BitImageFit.Cover""
          Alt=""Preview the photograph""
          OnClick=""() => isPreviewOpen = true""
          Src=""images/carousel/img3.jpg"" />

<BitImage Rounded
          Width=""8rem""
          AspectRatio=""1""
          IsEnabled=""false""
          Draggable=""false""
          ImageFit=""BitImageFit.Cover""
          Alt=""Preview the photograph (disabled)""
          OnClick=""() => isPreviewOpen = true""
          Src=""images/carousel/img3.jpg"" />

<BitModal @bind-IsOpen=""isPreviewOpen"" HeaderText=""A landscape photograph"" ShowCloseButton>
    <div style=""padding:1rem;max-width:min(48rem, 90vw)"">
        <BitImage Fluid Alt=""A landscape photograph"" Src=""images/carousel/img3.jpg"" />
    </div>
</BitModal>";
    private readonly string example12CsharpCode = @"
private bool isPreviewOpen;";

    private readonly string example13RazorCode = @"
<style>
    .image-caption {
        left: 0;
        right: 0;
        bottom: 0;
        color: white;
        padding: 0.75rem;
        position: absolute;
        box-sizing: border-box;
        background: linear-gradient(transparent, rgba(0, 0, 0, 0.65));
    }
</style>

<BitImage Rounded
          Width=""20rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph""
          Src=""images/carousel/img2.jpg"">
    <div class=""image-caption"">A caption laid over the image</div>
</BitImage>";

    private readonly string example14RazorCode = @"
<div>Decorative</div>
<BitImage Width=""6rem"" Src=""images/bit-logo-blue.png"" />

<div>Failed, still announced</div>
<BitImage Width=""6rem"" Alt=""Monthly sales chart"" Src=""images/no-such-chart.png"" />

<div>AriaLabel</div>
<BitImage @ref=""focusableImage""
          Rounded
          Width=""8rem""
          AspectRatio=""1""
          ImageFit=""BitImageFit.Cover""
          Alt=""A mountain lake at dawn""
          AriaLabel=""Open the photo gallery""
          OnClick=""() => galleryOpened++""
          Src=""images/carousel/img4.jpg"" />

<BitButton OnClick=""() => focusableImage!.FocusAsync()"">Focus the image</BitButton>
<div>Gallery opened <b>@galleryOpened</b> times</div>";
    private readonly string example14CsharpCode = @"
private BitImage? focusableImage;
private int galleryOpened;";

    private readonly string example15RazorCode = @"
<div style=""--bit-Image-radius: 1.5rem;
            --bit-Image-border-width: 3px;
            --bit-Image-border-color: var(--bit-clr-pri);
            --bit-Image-shadow: 0 8px 24px rgba(0, 0, 0, 0.35);
            --bit-Image-hover-shadow: 0 16px 40px rgba(0, 0, 0, 0.45);
            --bit-Image-hover-overlay: color-mix(in srgb, var(--bit-clr-pri) 25%, transparent);
            --bit-Image-active-overlay: color-mix(in srgb, var(--bit-clr-pri) 40%, transparent);
            --bit-Image-background: var(--bit-clr-bg-sec);
            --bit-Image-focus-color: var(--bit-clr-wrn);"">
    <BitImage Rounded Bordered Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Cover"" Alt=""Rounded and bordered"" Src=""images/carousel/img1.jpg"" />
    <BitImage Rounded Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Contain"" Alt=""The bit platform logo on the frame background"" Src=""images/bit-logo-blue.png"" />
    <BitImage Rounded Shadow Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Cover"" Alt=""Point here to see the deeper lift"" OnClick=""() => { }"" Src=""images/carousel/img2.jpg"" />
    <BitImage Rounded Width=""8rem"" AspectRatio=""1"" ImageFit=""BitImageFit.Cover"" Alt=""Tab here to see the amber focus ring"" OnClick=""() => { }"" Src=""images/carousel/img3.jpg"" />
</div>";

    private readonly string example16RazorCode = @"
<BitParams Parameters=""imageParams"">
    <BitImage Alt=""A landscape photograph"" Src=""images/carousel/img1.jpg"" />
    <BitImage Alt=""A missing photograph, replaced by the cascaded fallback"" Src=""images/no-such-image.jpg"" />
    <BitImage Rounded=""false"" Alt=""A landscape photograph"" Src=""images/carousel/img3.jpg"" />
</BitParams>";
    private readonly string example16CsharpCode = @"
private readonly BitImageParams[] imageParams =
[
    new()
    {
        Width = ""8rem"",
        AspectRatio = ""1"",
        ImageFit = BitImageFit.Cover,
        Rounded = true,
        Shadow = true,
        FadeIn = true,
        FallbackSrc = ""images/bit-logo-blue.png"",
    }
];";

    private readonly string example17RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        filter: hue-rotate(45deg);
        background-color: blueviolet;
    }

    .custom-image {
        width: 16rem;
        filter: opacity(25%);
        border-radius: 1rem 3rem;
    }
</style>

<div>Style</div>
<BitImage Alt=""The bit platform logo""
          Style=""border: 2px solid goldenrod; border-radius: 5px; width: 258px;""
          Src=""images/bit-logo-blue.png"" />

<div>Class</div>
<BitImage Alt=""The bit platform logo""
          Class=""custom-class""
          Src=""images/bit-logo-blue.png"" />

<div>Styles</div>
<BitImage Alt=""The bit platform logo""
          Styles=""@(new() { Image = ""filter: blur(5px)"" })""
          Src=""images/bit-logo-blue.png"" />

<div>Classes</div>
<BitImage Alt=""The bit platform logo""
          Classes=""@(new() { Image = ""custom-image"" })""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example18RazorCode = @"
<style>
    .image-caption {
        left: 0;
        right: 0;
        bottom: 0;
        color: white;
        padding: 0.75rem;
        position: absolute;
        box-sizing: border-box;
        background: linear-gradient(transparent, rgba(0, 0, 0, 0.65));
    }
</style>

<div dir=""rtl"">
    <BitImage Rounded
              Dir=""BitDir.Rtl""
              Width=""20rem""
              AspectRatio=""16/9""
              ImageFit=""BitImageFit.Cover""
              Alt=""عکسی از یک منظره""
              Src=""images/carousel/img4.jpg"">
        <div class=""image-caption"">نوشته‌ای روی تصویر</div>
    </BitImage>
</div>";
}
