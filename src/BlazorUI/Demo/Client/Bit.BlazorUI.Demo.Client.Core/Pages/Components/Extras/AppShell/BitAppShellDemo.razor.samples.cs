namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AppShell;

public partial class BitAppShellDemo
{
    private readonly string example1RazorCode = @"
<BitAppShell>
    <div class=""page"">
        <div class=""page-head"">Header</div>
        <div class=""page-body"">
            @foreach (var i in Enumerable.Range(1, 12))
            {
                <div class=""row"">Row @i</div>
            }
        </div>
    </div>
</BitAppShell>";

    private readonly string example2RazorCode = @"
<BitToggle @bind-Value=""noInsets"" Text=""NoInsets"" />
<BitToggle @bind-Value=""noTopInset"" Text=""NoTopInset"" />
<BitToggle @bind-Value=""noBottomInset"" Text=""NoBottomInset"" />
<BitToggle @bind-Value=""noStartInset"" Text=""NoStartInset"" />
<BitToggle @bind-Value=""noEndInset"" Text=""NoEndInset"" />

@* The safe-area variables inherit, so any element around the shell can set them. *@
<div style=""@deviceInsets"">
    <BitAppShell NoInsets=""noInsets""
                 NoEndInset=""noEndInset""
                 NoTopInset=""noTopInset""
                 NoStartInset=""noStartInset""
                 NoBottomInset=""noBottomInset"">
        <div class=""page-body"">
            @foreach (var i in Enumerable.Range(1, 12))
            {
                <div class=""row"">Row @i</div>
            }
        </div>
    </BitAppShell>
</div>";
    private readonly string example2CsharpCode = @"
private bool noInsets;
private bool noTopInset;
private bool noEndInset;
private bool noStartInset;
private bool noBottomInset;

// Desktop browsers report no safe areas, so the shell is handed the ones a phone would report
// (which the No*Inset flags still take back to zero), and the bars are painted to be seen.
private const string deviceInsets = ""--bit-AppShell-safe-area-top:1.5rem;--bit-AppShell-safe-area-bottom:1.5rem;"" +
                                    ""--bit-AppShell-safe-area-start:1rem;--bit-AppShell-safe-area-end:1rem;"" +
                                    ""--bit-AppShell-inset-background:var(--bit-clr-pri)"";";

    private readonly string example3RazorCode = @"
<div>Keyboard inset: <b>@keyboardInset.ToString(""0"")</b> px</div>

<BitAppShell AvoidKeyboard OnKeyboardInsetChanged=""HandleKeyboardInset"">
    <div class=""page"">
        <div class=""page-body"">
            @foreach (var i in Enumerable.Range(1, 10))
            {
                <div class=""row"">Row @i</div>
            }
        </div>
        @* Stuck to the bottom of the scrolling middle, whose height AvoidKeyboard shortens -
           so the composer rides up with the keyboard instead of disappearing behind it. *@
        <div class=""composer"">
            <BitTextField @bind-Value=""message"" Placeholder=""Write a message"" AriaLabel=""Message"" />
            <BitButton>Send</BitButton>
        </div>
    </div>
</BitAppShell>";
    private readonly string example3CsharpCode = @"
private string? message;
private double keyboardInset;

private void HandleKeyboardInset(double inset) { keyboardInset = inset; StateHasChanged(); }

// .composer { position: sticky; bottom: 0; }
//
// Chrome placed OUTSIDE the shell reads the same measurement:
//   .fab { bottom: calc(var(--bit-ash-keyboard-inset) + 1rem); }
// and chrome that hides while the reader types is styled against the attribute:
//   .bit-ash[data-bit-ash-keyboard] .tab-bar { display: none; }";

