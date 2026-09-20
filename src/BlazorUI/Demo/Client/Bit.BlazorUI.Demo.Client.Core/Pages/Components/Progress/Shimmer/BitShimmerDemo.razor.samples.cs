namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Shimmer;

public partial class BitShimmerDemo
{
    private readonly string example1RazorCode = @"
<BitShimmer />

<BitShimmer Height=""5rem"" />

<BitShimmer Width=""10rem"" />";

    private readonly string example2RazorCode = @"
<BitShimmer Height=""2rem"" />

<BitShimmer Shape=""BitShimmerShape.Square"" Height=""2rem"" />

<BitShimmer Shape=""BitShimmerShape.Pill"" Height=""2rem"" Width=""8rem"" />

<BitStack Horizontal Alignment=""BitAlignment.Center"">
    <BitShimmer Shape=""BitShimmerShape.Circle"" Height=""3rem"" />
    <BitShimmer Circle Width=""4rem"" />
</BitStack>";

    private readonly string example3RazorCode = @"
<BitShimmer Height=""2rem"" Radius=""0.25rem"" />

<BitShimmer Height=""2rem"" Radius=""1rem"" />

<BitShimmer Height=""2rem"" Shape=""BitShimmerShape.Pill"" Radius=""0"" />";

    private readonly string example4RazorCode = @"
<BitShimmer Lines=""3"" Height=""0.75rem"" />

<BitShimmer Lines=""4"" Height=""0.5rem"" Gap=""1rem"" LastLineWidth=""35%"" />

<BitShimmer Lines=""3"" Height=""1.5rem"" Gap=""0.25rem"" LastLineWidth=""100%"" />

<BitShimmer Lines=""4"" Height=""0.75rem"" LineWidths=""@([""100%"", ""88%"", ""94%"", ""52%""])"" />";

    private readonly string example5RazorCode = @"
<BitShimmer Height=""3rem"" />

<BitStack Horizontal Alignment=""BitAlignment.Center"">
    <BitShimmer Pulse Circle Height=""3rem"" />
    <BitShimmer Pulse Height=""3rem"" />
</BitStack>

<BitShimmer Animation=""BitShimmerAnimation.Fade"" Height=""3rem"" />

<BitShimmer Animation=""BitShimmerAnimation.None"" Height=""3rem"" />

<BitShimmer Height=""3rem"" Duration=""5000"" Delay=""1000"" />

<BitShimmer Lines=""4"" Height=""0.75rem"" Stagger=""200"" />";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => SimulateLoading(300)"">Fast response (300ms)</BitButton>
<BitButton OnClick=""() => SimulateLoading(1200)"">Just after the delay (1.2s)</BitButton>
<BitButton OnClick=""() => SimulateLoading(3000)"">Slow response (3s)</BitButton>

<BitShimmer Loaded=""@isDelayLoaded"" Height=""1.5rem"">The response is in.</BitShimmer>

<BitShimmer Loaded=""@isDelayLoaded"" ShowDelay=""1000"" Height=""1.5rem"">The response is in.</BitShimmer>

<BitShimmer Loaded=""@isDelayLoaded"" ShowDelay=""1000"" MinShowTime=""1000"" Height=""1.5rem"">The response is in.</BitShimmer>";
    private readonly string example6CsharpCode = @"
private bool isDelayLoaded = true;

private CancellationTokenSource? delayCts;

private async Task SimulateLoading(int duration)
{
    delayCts?.Cancel();
    delayCts?.Dispose();
    var cts = delayCts = new CancellationTokenSource();

    isDelayLoaded = false;
    StateHasChanged();

    try
    {
        await Task.Delay(duration, cts.Token);
    }
    catch (OperationCanceledException)
    {
        return;
    }

    isDelayLoaded = true;
}";

    private readonly string example7RazorCode = @"
<p>
    The plan costs <BitShimmer Inline Width=""4rem"" Height=""1em"" /> per month and renews on <BitShimmer Inline Width=""6rem"" Height=""1em"" />.
</p>";

    private readonly string example8RazorCode = @"
<BitShimmer Loaded=""@isDataLoaded"" Height=""1.5rem"">
    Content loaded successfully.
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isDataLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example8CsharpCode = @"
private bool isDataLoaded;";

    private readonly string example9RazorCode = @"
<BitShimmer Overlay Loaded=""@isOverlayLoaded"" Radius=""0.5rem"">
    <BitCard Style=""width:18rem"">
        <BitText Typography=""BitTypography.H6"">Monthly revenue</BitText>
        <BitText Typography=""BitTypography.H3"">$48,120</BitText>
        <BitText Typography=""BitTypography.Caption1"">Up 12% on the previous month.</BitText>
    </BitCard>
</BitShimmer>

<BitShimmer Loaded=""@isOverlayLoaded"" Height=""8rem"" Width=""18rem"" Radius=""0.5rem"">
    <BitCard Style=""width:18rem"">
        <BitText Typography=""BitTypography.H6"">Monthly revenue</BitText>
        <BitText Typography=""BitTypography.H3"">$48,120</BitText>
        <BitText Typography=""BitTypography.Caption1"">Up 12% on the previous month.</BitText>
    </BitCard>
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isOverlayLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example9CsharpCode = @"
private bool isOverlayLoaded;";

