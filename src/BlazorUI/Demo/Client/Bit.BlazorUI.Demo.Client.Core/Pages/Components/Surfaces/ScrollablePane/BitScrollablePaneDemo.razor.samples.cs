namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.ScrollablePane;

public partial class BitScrollablePaneDemo
{
    private readonly string example1RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<BitScrollablePane Height=""10rem"" Class=""pane"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take shape.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. Think of this text as a bridge, connecting the empty spaces of now with the narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the symphony,
        the foundation upon which your creativity will build.
    </p>
</BitScrollablePane>";

    private readonly string example2RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitScrollablePane Width=""18rem"" Height=""6rem"" Class=""pane"">
    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding, and every story found its listener.
    </p>
</BitScrollablePane>

<BitButton OnClick=""() => maxHeightLines++"">Add a line</BitButton>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => maxHeightLines = 2"">Reset</BitButton>

<BitScrollablePane MaxHeight=""10rem"" Class=""pane"">
    @for (var i = 1; i <= maxHeightLines; i++)
    {
        <div class=""item"">Line @i</div>
    }
</BitScrollablePane>";
    private readonly string example2CsharpCode = @"
private int maxHeightLines = 2;";

    private readonly string example3RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .chip-row {
        gap: 0.5rem;
        display: flex;
    }

    .chip {
        flex: 0 0 auto;
        white-space: nowrap;
        border-radius: 1rem;
        padding: 0.25rem 0.75rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitChoiceGroup @bind-Value=""overflow""
                Horizontal
                Label=""Overflow""
                TItem=""BitChoiceGroupOption<BitOverflow>"" TValue=""BitOverflow"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitOverflow.Auto"" />
    <BitChoiceGroupOption Text=""Hidden"" Value=""BitOverflow.Hidden"" />
    <BitChoiceGroupOption Text=""Scroll"" Value=""BitOverflow.Scroll"" />
    <BitChoiceGroupOption Text=""Visible"" Value=""BitOverflow.Visible"" />
</BitChoiceGroup>

<BitToggle @bind-Value=""noScroll"" Label=""NoScroll"" />

<BitScrollablePane Overflow=""overflow"" NoScroll=""noScroll"" Height=""12rem"" Width=""20rem"" Class=""pane"">
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""item"">Item @i</div>
    }
</BitScrollablePane>

<BitScrollablePane Horizontal Width=""20rem"" Class=""pane"">
    <div class=""chip-row"">
        @for (var i = 1; i <= 12; i++)
        {
            <div class=""chip"">Item @i</div>
        }
    </div>
</BitScrollablePane>";
    private readonly string example3CsharpCode = @"
private bool noScroll;
private BitOverflow overflow;";

    private readonly string example4RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitChoiceGroup @bind-Value=""overscroll""
                Horizontal
                Label=""Overscroll""
                TItem=""BitChoiceGroupOption<BitOverscroll>"" TValue=""BitOverscroll"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitOverscroll.Auto"" />
    <BitChoiceGroupOption Text=""Contain"" Value=""BitOverscroll.Contain"" />
    <BitChoiceGroupOption Text=""None"" Value=""BitOverscroll.None"" />
</BitChoiceGroup>

<BitScrollablePane Height=""12rem"" Width=""20rem"" Class=""pane"">
    <div class=""item"">The outer pane</div>
    <BitScrollablePane Height=""8rem"" Overscroll=""overscroll"" Class=""pane"">
        @for (var i = 1; i <= 12; i++)
        {
            <div class=""item"">Inner @i</div>
        }
    </BitScrollablePane>
    @for (var i = 1; i <= 6; i++)
    {
        <div class=""item"">Outer @i</div>
    }
</BitScrollablePane>";
    private readonly string example4CsharpCode = @"
private BitOverscroll overscroll = BitOverscroll.Contain;";