    private readonly string example4RazorCode = @"
<BitToggle @bind-Value=""instantScroll"" Text=""ScrollBehavior: Instant"" />

<BitButton OnClick=""() => scrollShell?.GoToTop()"">GoToTop</BitButton>
<BitButton OnClick=""() => scrollShell?.GoToBottom()"">GoToBottom</BitButton>
<BitButton OnClick=""() => scrollShell?.ScrollTo(null, 240)"">ScrollTo(240)</BitButton>
<BitButton OnClick=""() => scrollShell?.ScrollBy(0, 120)"">ScrollBy(+120)</BitButton>
<BitButton OnClick=""ScrollToTarget"">ScrollToElement</BitButton>
<BitButton OnClick=""ReadOffset"">GetScrollOffset</BitButton>

<div role=""status"">@offsetText</div>

<BitAppShell @ref=""scrollShell""
             ScrollBehavior=""@(instantScroll ? BitScrollBehavior.Instant : BitScrollBehavior.Smooth)"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 30))
        {
            <div class=""row"" id=""@(i == 20 ? ""target-row"" : null)"">
                Row @i @(i == 20 ? ""(the ScrollToElement target)"" : null)
            </div>
        }
    </div>
</BitAppShell>";
    private readonly string example4CsharpCode = @"
private bool instantScroll;
private BitAppShell? scrollShell;
private string offsetText = ""-"";

private Task ScrollToTarget() => scrollShell?.ScrollToElement(""target-row"") ?? Task.CompletedTask;

private async Task ReadOffset()
{
    var offset = await (scrollShell?.GetScrollOffset() ?? Task.FromResult<BitScrollOffset?>(null));

    offsetText = offset is null
        ? ""-""
        : $""Top {offset.Top:0} of {offset.MaxTop:0} ({offset.PercentY * 100:0}%), AtTop: {offset.AtTop}, AtBottom: {offset.AtBottom}"";
}";

    private readonly string example5RazorCode = @"
<style>
    .headroom-head {
        transition: translate var(--bit-mot-duration) var(--bit-mot-easing),
                    box-shadow var(--bit-mot-duration) var(--bit-mot-easing);
    }

    /* Lifted off the content once the shell is scrolled... */
    .bit-ash[data-bit-ash-scrolled] .headroom-head {
        box-shadow: var(--bit-shd-appbar-top);
    }

    /* ...and out of the way while the reader scrolls down, back the moment they scroll up
       - or the moment the keyboard focus lands in it, so a focused control is never hidden. */
    .bit-ash[data-bit-ash-scroll-direction=""down""] .headroom-head:not(:focus-within) {
        translate: 0 -100%;
    }
</style>

<div>Top: <b>@scrollTop.ToString(""0"")</b> px (@((scrollPercent * 100).ToString(""0""))%)</div>
<div>Direction: <b>@scrollDirection</b></div>
<div>Phase: <b>@scrollPhase</b></div>
<div>Reached: <b>@reachedEdge</b></div>

<BitAppShell TrackScrollState
             ReachOffset=""16""
             OnScroll=""HandleScroll""
             OnScrollStart=""HandleScrollStart""
             OnScrollEnd=""HandleScrollEnd""
             OnReachedTop=""HandleReachedTop""
             OnReachedBottom=""HandleReachedBottom"">
    <div class=""page"">
        <div class=""page-head headroom-head"">Header</div>
        <div class=""page-body"">
            @foreach (var i in Enumerable.Range(1, 40))
            {
                <div class=""row"">Row @i</div>
            }
        </div>
    </div>
</BitAppShell>";
    private readonly string example5CsharpCode = @"
private double scrollTop;
private double scrollPercent;
private string scrollPhase = ""idle"";
private string reachedEdge = ""-"";
private string scrollDirection = ""-"";

private void HandleScroll(BitScrollOffset offset)
{
    scrollTop = offset.Top;
    scrollPercent = offset.PercentY;
    scrollDirection = offset.ScrollingDown ? ""down"" : offset.ScrollingUp ? ""up"" : ""-"";

    StateHasChanged();
}

private void HandleScrollStart() { scrollPhase = ""scrolling""; StateHasChanged(); }

private void HandleScrollEnd() { scrollPhase = ""idle""; StateHasChanged(); }

private void HandleReachedTop() { reachedEdge = ""top""; StateHasChanged(); }

private void HandleReachedBottom() { reachedEdge = ""bottom""; StateHasChanged(); }";

