namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Embed;

public partial class BitEmbedDemo
{
    private readonly string example1RazorCode = @"
<BitEmbed Src=""https://www.youtube-nocookie.com/embed/79ssLqSInxc""
          Title=""bit Boilerplate features overview"" />";

    private readonly string example2RazorCode = @"
<BitEmbed Src=""https://www.youtube-nocookie.com/embed/79ssLqSInxc""
          Title=""bit Boilerplate features overview""
          MaxWidth=""560px"" />

<BitEmbed Src=""https://www.openstreetmap.org/export/embed.html?bbox=-0.1357%2C51.4985%2C-0.1157%2C51.5085""
          Title=""A map of central London""
          AspectRatio=""4 / 3""
          MaxWidth=""400px"" />";

    private readonly string example3RazorCode = @"
<BitEmbed Src=""https://www.youtube-nocookie.com/embed/79ssLqSInxc""
          Title=""bit Boilerplate features overview""
          Allow=""accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share""
          AllowFullScreen
          Loading=""BitImageLoading.Eager""
          ReferrerPolicy=""BitImageReferrerPolicy.StrictOriginWhenCrossOrigin""
          MaxWidth=""560px"" />

<BitEmbed Src=""https://www.openstreetmap.org/export/embed.html?bbox=-0.1357%2C51.4985%2C-0.1157%2C51.5085""
          Title=""A map of central London""
          Sandbox=""allow-scripts allow-same-origin""
          AspectRatio=""4 / 3""
          MaxWidth=""400px"" />";

    private readonly string example4RazorCode = @"
<style>
    .thumbnail {
        width: 100%;
        height: 100%;
        display: block;
        object-fit: cover;
    }

    .play-icon {
        width: 4rem;
        height: 4rem;
        display: flex;
        color: white;
        position: absolute;
        border-radius: 50%;
        align-items: center;
        justify-content: center;
        background-color: rgba(0, 0, 0, 0.7);
    }
</style>

<BitEmbed Src=""https://www.youtube-nocookie.com/embed/79ssLqSInxc?autoplay=1""
          Title=""bit Boilerplate features overview""
          Allow=""autoplay; encrypted-media; picture-in-picture""
          AllowFullScreen
          MaxWidth=""560px""
          PlaceholderAriaLabel=""Play the bit Boilerplate features overview"">
    <PlaceholderTemplate>
        <img class=""thumbnail"" src=""https://i.ytimg.com/vi/79ssLqSInxc/hqdefault.jpg"" alt="""" />
        <span class=""play-icon"" aria-hidden=""true"">
            <svg width=""28"" height=""28"" viewBox=""0 0 24 24""><path d=""M8 5v14l11-7z"" fill=""currentColor"" /></svg>
        </span>
    </PlaceholderTemplate>
</BitEmbed>";

    private readonly string example5RazorCode = @"
<style>
    .custom-root {
        outline-offset: 0.25rem;
        border-radius: 0.5rem;
        outline: 2px dashed var(--bit-clr-pri);
    }
</style>

<BitEmbed Src=""https://www.youtube-nocookie.com/embed/79ssLqSInxc""
          Title=""bit Boilerplate features overview""
          MaxWidth=""400px""
          Style=""--bit-Embed-radius: 1rem;"" />

<BitEmbed Src=""https://www.youtube-nocookie.com/embed/79ssLqSInxc""
          Title=""bit Boilerplate features overview""
          MaxWidth=""400px""
          Classes=""@(new() { Root = ""custom-root"" })"" />

<BitEmbed Src=""https://www.openstreetmap.org/export/embed.html?bbox=-0.1357%2C51.4985%2C-0.1157%2C51.5085""
          Title=""A map of central London""
          AspectRatio=""4 / 3""
          MaxWidth=""400px""
          Styles=""@(new() { Root = ""border: 2px solid var(--bit-clr-pri);"", Frame = ""filter: grayscale(1);"" })"" />";

    private readonly string example6RazorCode = @"
<style>
    .thumbnail {
        width: 100%;
        height: 100%;
        display: block;
        object-fit: cover;
    }

    .play-icon {
        width: 4rem;
        height: 4rem;
        display: flex;
        color: white;
        position: absolute;
        border-radius: 50%;
        align-items: center;
        justify-content: center;
        background-color: rgba(0, 0, 0, 0.7);
    }

    .caption {
        color: white;
        bottom: 0.75rem;
        position: absolute;
        border-radius: 0.25rem;
        inset-inline-start: 0.75rem;
        padding: 0.25rem 0.5rem;
        background-color: rgba(0, 0, 0, 0.7);
    }
</style>

<div dir=""rtl"">
    <BitEmbed Dir=""BitDir.Rtl""
              Src=""https://www.youtube-nocookie.com/embed/79ssLqSInxc?autoplay=1""
              Title=""معرفی امکانات bit Boilerplate""
              Allow=""autoplay; encrypted-media; picture-in-picture""
              AllowFullScreen
              MaxWidth=""560px""
              PlaceholderAriaLabel=""پخش ویدیوی معرفی امکانات bit Boilerplate"">
        <PlaceholderTemplate>
            <img class=""thumbnail"" src=""https://i.ytimg.com/vi/79ssLqSInxc/hqdefault.jpg"" alt="""" />
            <span class=""play-icon"" aria-hidden=""true"">
                <svg width=""28"" height=""28"" viewBox=""0 0 24 24""><path d=""M8 5v14l11-7z"" fill=""currentColor"" /></svg>
            </span>
            <span class=""caption"">معرفی امکانات bit Boilerplate</span>
        </PlaceholderTemplate>
    </BitEmbed>
</div>";
}
