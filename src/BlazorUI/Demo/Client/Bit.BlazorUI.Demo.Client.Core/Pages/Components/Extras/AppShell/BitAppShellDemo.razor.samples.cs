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

<BitAppShell NoInsets=""noInsets"" Styles=""insetStyles"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 12))
        {
            <div class=""row"">Row @i</div>
        }
    </div>
</BitAppShell>";
    private readonly string example2CsharpCode = @"
private bool noInsets;

// The four bars are sized from env(safe-area-inset-*), which is 0 on a desktop browser,
// so this example gives them a size and a color of their own to make them visible.
private readonly BitAppShellClassStyles insetStyles = new()
{
    Top = ""height:1.5rem;background:#0d7bbd"",
    Bottom = ""height:1.5rem;background:#0d7bbd"",
    Left = ""width:1rem;background:#7a3fb5"",
    Right = ""width:1rem;background:#7a3fb5"",
};";

    private readonly string example3RazorCode = @"
<BitAppShell AvoidKeyboard>
    <div class=""page"">
        <div class=""page-body"">
            @foreach (var i in Enumerable.Range(1, 10))
            {
                <div class=""row"">Row @i</div>
            }
        </div>
        @* Stuck to the bottom of the scrolling middle, whose height AvoidKeyboard is what shortens -
           so the composer rides up with the keyboard instead of disappearing behind it. *@
        <div class=""composer"">
            <BitTextField @bind-Value=""message"" Placeholder=""Write a message"" />
            <BitButton>Send</BitButton>
        </div>
    </div>
</BitAppShell>";
    private readonly string example3CsharpCode = @"
private string? message;

// .composer { position: sticky; bottom: 0; }
//
// A page that places chrome of its own OUTSIDE the shell reads the same measurement:
//   .fab { bottom: calc(var(--bit-ash-keyboard-inset) + 1rem); }";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""() => scrollShell?.GoToTop()"">GoToTop</BitButton>
<BitButton OnClick=""() => scrollShell?.GoToBottom()"">GoToBottom</BitButton>
<BitButton OnClick=""() => scrollShell?.ScrollTo(null, 240)"">ScrollTo(240)</BitButton>
<BitButton OnClick=""() => scrollShell?.ScrollBy(0, 120)"">ScrollBy(+120)</BitButton>
<BitButton OnClick=""ScrollToTarget"">ScrollToElement</BitButton>
<BitButton OnClick=""ReadOffset"">GetScrollOffset</BitButton>

<div>@offsetText</div>

<BitAppShell @ref=""scrollShell"">
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
<div>Top: <b>@scrollTop.ToString(""0"")</b> px (@((scrollPercent * 100).ToString(""0""))%)</div>
<div>Direction: <b>@scrollDirection</b></div>
<div>Phase: <b>@scrollPhase</b></div>
<div>Reached: <b>@reachedEdge</b></div>

<BitAppShell ReachOffset=""16""
             OnScroll=""HandleScroll""
             OnScrollStart=""HandleScrollStart""
             OnScrollEnd=""HandleScrollEnd""
             OnReachedTop=""HandleReachedTop""
             OnReachedBottom=""HandleReachedBottom"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 40))
        {
            <div class=""row"">Row @i</div>
        }
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
<BitToggle @bind-Value=""instantScroll"" Text=""ScrollBehavior: Instant (off = Smooth)"" />

<BitButton OnClick=""() => behaviorShell?.GoToTop()"">GoToTop</BitButton>
<BitButton OnClick=""() => behaviorShell?.GoToBottom()"">GoToBottom</BitButton>

<BitAppShell @ref=""behaviorShell""
             ScrollBehavior=""@(instantScroll ? BitScrollBehavior.Instant : BitScrollBehavior.Smooth)"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 30))
        {
            <div class=""row"">Row @i</div>
        }
    </div>
</BitAppShell>";
    private readonly string example6CsharpCode = @"
private bool instantScroll;
private BitAppShell? behaviorShell;";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""noScroll"" Text=""NoScroll"" />

<BitButton OnClick=""() => clipShell?.ScrollBy(0, 80)"">ScrollBy(+80)</BitButton>
<BitButton OnClick=""() => clipShell?.GoToTop()"">GoToTop</BitButton>

<BitAppShell @ref=""clipShell"" NoScroll=""noScroll"" Overscroll=""BitOverscroll.Contain"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 30))
        {
            <div class=""row"">Row @i</div>
        }
    </div>
</BitAppShell>";
    private readonly string example7CsharpCode = @"
private bool noScroll;
private BitAppShell? clipShell;";

    private readonly string example8RazorCode = @"
@* MainLayout.razor - the app shell wraps everything the application renders. *@

<BitAppShell @ref=""appShell"" PersistScroll>
    <BitLayout>
        <Header><AppHeader /></Header>
        <Main>@Body</Main>
    </BitLayout>
</BitAppShell>

@code {
    private BitAppShell? appShell;

    // Use AutoGoToTop instead to open every page at its top:
    //   <BitAppShell AutoGoToTop>

    // On sign-out, forget where the previous user was left in each page:
    private Task SignOut() => appShell?.ClearPersistedScroll() ?? Task.CompletedTask;
}";

    private readonly string example9RazorCode = @"
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
    private readonly string example9CsharpCode = @"
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

    private readonly string example10RazorCode = @"
<BitAppShell Classes=""shellClasses"" Styles=""shellStyles"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 12))
        {
            <div class=""row"">Row @i</div>
        }
    </div>
</BitAppShell>";
    private readonly string example10CsharpCode = @"
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

    private readonly string example11RazorCode = @"
<BitAppShell Dir=""BitDir.Rtl"" Styles=""insetStyles"">
    <div class=""page-body"">
        @foreach (var i in Enumerable.Range(1, 12))
        {
            <div class=""row"">سطر @i</div>
        }
    </div>
</BitAppShell>";
}