    private readonly string example5RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitChoiceGroup @bind-Value=""scrollbarWidth""
                Horizontal
                Label=""ScrollbarWidth""
                TItem=""BitChoiceGroupOption<BitScrollbarWidth>"" TValue=""BitScrollbarWidth"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarWidth.Auto"" />
    <BitChoiceGroupOption Text=""Thin"" Value=""BitScrollbarWidth.Thin"" />
    <BitChoiceGroupOption Text=""None"" Value=""BitScrollbarWidth.None"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""gutter""
                Horizontal
                Label=""Gutter""
                TItem=""BitChoiceGroupOption<BitScrollbarGutter>"" TValue=""BitScrollbarGutter"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarGutter.Auto"" />
    <BitChoiceGroupOption Text=""Stable"" Value=""BitScrollbarGutter.Stable"" />
    <BitChoiceGroupOption Text=""BothEdges"" Value=""BitScrollbarGutter.BothEdges"" />
</BitChoiceGroup>

<BitStack Horizontal Wrap Gap=""1rem"">
    <BitToggle @bind-Value=""scrollbarColored"" Label=""ScrollbarColor"" />
    <BitToggle @bind-Value=""scrollbarOverflowing"" Label=""Overflowing"" />
</BitStack>

<BitScrollablePane Height=""12rem"" Width=""20rem"" Class=""pane""
                   ScrollbarWidth=""scrollbarWidth""
                   Gutter=""gutter""
                   ScrollbarColor=""@(scrollbarColored ? ""var(--bit-clr-pri) var(--bit-clr-bg-ter)"" : null)"">
    @for (var i = 1; i <= (scrollbarOverflowing ? 10 : 3); i++)
    {
        <div class=""item"">Item @i</div>
    }
</BitScrollablePane>";
    private readonly string example5CsharpCode = @"
private BitScrollbarWidth scrollbarWidth;
private BitScrollbarGutter gutter = BitScrollbarGutter.Stable;
private bool scrollbarColored;
private bool scrollbarOverflowing;";

    private readonly string example6RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .chip-row {
        gap: 0.5rem;
        display: flex;
    }