    private readonly string example10RazorCode = @"
<BitShimmer Loaded=""@isContentLoaded"" Width=""15rem"">
    <Content>
        <BitImage Height=""8rem"" Alt=""bit logo""
                  Src=""/images/bit-logo-blue.png"" />
        <br />
        <BitPersona PrimaryText=""Xafan Salina""
                    SecondaryText=""Software Engineer""
                    Size=""@BitPersonaSize.Size56""
                    Presence=""@BitPersonaPresence.Online""
                    ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />
    </Content>
    <Template>
        <BitShimmer Height=""8rem"" />
        <br />
        <BitStack Horizontal Alignment=""BitAlignment.Center"">
            <BitShimmer Circle Height=""3.5rem"" />
            <BitStack>
                <BitShimmer Height=""1.25rem"" Width=""8.5rem"" />
                <BitShimmer Height=""0.75rem"" Width=""7rem"" />
            </BitStack>
        </BitStack>
    </Template>
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isContentLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example10CsharpCode = @"
private bool isContentLoaded;";

    private readonly string example11RazorCode = @"
<BitShimmer Loaded=""@isAccessibleLoaded""
            Label=""Loading your profile""
            LoadedLabel=""Profile loaded""
            AriaLabel=""Profile""
            Lines=""2""
            Height=""1rem"">
    Xafan Salina, Software Engineer.
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isAccessibleLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example11CsharpCode = @"
private bool isAccessibleLoaded;";

    private readonly string example12RazorCode = @"
<BitShimmer Height=""2rem"" Background=""BitColor.Primary"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Secondary"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Tertiary"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Info"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Success"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Warning"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SevereWarning"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Error"" />
<BitShimmer Height=""2rem"" Background=""BitColor.PrimaryBackground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SecondaryBackground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.TertiaryBackground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.PrimaryForeground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SecondaryForeground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.TertiaryForeground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.PrimaryBorder"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SecondaryBorder"" />
<BitShimmer Height=""2rem"" Background=""BitColor.TertiaryBorder"" />";

    private readonly string example13RazorCode = @"
<BitShimmer Height=""1rem"" Color=""BitColor.Primary"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Secondary"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Tertiary"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Info"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Success"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Warning"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SevereWarning"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Error"" />
<BitShimmer Height=""1rem"" Color=""BitColor.PrimaryBackground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SecondaryBackground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.TertiaryBackground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.PrimaryForeground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SecondaryForeground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.TertiaryForeground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.PrimaryBorder"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SecondaryBorder"" />
<BitShimmer Height=""1rem"" Color=""BitColor.TertiaryBorder"" />";

    private readonly string example14RazorCode = @"
<BitStack Horizontal Alignment=""BitAlignment.Center"">
    <BitShimmer Circle Size=""BitSize.Small"" />
    <BitShimmer Size=""BitSize.Small"" />
</BitStack>

<BitStack Horizontal Alignment=""BitAlignment.Center"">
    <BitShimmer Circle Size=""BitSize.Medium"" />
    <BitShimmer Size=""BitSize.Medium"" />
</BitStack>

<BitStack Horizontal Alignment=""BitAlignment.Center"">
    <BitShimmer Circle Size=""BitSize.Large"" />
    <BitShimmer Size=""BitSize.Large"" />
</BitStack>";

    private readonly string example15RazorCode = @"
<style>
    .custom-class {
        box-shadow: aqua 0 0 1rem 0.5rem;
    }

    .custom-root {
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-shimmer {
        background: linear-gradient(90deg, transparent, darkred, transparent);
    }

    .custom-wrapper {
        border: solid tomato;
        border-radius: 0.5rem;
    }
</style>


<BitShimmer Height=""2.7rem"" Style=""border:2px solid gray"" />
<BitShimmer Height=""2.7rem"" Class=""custom-class"" />

<BitShimmer Height=""2.7rem"" Styles=""@(new() { Root = ""--bit-smr-bg-clr: goldenrod"",
                                                ShimmerWrapper = ""background-color: saddlebrown"" })"" />
<BitShimmer Height=""2.7rem"" Lines=""2"" Classes=""@(new() { Root = ""custom-root"",
                                                          Shimmer=""custom-shimmer"",
                                                          ShimmerWrapper = ""custom-wrapper"" })"" />";

    private readonly string example16RazorCode = @"
<BitShimmer Dir=""BitDir.Rtl"" Lines=""3"" Height=""1rem"" />

<BitStack Horizontal Alignment=""BitAlignment.Center"" Dir=""BitDir.Rtl"">
    <BitShimmer Circle Height=""3rem"" />
    <BitShimmer Height=""1.5rem"" />
</BitStack>";
}