    private readonly string example6RazorCode = @"
<BitToggle @bind-Value=""clipOverflowX"" Text=""OverflowX: Hidden"" />
<BitToggle @bind-Value=""noScroll"" Text=""NoScroll"" />
<BitToggle @bind-Value=""stableGutter"" Text=""Gutter: Stable"" />
<BitToggle @bind-Value=""shortOverflowPage"" Text=""Short page"" />

<BitChoiceGroup @bind-Value=""overscroll"" Label=""Overscroll"" Horizontal TItem=""BitChoiceGroupOption<BitOverscroll>"" TValue=""BitOverscroll"">
    <BitChoiceGroupOption Text=""None"" Value=""BitOverscroll.None"" />
    <BitChoiceGroupOption Text=""Contain"" Value=""BitOverscroll.Contain"" />
    <BitChoiceGroupOption Text=""Auto"" Value=""BitOverscroll.Auto"" />
</BitChoiceGroup>

<BitButton OnClick=""() => overflowShell?.ScrollBy(0, 80)"">ScrollBy(+80)</BitButton>

<BitAppShell @ref=""overflowShell""
             NoScroll=""noScroll""
             Overscroll=""overscroll""
             Gutter=""@(stableGutter ? BitScrollbarGutter.Stable : null)""
             OverflowX=""@(clipOverflowX ? BitOverflow.Hidden : null)"">
    <div class=""page-body"">
        <div class=""row wide-row"">A row wider than the shell</div>
        @foreach (var i in Enumerable.Range(1, shortOverflowPage ? 1 : 20))
        {
            <div class=""row"">Row @i</div>
        }
    </div>
</BitAppShell>";
    private readonly string example6CsharpCode = @"
private bool noScroll;
private bool stableGutter;
private bool clipOverflowX;
private bool shortOverflowPage;
private BitAppShell? overflowShell;
private BitOverscroll overscroll = BitOverscroll.None;";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""stickyPadding"" Text=""ScrollPadding: 2.5rem (the header's height)"" />

<BitButton OnClick=""ScrollToPaddedRow"">ScrollToElement(row 15)</BitButton>
<BitButton OnClick=""() => paddingShell?.GoToTop()"">GoToTop</BitButton>

<BitAppShell @ref=""paddingShell"" ScrollPadding=""@paddingValue"">
    <div class=""page"">
        @* Stuck to the top of the scrolling middle, which is what the padding leaves room for. *@
        <div class=""page-head"">A sticky header</div>
        <div class=""page-body"">
            @foreach (var i in Enumerable.Range(1, 30))
            {
                <div class=""row"" id=""@(i == 15 ? ""padded-row"" : null)"">
                    Row @i @(i == 15 ? ""(the target)"" : null)
                </div>
            }
        </div>
    </div>
</BitAppShell>";
    private readonly string example7CsharpCode = @"
private bool stickyPadding;
private BitAppShell? paddingShell;

private string? paddingValue => stickyPadding ? ""2.5rem 0 0 0"" : null;

private Task ScrollToPaddedRow() => paddingShell?.ScrollToElement(""padded-row"") ?? Task.CompletedTask;";

    private readonly string example8RazorCode = @"
<BitToggle @bind-Value=""autoScroll"" Text=""AutoScroll"" />
<BitToggle @bind-Value=""preserveScroll"" Text=""PreserveScroll"" />

<BitButton OnClick=""AppendMessage"">Append to the end</BitButton>
<BitButton OnClick=""PrependMessages"">Prepend 5 older</BitButton>
<BitButton OnClick=""() => feedShell?.Refresh()"">Refresh</BitButton>

<BitAppShell @ref=""feedShell"" AutoScroll=""autoScroll"" PreserveScroll=""preserveScroll"">
    <div class=""page-body"">
        @foreach (var message in feed)
        {
            <div class=""row"">@message</div>
        }
    </div>
</BitAppShell>";
    private readonly string example8CsharpCode = @"
private bool autoScroll = true;
private bool preserveScroll = true;
private int feedNext = 13;
private int feedOlder;
private BitAppShell? feedShell;
private readonly List<string> feed = [.. Enumerable.Range(1, 12).Select(i => $""Message {i}"")];

private void AppendMessage() => feed.Add($""Message {feedNext++}"");

// Older content lands ABOVE what the reader is looking at, which is the arrival PreserveScroll is for.
private void PrependMessages()
{
    for (var i = 0; i < 5; i++)
    {
        feed.Insert(0, $""Older message {--feedOlder}"");
    }
}";

