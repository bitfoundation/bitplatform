namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TextRotator;

public partial class BitTextRotatorDemo
{
    private readonly string example1RazorCode = @"
<div>Build apps for <BitTextRotator Items=""@([""the web"", ""mobile"", ""desktop""])"" /> with one codebase.</div>

<div>Ship them as <BitTextRotator ForceAnimation Items=""@([""a PWA"", ""Android"", ""iOS"", ""Windows"", ""macOS""])"" /> apps, even with reduced motion.</div>";

    private readonly string example2RazorCode = @"
<div>A <BitTextRotator Interval=""1500"" Items=""@([""quick"", ""brisk"", ""rapid""])"" /> rotation of a second and a half.</div>

<div>A <BitTextRotator Interval=""5000"" Items=""@([""calm"", ""steady"", ""relaxed""])"" /> rotation of five seconds.</div>";

    private readonly string example3RazorCode = @"
<BitTextRotator Element=""h2"" Items=""@([""Fast."", ""Accessible."", ""Themeable.""])"" />

<BitText Typography=""BitTypography.H4"">
    Made for <BitTextRotator Items=""@([""developers"", ""designers"", ""teams""])"" />
</BitText>";

    private readonly string example4RazorCode = @"
<div style=""--bit-TextRotator-distance:-0.5em"">
    Items that come in from <BitTextRotator Items=""@([""above"", ""the top"", ""overhead""])"" />
</div>

<div style=""--bit-TextRotator-distance:0;--bit-TextRotator-duration:800ms"">
    Items that <BitTextRotator Items=""@([""cross-fade"", ""dissolve"", ""blend""])"" /> in place
</div>

<div style=""--bit-TextRotator-distance:1.25em;--bit-TextRotator-easing:cubic-bezier(0, 1, 0, 1)"">
    Items that <BitTextRotator Items=""@([""snap"", ""spring"", ""jump""])"" /> into place
</div>";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""isPaused"" Text=""Paused"" />
<div>Pause me on <BitTextRotator Paused=""isPaused"" Items=""@([""this word"", ""that word"", ""any word""])"" /></div>

<div>A disabled rotator: <BitTextRotator Disabled Items=""@([""held"", ""still""])"" /></div>";
    private readonly string example5CsharpCode = @"
private bool isPaused;";

    private readonly string example6RazorCode = @"
<style>
    .terminal {
        display: block;
        padding: 1rem;
        border-radius: 0.5rem;
        color: #e2e8f0;
        background-color: #0f172a;
    }
</style>

<code class=""terminal"">
    bit new MyApp
    --database <BitTextRotator Interval=""3000"" Items=""@([""sqlite"", ""sqlserver"", ""postgresql""])"" />
    --api <BitTextRotator Interval=""6000"" Items=""@([""integrated"", ""standalone""])"" />
    --module <BitTextRotator Interval=""5000"" Items=""@([""admin"", ""sales""])"" />
</code>";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""pauseAll"" Text=""Pause animations"" />

<BitParams Parameters=""rotatorParams"">
    <div>Fast at <BitTextRotator Items=""@([""one"", ""two"", ""three""])"" /></div>
    <div>Fast at <BitTextRotator Items=""@([""red"", ""green"", ""blue""])"" /></div>
    <div>Slow at <BitTextRotator Interval=""4000"" Items=""@([""its own"", ""Interval""])"" /></div>
</BitParams>";
    private readonly string example7CsharpCode = @"
private bool pauseAll;
private BitTextRotatorParams[] rotatorParams => [new() { Interval = 1500, Paused = pauseAll }];";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        font-size: 1.5rem;
        font-style: italic;
    }
</style>

<BitTextRotator Style=""font-size:2rem;font-weight:bold"" Items=""@([""Styled"", ""Bold"", ""Big""])"" />

<BitTextRotator Class=""custom-class"" Items=""@([""Classy"", ""Italic"", ""Large""])"" />

<BitTextRotator Styles=""@(new() { CurrentItem = ""color:var(--bit-clr-pri)"" })"" Items=""@([""Primary"", ""colored"", ""items""])"" />

<div style=""--bit-TextRotator-duration:1s;--bit-TextRotator-distance:1em"">
    <BitTextRotator Items=""@([""Restyled by"", ""the variables of"", ""its container""])"" />
</div>";

    private readonly string example9RazorCode = @"
<div dir=""rtl"">
    ساخت برنامه برای <BitTextRotator Dir=""BitDir.Rtl"" Items=""@([""وب"", ""موبایل"", ""دسکتاپ""])"" /> با یک کد
</div>";
}