    .chip {
        flex: 0 0 auto;
        white-space: nowrap;
        border-radius: 1rem;
        padding: 0.25rem 0.75rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitToggle @bind-Value=""autoHideScrollbar"" Label=""AutoHideScrollbar"" />
<BitNumberField Label=""AutoHideDelay (ms)"" Min=""0"" Step=""200"" @bind-Value=""autoHideDelay"" Style=""max-width: 20rem"" />

<BitScrollablePane Height=""10rem"" Class=""pane"" Modern AutoHideScrollbar=""autoHideScrollbar"" AutoHideDelay=""(int)autoHideDelay"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take shape.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. Think of this text as a bridge, connecting the empty spaces of now with the narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the symphony,
        the foundation upon which your creativity will build.
    </p>
</BitScrollablePane>

<BitScrollablePane Horizontal Width=""20rem"" Class=""pane"" Modern AutoHideScrollbar=""autoHideScrollbar"" AutoHideDelay=""(int)autoHideDelay"">
    <div class=""chip-row"">
        @for (var i = 1; i <= 12; i++)
        {
            <div class=""chip"">Item @i</div>
        }
    </div>
</BitScrollablePane>";
    private readonly string example6CsharpCode = @"
private bool autoHideScrollbar = true;
private double autoHideDelay = 800;";

    private readonly string example7RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .chip-row {
        gap: 0.5rem;
        display: flex;
    }

    .chip {
        flex: 0 0 auto;
        white-space: nowrap;
        border-radius: 1rem;
        padding: 0.25rem 0.75rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .wide-grid {
        gap: 0.5rem;
        display: flex;
        width: max-content;
        padding: 0.25rem 0;
        flex-direction: column;
    }
</style>

<BitToggle @bind-Value=""fade"" Label=""Fade"" />

<BitSlider Label=""FadeSize (rem)"" Min=""0.5"" Max=""5"" Step=""0.5"" @bind-Value=""fadeSize"" Style=""max-width: 20rem"" />

<BitScrollablePane Height=""10rem"" Class=""pane"" Modern AutoHideScrollbar
                   Fade=""fade"" FadeSize=""@($""{fadeSize}rem"")"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take shape.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. Think of this text as a bridge, connecting the empty spaces of now with the narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the symphony,
        the foundation upon which your creativity will build.
    </p>
</BitScrollablePane>

<BitScrollablePane Height=""10rem"" Width=""20rem"" Class=""pane"" ScrollbarWidth=""BitScrollbarWidth.None"" Fade=""fade"">
    <div class=""wide-grid"">
        @for (var row = 1; row <= 8; row++)
        {
            <div class=""chip-row"">
                @for (var col = 1; col <= 8; col++)
                {
                    <div class=""chip"">R@(row)C@(col)</div>
                }
            </div>
        }
    </div>
</BitScrollablePane>";
    private readonly string example7CsharpCode = @"
private bool fade = true;
private double fadeSize = 2;";

    private readonly string example8RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<BitToggle @bind-Value=""focusable"" Label=""Focusable"" />

<BitScrollablePane Height=""10rem"" Class=""pane"" Fade
                   Focusable=""focusable""
                   AriaLabel=""Release notes"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take shape.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. Think of this text as a bridge, connecting the empty spaces of now with the narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the symphony,
        the foundation upon which your creativity will build.
    </p>
</BitScrollablePane>";
    private readonly string example8CsharpCode = @"
private bool focusable = true;";

    private readonly string example9RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .readout {
        font-family: monospace;
        color: var(--bit-clr-fg-sec);
    }
</style>

<BitNumberField Label=""ScrollThrottle (ms)"" Min=""0"" Step=""50"" @bind-Value=""scrollThrottle"" Style=""max-width: 20rem"" />

<BitProgress Value=""@((scrollOffset?.PercentY ?? 0) * 100)"" AriaLabel=""Scroll progress"" />

<BitScrollablePane Height=""12rem"" Class=""pane""
                   ScrollThrottle=""(int)scrollThrottle""
                   OnScroll=""HandleScroll""
                   OnScrollStart=""HandleScrollStart""
                   OnScrollEnd=""HandleScrollEnd"">
    @for (var i = 1; i <= 20; i++)
    {
        <div class=""item"">Row @i</div>
    }
</BitScrollablePane>

<div class=""readout"">
    Top: @((scrollOffset?.Top ?? 0).ToString(""0"")) of @((scrollOffset?.MaxTop ?? 0).ToString(""0""))
    &nbsp;|&nbsp; PercentY: @(((scrollOffset?.PercentY ?? 0) * 100).ToString(""0""))%
    &nbsp;|&nbsp; AtTop: @(scrollOffset?.AtTop.ToString() ?? ""-"")
    &nbsp;|&nbsp; AtBottom: @(scrollOffset?.AtBottom.ToString() ?? ""-"")
</div>

<div class=""readout"">State: @scrollState &nbsp;|&nbsp; Going: @scrollDirection</div>";
    private readonly string example9CsharpCode = @"
private double scrollThrottle;
private string scrollState = ""-"";
private string scrollDirection = ""-"";
private BitScrollOffset? scrollOffset;
private void HandleScroll(BitScrollOffset offset)
{
    scrollOffset = offset;

    // A report that carries no move of its own - the first one, or one the pane's own size changed -
    // leaves the direction where it was rather than blanking it out.
    if (offset.ScrollingDown) scrollDirection = $""down ({offset.DeltaTop:0.#}px)"";
    else if (offset.ScrollingUp) scrollDirection = $""up ({-offset.DeltaTop:0.#}px)"";

    StateHasChanged();
}
private void HandleScrollStart() => scrollState = ""scrolling..."";
private void HandleScrollEnd(BitScrollOffset offset) => scrollState = $""stopped at {offset.Top:0} ({offset.PercentY * 100:0}%)"";";

    private readonly string example10RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .readout {
        font-family: monospace;
        color: var(--bit-clr-fg-sec);
    }
</style>

<BitNumberField Label=""ReachOffset (px)"" Min=""0"" Step=""20"" @bind-Value=""reachOffset"" Style=""max-width: 20rem"" />

<BitScrollablePane Height=""14rem"" Class=""pane""
                   ReachOffset=""(int)reachOffset""
                   OnReachedTop=""HandleReachedTop""
                   OnReachedBottom=""LoadMoreRows"">
    @foreach (var row in endlessRows)
    {
        <div class=""item"">@row</div>
    }
    @if (loadingMore)
    {
        <div class=""item"">Loading...</div>
    }
</BitScrollablePane>

<div class=""readout"">Last edge reached: <b>@reachedEdge</b> &nbsp;|&nbsp; rows: <b>@endlessRows.Count</b></div>";
    private readonly string example10CsharpCode = @"
private bool loadingMore;
private double reachOffset = 40;
private string reachedEdge = ""-"";
private readonly List<string> endlessRows = [.. Enumerable.Range(1, 12).Select(i => $""Row {i}"")];
private void HandleReachedTop() => reachedEdge = ""top"";
private async Task LoadMoreRows()
{
    reachedEdge = ""bottom"";

    if (loadingMore || endlessRows.Count >= 60) return;

    loadingMore = true;
    StateHasChanged();

    await Task.Delay(600);

    var next = endlessRows.Count;
    endlessRows.AddRange(Enumerable.Range(next + 1, 12).Select(i => $""Row {i}""));

    loadingMore = false;
    StateHasChanged();
}";

    private readonly string example11RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .sticky-head {
        top: 0;
        z-index: 1;
        position: sticky;
        padding: 0.25rem 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-pri);
        border-bottom: 1px solid var(--bit-clr-brd-sec);
    }

    .readout {
        font-family: monospace;
        color: var(--bit-clr-fg-sec);
    }
</style>

<BitToggle @bind-Value=""smooth"" Label=""Smooth"" />

<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitButton OnClick=""() => scrollablePane?.ScrollToStart()"">To start</BitButton>
    <BitButton OnClick=""() => scrollablePane?.ScrollToEnd()"">To end</BitButton>
    <BitButton OnClick=""() => scrollablePane?.ScrollTo(null, 200)"">To 200px</BitButton>
    <BitButton OnClick=""() => scrollablePane?.ScrollBy(0, 100)"">Down 100px</BitButton>
    <BitButton OnClick='() => scrollablePane?.ScrollToElement(""scp-row-15"")'>To row 15</BitButton>
    <BitButton OnClick='() => scrollablePane?.ScrollToElement(""scp-row-15"", alignment: BitScrollAlignment.Center)'>To row 15, centered</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""ReadScrollOffset"">Read position</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => scrollablePane?.FocusAsync()"">Focus the pane</BitButton>
</BitStack>

<BitScrollablePane @ref=""scrollablePane"" Height=""12rem"" Class=""pane"" Smooth=""smooth""
                   ScrollPadding=""2.5rem"" Focusable AriaLabel=""Rows"">
    <div class=""sticky-head"">A sticky header, with 2.5rem of ScrollPadding under it</div>
    @for (var i = 1; i <= 25; i++)
    {
        <div class=""item"" id=""@($""scp-row-{i}"")"">Row @i</div>
    }
</BitScrollablePane>

<div class=""readout"">@readPosition</div>

<BitScrollablePane Height=""8rem"" Class=""pane"" InitialScrollTop=""250"">
    @for (var i = 1; i <= 25; i++)
    {
        <div class=""item"">Row @i</div>
    }
</BitScrollablePane>";
    private readonly string example11CsharpCode = @"
private bool smooth = true;
private string readPosition = ""-"";
private BitScrollablePane? scrollablePane;
private async Task ReadScrollOffset()
{
    if (scrollablePane is null) return;

    var offset = await scrollablePane.GetScrollOffset();

    readPosition = offset is null
        ? ""-""
        : $""Top {offset.Top:0} of {offset.MaxTop:0}, at the bottom: {offset.AtBottom}"";
}";

    private readonly string example12RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitNumberField Label=""AutoScrollThreshold (px)"" Min=""0"" Step=""10"" @bind-Value=""autoScrollThreshold"" Style=""max-width: 20rem"" />

<BitButton OnClick=""AddAutoScrollContent"" IsEnabled=""@(autoScrollRunning is false)"">Add lines periodically</BitButton>

<BitScrollablePane Height=""14rem"" Class=""pane"" AutoScroll AutoScrollThreshold=""(int)autoScrollThreshold"">
    <div class=""item"">The log starts here.</div>
    @foreach (var line in autoScrollLines)
    {
        <div class=""item"">@line</div>
    }
</BitScrollablePane>";
    private readonly string example12CsharpCode = @"
private bool autoScrollRunning;
private double autoScrollThreshold;
private readonly List<string> autoScrollLines = [];
private async Task AddAutoScrollContent()
{
    autoScrollRunning = true;

    try
    {
        for (var i = 0; i < 15; i++)
        {
            await Task.Delay(700);

            autoScrollLines.Add($""A new line arrived at {DateTime.Now:HH:mm:ss} ({Random.Shared.Next(1, 100)})"");

            StateHasChanged();
        }
    }
    finally
    {
        autoScrollRunning = false;
    }
}";

    private readonly string example13RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .no-anchor {
        overflow-anchor: none;
    }

    .readout {
        font-family: monospace;
        color: var(--bit-clr-fg-sec);
    }
</style>

<BitToggle @bind-Value=""preserveScroll"" Label=""PreserveScroll"" />

<BitScrollablePane Height=""14rem"" Class=""pane no-anchor"" Fade
                   PreserveScroll=""preserveScroll""
                   ReachOffset=""60""
                   OnReachedTop=""LoadOlderMessages"">
    @if (loadingOlder)
    {
        <div class=""item"">Loading older messages...</div>
    }
    @foreach (var message in conversation)
    {
        <div @key=""message"" class=""item"">@message</div>
    }
</BitScrollablePane>

<div class=""readout"">Oldest message loaded: <b>@oldestMessage</b> &nbsp;|&nbsp; messages: <b>@conversation.Count</b></div>";
    private readonly string example13CsharpCode = @"
private bool preserveScroll = true;
private bool loadingOlder;
private int oldestMessage = 1;
private readonly List<string> conversation = [.. Enumerable.Range(1, 14).Select(i => $""Message {i}"")];
private async Task LoadOlderMessages()
{
    if (loadingOlder || oldestMessage <= -40) return;

    loadingOlder = true;
    StateHasChanged();

    await Task.Delay(500);

    // The older messages go in at the TOP, which is what pushes everything the reader was looking at
    // down the screen unless the pane keeps their place for them.
    conversation.InsertRange(0, Enumerable.Range(oldestMessage - 8, 8).Select(i => $""Message {i}""));
    oldestMessage -= 8;

    loadingOlder = false;
    StateHasChanged();
}";

    private readonly string example14RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .card {
        width: 8rem;
        height: 4rem;
        padding: 0.5rem;
        display: inline-block;
        margin: 0.5rem 0.5rem 0.5rem 0;
        color: var(--bit-clr-fg-pri);
        border-radius: var(--bit-shp-radius-sm);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitChoiceGroup @bind-Value=""snap""
                Horizontal
                Label=""Snap""
                TItem=""BitChoiceGroupOption<BitScrollSnap>"" TValue=""BitScrollSnap"">
    <BitChoiceGroupOption Text=""None"" Value=""BitScrollSnap.None"" />
    <BitChoiceGroupOption Text=""Proximity"" Value=""BitScrollSnap.Proximity"" />
    <BitChoiceGroupOption Text=""Mandatory"" Value=""BitScrollSnap.Mandatory"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""snapAlign""
                Horizontal
                Label=""SnapAlign""
                TItem=""BitChoiceGroupOption<BitScrollSnapAlign>"" TValue=""BitScrollSnapAlign"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitScrollSnapAlign.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitScrollSnapAlign.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitScrollSnapAlign.End"" />
</BitChoiceGroup>

<BitToggle @bind-Value=""snapStop"" Label=""SnapStop"" />

<BitScrollablePane Horizontal Width=""22rem"" Class=""pane"" Modern
                   Snap=""snap"" SnapAlign=""snapAlign"" SnapStop=""snapStop"">
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""card"">Card @i</div>
    }
</BitScrollablePane>";
    private readonly string example14CsharpCode = @"
private bool snapStop = true;
private BitScrollSnap snap = BitScrollSnap.Mandatory;
private BitScrollSnapAlign snapAlign = BitScrollSnapAlign.Start;";

    private readonly string example15RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .card {
        width: 8rem;
        height: 4rem;
        padding: 0.5rem;
        display: inline-block;
        margin: 0.5rem 0.5rem 0.5rem 0;
        color: var(--bit-clr-fg-pri);
        border-radius: var(--bit-shp-radius-sm);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitStack Horizontal Wrap Gap=""1rem"">
    <BitToggle @bind-Value=""dragScroll"" Label=""DragScroll"" />
    <BitToggle @bind-Value=""dragMomentum"" Label=""DragMomentum"" />
    <BitToggle @bind-Value=""horizontalWheel"" Label=""HorizontalWheel"" />
</BitStack>

<BitScrollablePane Horizontal Width=""22rem"" Class=""pane"" Modern
                   DragScroll=""dragScroll"" DragMomentum=""dragMomentum""
                   HorizontalWheel=""horizontalWheel"">
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""card"">Card @i</div>
    }
</BitScrollablePane>";
    private readonly string example15CsharpCode = @"
private bool dragScroll = true;
private bool dragMomentum = true;
private bool horizontalWheel = true;";

    private readonly string example16RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .themed-panes {
        --bit-ScrollablePane-scrollbar-size: 0.75rem;
        --bit-ScrollablePane-scrollbar-thumb-radius: 0.125rem;
        --bit-ScrollablePane-scrollbar-thumb-color: var(--bit-clr-pri);
        --bit-ScrollablePane-scrollbar-thumb-hover-color: var(--bit-clr-pri-hover);
        --bit-ScrollablePane-scrollbar-thumb-active-color: var(--bit-clr-pri-active);
        --bit-ScrollablePane-scrollbar-track-color: var(--bit-clr-bg-sec);
        --bit-ScrollablePane-focus-color: var(--bit-clr-sec);
    }
</style>

<div class=""themed-panes"">
    <BitScrollablePane Height=""8rem"" Class=""pane"" Modern Focusable AriaLabel=""Themed pane"">
        @for (var i = 1; i <= 10; i++)
        {
            <div class=""item"">Styled by the container @i</div>
        }
    </BitScrollablePane>
    <BitScrollablePane Height=""8rem"" Class=""pane"" Modern Fade
                       Style=""--bit-ScrollablePane-scrollbar-thumb-color: var(--bit-clr-sec); --bit-ScrollablePane-fade-size: 3rem;"">
        @for (var i = 1; i <= 10; i++)
        {
            <div class=""item"">Styled by its own Style @i</div>
        }
    </BitScrollablePane>
</div>";

    private readonly string example17RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }
</style>

<BitParams Parameters=""@scrollablePaneParams"">
    <BitStack Horizontal Wrap Gap=""1rem"">
        <BitScrollablePane Width=""14rem"" Class=""pane"">
            @for (var i = 1; i <= 10; i++)
            {
                <div class=""item"">Cascaded @i</div>
            }
        </BitScrollablePane>
        <BitScrollablePane Width=""14rem"" Class=""pane"" Fade=""false"" Height=""6rem"">
            @for (var i = 1; i <= 10; i++)
            {
                <div class=""item"">Own Fade, Height @i</div>
            }
        </BitScrollablePane>
    </BitStack>
</BitParams>";
    private readonly string example17CsharpCode = @"
private readonly List<IBitComponentParams> scrollablePaneParams =
[
    new BitScrollablePaneParams
    {
        Height = ""10rem"",
        Modern = true,
        AutoHideScrollbar = true,
        Fade = true,
        Overscroll = BitOverscroll.Contain,
    }
];";

    private readonly string example18RazorCode = @"
<style>
    .item {
        margin: 0.5rem 0;
        padding: 0.5rem 1.25rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-ter);
    }

    .custom-pane {
        padding: 0.5rem;
        border-radius: 0.5rem;
        background-color: var(--bit-clr-bg-pri);
    }
</style>

<BitScrollablePane Height=""8rem""
                   Style=""border: 2px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"">
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""item"">Item @i</div>
    }
</BitScrollablePane>

<BitScrollablePane Height=""8rem"" Class=""custom-pane"" Modern>
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""item"">Item @i</div>
    }
</BitScrollablePane>";

    private readonly string example19RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<BitScrollablePane Dir=""BitDir.Rtl"" lang=""fa"" Height=""8rem"" Class=""pane"" Modern>
    <p>
        داستان‌ها روزگاری پیوند میان مردم را می‌بافتند، سمفونی‌ای از صداها که رویاهای مشترک را می‌ساخت.
        هر واژه معنایی داشت و هر مکث فهمی به همراه می‌آورد.
    </p>
    <p>
        در آغاز، سکوت است؛ بومی سفید که در انتظار پر شدن است، فضایی آرام که در آن خلاقیت منتظر بیدار شدن است.
        این واژه‌ها موقتی‌اند و جای ایده‌هایی را گرفته‌اند که هنوز نیامده‌اند.
    </p>
    <p>
        در این فضا، امکان حکمرانی می‌کند. لحظه‌ای معلق در زمان، جایی که تخیل آزادانه می‌رقصد و هر واژه
        توان آن را دارد که به چیزی خارق‌العاده بدل شود.
    </p>
</BitScrollablePane>

<BitScrollablePane Dir=""BitDir.Rtl"" lang=""fa"" Horizontal Width=""20rem"" Class=""pane"" Fade>
    داستان‌ها روزگاری پیوند میان مردم را می‌بافتند، سمفونی‌ای از صداها که رویاهای مشترک را می‌ساخت.
</BitScrollablePane>";
}