    private readonly string example9RazorCode = @"
@* MainLayout.razor - the app shell wraps everything the application renders. *@

<BitAppShell @ref=""appShell"" PersistScroll ScrollRestoration=""BitAppShellScrollRestoration.History"">
    <BitLayout>
        <Header><AppHeader /></Header>
        <Main>@Body</Main>
    </BitLayout>
</BitAppShell>

@code {
    private BitAppShell? appShell;

    // Back and forward put the reader back where they left a page; a link opens it at its top.
    // Leave ScrollRestoration out to restore every navigation to a page, like the tabs of an app.
    // Or use AutoGoToTop instead to open every page at its top:
    //   <BitAppShell AutoGoToTop>

    // On sign-out, forget where the previous user was left in each page:
    private Task SignOut() => appShell?.ClearPersistedScroll() ?? Task.CompletedTask;
}";

    private readonly string example10RazorCode = @"
<BitAppShell Values=""cascadingValues"">
    <AppShellDemoConsumer />
</BitAppShell>

<BitButton OnClick=""RenameUser"">Change the cascaded name</BitButton>

@* AppShellDemoConsumer.razor - anywhere below the shell, however deep. *@
<div>Cascaded by type: <b>@User?.Name</b> (@User?.Role)</div>
<div>Cascaded by name: <b>@Tenant</b></div>

@code {
    [CascadingParameter] public AppShellDemoUser? User { get; set; }

    [CascadingParameter(Name = ""Tenant"")] public string? Tenant { get; set; }
}";
    private readonly string example10CsharpCode = @"
public record AppShellDemoUser(string Name, string Role);

private string userName = ""Saleh Yusefnejad"";

private IEnumerable<BitCascadingValue> cascadingValues =>
[
    new(new AppShellDemoUser(userName, ""Developer"")),
    new(""bit platform"", ""Tenant""),
];

private void RenameUser()
{
    userName = userName == ""Saleh Yusefnejad"" ? ""Yaser Moradi"" : ""Saleh Yusefnejad"";
}";

    private readonly string example11RazorCode = @"
@* The bars are given a size around both shells, so the inset each one keeps can be seen on a desktop. *@
<div style=""--bit-AppShell-safe-area-top:0.75rem;--bit-AppShell-safe-area-bottom:0.75rem;--bit-AppShell-inset-background:var(--bit-clr-pri)"">
    <BitParams Parameters=""appShellParams"">
        <BitAppShell>
            <div class=""page-body""><div class=""row"">All cascaded</div></div>
        </BitAppShell>

        <BitAppShell NoBottomInset=""false"">
            <div class=""page-body""><div class=""row"">Own NoBottomInset</div></div>
        </BitAppShell>
    </BitParams>
</div>";
    private readonly string example11CsharpCode = @"
private readonly BitAppShellParams[] appShellParams =
[
    new()
    {
        NoBottomInset = true
    }
];";

    private readonly string example12RazorCode = @"
<BitAppShell Classes=""shellClasses"" Styles=""shellStyles"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 12))
        {
            <div class=""row"">Row @i</div>
        }
    </div>
</BitAppShell>";
    private readonly string example12CsharpCode = @"
private readonly BitAppShellClassStyles shellStyles = new()
{
    Root = ""border-radius:0.5rem;overflow:hidden"",
    Top = ""height:0.5rem;background:#3a9b3a"",
    Bottom = ""height:0.5rem;background:#3a9b3a"",
    Main = ""padding:0.75rem"",
};

private readonly BitAppShellClassStyles shellClasses = new()
{
    Main = ""styled-main"",
};";

    private readonly string example13RazorCode = @"
<BitAppShell Dir=""BitDir.Rtl"" Style=""@rtlInsets"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 12))
        {
            <div class=""row"">سطر @i</div>
        }
    </div>
</BitAppShell>";
    private readonly string example13CsharpCode = @"
// Desktop browsers report no safe areas, so the shell is handed the ones a phone would report
// (which the No*Inset flags still take back to zero), and the bars are painted to be seen.
private const string deviceInsets = ""--bit-AppShell-safe-area-top:1.5rem;--bit-AppShell-safe-area-bottom:1.5rem;"" +
                                    ""--bit-AppShell-safe-area-start:1rem;--bit-AppShell-safe-area-end:1rem;"" +
                                    ""--bit-AppShell-inset-background:var(--bit-clr-pri)"";

// The start bar is wider and of its own color, so the side it lands on can be seen.
private const string rtlInsets = $""{deviceInsets};--bit-AppShell-safe-area-start:2rem;--bit-AppShell-inset-start-background:var(--bit-clr-sec)"";";
}
