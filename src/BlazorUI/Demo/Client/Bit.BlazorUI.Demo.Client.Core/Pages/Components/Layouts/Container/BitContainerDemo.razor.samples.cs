namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Container;

public partial class BitContainerDemo
{
    private readonly string example1RazorCode = @"
<style>
    .band {
        background: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-sec);
    }

    .container {
        background: color-mix(in srgb, var(--bit-clr-pri) 16%, transparent);
    }

    .content {
        padding: 0.5rem;
        text-align: center;
        color: var(--bit-clr-pri-text);
        background: var(--bit-clr-pri);
    }
</style>


<div class=""band"">
    <BitContainer Class=""container"">
        <div class=""content"">The content of the page</div>
    </BitContainer>
</div>";

    private readonly string example2RazorCode = @"
<div class=""band"">
    <BitContainer MaxWidth=""480px"" Class=""container"">
        <div class=""content"">480px</div>
    </BitContainer>
</div>

<div class=""band"">
    <BitContainer MaxWidth=""40rem"" Class=""container"">
        <div class=""content"">40rem</div>
    </BitContainer>
</div>

<div class=""band"">
    <BitContainer MaxWidth=""none"" Class=""container"">
        <div class=""content"">none</div>
    </BitContainer>
</div>";

    private readonly string example3RazorCode = @"
<div class=""band"">
    <BitContainer MaxWidth=""480px"" NoGutter Class=""container"">
        <div class=""content"">No gutter</div>
    </BitContainer>
</div>";

    private readonly string example4RazorCode = @"
<style>
    .hero {
        padding-block: 1.5rem;
        color: var(--bit-clr-pri-text);
        background: var(--bit-clr-pri);
    }

    .hero-title {
        font-size: 1.25rem;
        font-weight: 600;
    }

    .section {
        padding-block: 1rem;
        border-inline: 1px dashed var(--bit-clr-brd-sec);
    }
</style>


<div class=""hero"">
    <BitContainer MaxWidth=""560px"">
        <div class=""hero-title"">A full-width hero</div>
        <div>Its text lines up with the section below.</div>
    </BitContainer>
</div>
<BitContainer MaxWidth=""560px"" Class=""section"">
    The section under the hero, in a container of the same width.
</BitContainer>";

    private readonly string example5RazorCode = @"
<div class=""band"">
    <BitContainer Element=""section"" AriaLabel=""Latest news"" MaxWidth=""480px"" Class=""container"">
        <div class=""content"">A section landmark</div>
    </BitContainer>
</div>";

    private readonly string example6RazorCode = @"
<BitParams Parameters=""@containerParams"">
    <div class=""band"">
        <BitContainer Class=""container"">
            <div class=""content"">Cascaded 480px</div>
        </BitContainer>
    </div>

    <div class=""band"">
        <BitContainer MaxWidth=""320px"" Class=""container"">
            <div class=""content"">Its own 320px</div>
        </BitContainer>
    </div>
</BitParams>";
    private readonly string example6CsharpCode = @"
private readonly BitContainerParams[] containerParams =
[
    new() { MaxWidth = ""480px"" }
];";

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        outline: 2px dashed tomato;
        outline-offset: -2px;
    }
</style>


<div class=""band"">
    <BitContainer Style=""--bit-Container-max-width:400px;--bit-Container-padding-inline:3rem;--bit-Container-padding-block:1rem"" Class=""container"">
        <div class=""content"">Variables on Style</div>
    </BitContainer>
</div>

<div class=""band"">
    <BitContainer MaxWidth=""400px"" Class=""container custom-class"">
        <div class=""content"">Class</div>
    </BitContainer>
</div>";
}
