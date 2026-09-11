namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TextShimmer;

public partial class BitTextShimmerDemo
{
    private readonly string example1RazorCode = @"
<BitTextShimmer Text=""Thinking about your question..."" />

<BitTextShimmer ForceAnimation Text=""This shimmer keeps animating even in reduced motion mode"" />";

    private readonly string example2RazorCode = @"
<BitTextShimmer Element=""h1"" Text=""A shimmering heading"" />

<div>An <BitTextShimmer Element=""span"" Text=""inline text shimmer"" /> in the middle of a sentence.</div>";

    private readonly string example3RazorCode = @"
<BitTextShimmer Spread=""5"" Text=""A wide shimmer band"" />

<BitTextShimmer Spread=""0.5"" Text=""A narrow shimmer band"" />

<BitTextShimmer SpreadLength=""2em"" Text=""A band two ems wide"" />

<BitTextShimmer SpreadLength=""2em"" Element=""h3"" Text=""A band two ems wide"" />";

    private readonly string example4RazorCode = @"
<BitTextShimmer ContentLength=""30"">
    Thinking <strong>really</strong> hard about it...
</BitTextShimmer>

<BitTextShimmer SpreadLength=""3em"">
    <BitIcon IconName=""@BitIconName.Robot"" /> Searching <em>the docs</em> for an answer...
</BitTextShimmer>

<BitTextShimmer SpreadLength=""3em"">
    <span style=""-webkit-text-fill-color:currentcolor"">✨</span> Generating a summary...
</BitTextShimmer>";

    private readonly string example5RazorCode = @"
<BitTextShimmer Duration=""4000"" Text=""Slow and calm shimmer (4 seconds)"" />

<BitTextShimmer Duration=""750"" Text=""Fast and urgent shimmer (750 milliseconds)"" />

<BitTextShimmer RepeatDelay=""2000"" Text=""A sweep, then an extra two seconds of rest"" />

<BitTextShimmer Delay=""0"" Text=""Reading the question..."" />
<BitTextShimmer Delay=""250"" Text=""Searching the knowledge base..."" />
<BitTextShimmer Delay=""500"" Text=""Drafting an answer..."" />";

    private readonly string example6RazorCode = @"
<BitTextShimmer Text=""Following the reading direction (default)"" />

<BitTextShimmer Reversed Text=""Against the reading direction"" />

<BitTextShimmer Alternate Text=""Back and forth"" />";

    private readonly string example7RazorCode = @"
<BitTextShimmer Angle=""25"" Element=""h2"" Text=""A band tilted by 25 degrees"" />

<BitTextShimmer Angle=""-25"" Element=""h2"" Text=""A band tilted by -25 degrees"" />";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => replayCount++"">Replay</BitButton>

<BitTextShimmer @key=""@($""once-{replayCount}"")"" Iterations=""1"" Element=""h2"" Text=""Revealed with a single sweep"" />

<BitTextShimmer @key=""@($""thrice-{replayCount}"")"" Iterations=""3"" Text=""Three sweeps, then at rest"" />";
    private readonly string example8CsharpCode = @"
private int replayCount;";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""isPaused"" Text=""Paused"" />
<BitTextShimmer Paused=""isPaused"" Text=""Pause me and resume me"" />

<BitTextShimmer PauseOnHover Text=""Hover over me to pause the sweep"" />

<BitToggle @bind-Value=""isStatic"" Text=""Static"" />
<BitTextShimmer Static=""isStatic"" Text=""Stop me and start me again"" />

<BitTextShimmer IsEnabled=""false"" Text=""A disabled shimmer"" />";
    private readonly string example9CsharpCode = @"
private bool isPaused;
private bool isStatic;";

    private readonly string example10RazorCode = @"
<BitButton OnClick=""AskAsync"" IsEnabled=""isThinking is false"">Ask a question</BitButton>

<BitTextShimmer role=""status""
                Static=""isThinking is false""
                RepeatDelay=""1000""
                Text=""@(isThinking ? ""Thinking about your question..."" : thinkingResult)"" />";
    private readonly string example10CsharpCode = @"
private bool isThinking;
private string thinkingResult = ""Ask a question to see the assistant think."";

private async Task AskAsync()
{
    isThinking = true;
    StateHasChanged();

    await Task.Delay(4000);

    thinkingResult = ""Here is your answer: a text shimmer is a gradient band clipped to the glyphs of the text."";
    isThinking = false;
}";

    private readonly string example11RazorCode = @"
<BitTextShimmer Color=""BitColor.Primary"" Text=""Primary"" />
<BitTextShimmer Color=""BitColor.Secondary"" Text=""Secondary"" />
<BitTextShimmer Color=""BitColor.Tertiary"" Text=""Tertiary"" />
<BitTextShimmer Color=""BitColor.Info"" Text=""Info"" />
<BitTextShimmer Color=""BitColor.Success"" Text=""Success"" />
<BitTextShimmer Color=""BitColor.Warning"" Text=""Warning"" />
<BitTextShimmer Color=""BitColor.SevereWarning"" Text=""SevereWarning"" />
<BitTextShimmer Color=""BitColor.Error"" Text=""Error"" />

<BitTextShimmer BaseColor=""#3f3f46"" GradientColor=""#22d3ee"" Text=""An ocean colored shimmer"" />

<BitTextShimmer BaseColor=""#92400e"" GradientColor=""#fbbf24"" Text=""A golden colored shimmer"" />";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        font-size: 1.5rem;
        font-style: italic;
    }
</style>

<BitTextShimmer Style=""font-size:2rem;font-weight:bold"" Text=""A styled text shimmer"" />

<BitTextShimmer Class=""custom-class"" Text=""A classy text shimmer"" />

<BitTextShimmer Style=""--bit-tsh-gradient-clr:hotpink"" Text=""A band colored through a custom property"" />";

    private readonly string example13RazorCode = @"
<BitTextShimmer Dir=""BitDir.Rtl"" Text=""در حال فکر کردن به سوال شما..."" />

<div dir=""rtl"">
    <BitTextShimmer Text=""در حال جستجو در مستندات..."" />
</div>

<BitTextShimmer Dir=""BitDir.Rtl"" Angle=""25"" Element=""h2"" Text=""نواری که ۲۵ درجه کج شده است"" />";
}
