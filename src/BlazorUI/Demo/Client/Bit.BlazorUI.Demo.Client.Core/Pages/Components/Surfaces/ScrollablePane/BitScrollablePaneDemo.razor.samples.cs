namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.ScrollablePane;

public partial class BitScrollablePaneDemo
{
    private readonly string example1RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }
</style>

<BitScrollablePane Height=""12rem"" Class=""pane"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>";

    private readonly string example2RazorCode = @"
<BitScrollablePane Height=""10rem"" Class=""pane"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>

<BitScrollablePane Width=""300px"" Horizontal Class=""pane"">
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
</BitScrollablePane>

<BitButton OnClick=""() => maxHeightLines++"">Add a line</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => maxHeightLines = 2"">Reset</BitButton>

<BitScrollablePane MaxHeight=""10rem"" Class=""pane"">
    @for (var i = 1; i <= maxHeightLines; i++)
    {
        <div class=""item"">Line @i</div>
    }
</BitScrollablePane>";

    private readonly string example3RazorCode = @"
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

<BitNumberField Label=""Items count"" Min=""4"" @bind-Value=""@overflowItemsCount"" />

<BitScrollablePane Overflow=""@overflow"" NoScroll=""noScroll"" Height=""16rem"" Class=""pane"">
    @for (int i = 0; i < overflowItemsCount; i++)
    {
        var index = i;
        <div class=""item"">@index</div>
    }
</BitScrollablePane>";
    private readonly string example3CsharpCode = @"
private bool noScroll;
private BitOverflow overflow;
private double overflowItemsCount = 6;";

    private readonly string example4RazorCode = @"
<BitScrollablePane Horizontal Width=""20rem"" Class=""pane"">
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    Each word carried meaning, each pause brought understanding.
</BitScrollablePane>

<style>
    .chip-row { display: flex; gap: 0.5rem; }
    .chip { flex: 0 0 auto; padding: 0.25rem 0.75rem; border-radius: 1rem; background-color: #777; color: #fff; }
</style>

<BitScrollablePane Horizontal Width=""20rem"" Class=""pane"">
    <div class=""chip-row"">
        @for (var i = 1; i <= 12; i++)
        {
            <div class=""chip"">Item @i</div>
        }
    </div>
</BitScrollablePane>";

    private readonly string example5RazorCode = @"
<BitChoiceGroup @bind-Value=""overscroll""
                Horizontal
                Label=""Overscroll""
                TItem=""BitChoiceGroupOption<BitOverscroll>"" TValue=""BitOverscroll"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitOverscroll.Auto"" />
    <BitChoiceGroupOption Text=""Contain"" Value=""BitOverscroll.Contain"" />
    <BitChoiceGroupOption Text=""None"" Value=""BitOverscroll.None"" />
</BitChoiceGroup>

<BitScrollablePane Height=""12rem"" Width=""22rem"" Class=""pane"">
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
    private readonly string example5CsharpCode = @"
private BitOverscroll overscroll = BitOverscroll.Contain;";

    private readonly string example6RazorCode = @"
<BitChoiceGroup @bind-Value=""gutter""
                Horizontal
                Label=""Scrollbar gutter""
                TItem=""BitChoiceGroupOption<BitScrollbarGutter>"" TValue=""BitScrollbarGutter"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarGutter.Auto"" />
    <BitChoiceGroupOption Text=""Stable"" Value=""BitScrollbarGutter.Stable"" />
    <BitChoiceGroupOption Text=""BothEdges"" Value=""BitScrollbarGutter.BothEdges"" />
</BitChoiceGroup>

<BitNumberField Label=""Items count"" Min=""4"" @bind-Value=""@gutterItemsCount"" />

<BitScrollablePane Gutter=""@gutter"" Height=""16rem"" Class=""pane"">
    @for (int i = 0; i < gutterItemsCount; i++)
    {
        var index = i;
        <div class=""item"">@index</div>
    }
</BitScrollablePane>";
    private readonly string example6CsharpCode = @"
private BitScrollbarGutter gutter;
private double gutterItemsCount = 6;";

    private readonly string example7RazorCode = @"
<BitScrollablePane Height=""10rem"" Class=""pane"" ScrollbarWidth=""BitScrollbarWidth.Thin"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>

<BitScrollablePane Height=""10rem"" Class=""pane"" ScrollbarWidth=""BitScrollbarWidth.None"" Fade>
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>

<BitScrollablePane Height=""10rem"" Class=""pane"" ScrollbarColor=""#0078D4 #DEECF9"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>";

    private readonly string example8RazorCode = @"
<BitToggle @bind-Value=""autoHideScrollbar"" Label=""AutoHideScrollbar"" />

<BitScrollablePane Height=""10rem"" Class=""pane"" Modern AutoHideScrollbar=""autoHideScrollbar"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>

<BitScrollablePane Horizontal Width=""20rem"" Class=""pane"" Modern
                   AutoHideScrollbar=""autoHideScrollbar"" Style=""--bit-scp-sbs:0.75rem"">
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    Each word carried meaning, each pause brought understanding.
</BitScrollablePane>";
    private readonly string example8CsharpCode = @"
private bool autoHideScrollbar = true;";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""fade"" Label=""Fade"" />
<BitSlider Label=""FadeSize (rem)"" Min=""0.5"" Max=""5"" Step=""0.5"" @bind-Value=""fadeSize"" />

<BitScrollablePane Height=""12rem"" Class=""pane"" Fade=""fade"" FadeSize=""@($""{fadeSize}rem"")"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>

<BitScrollablePane Height=""10rem"" Width=""22rem"" Class=""pane"" Fade>
    <div class=""wide-grid"">
        @for (var row = 1; row <= 8; row++)
        {
            <div class=""wide-row"">
                @for (var col = 1; col <= 8; col++)
                {
                    <div class=""chip"">R@(row)C@(col)</div>
                }
            </div>
        }
    </div>
</BitScrollablePane>";
    private readonly string example9CsharpCode = @"
private bool fade = true;
private double fadeSize = 2;";

    private readonly string example10RazorCode = @"
<BitNumberField Label=""ScrollThrottle (ms)"" Min=""0"" Step=""50"" @bind-Value=""scrollThrottle"" />

<BitProgress Value=""@((scrollOffset?.PercentY ?? 0) * 100)"" />

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

<div>
    Top: @((scrollOffset?.Top ?? 0).ToString(""0"")) of @((scrollOffset?.MaxTop ?? 0).ToString(""0""))
    | PercentY: @(((scrollOffset?.PercentY ?? 0) * 100).ToString(""0""))%
    | AtTop: @(scrollOffset?.AtTop.ToString() ?? ""-"")
    | AtBottom: @(scrollOffset?.AtBottom.ToString() ?? ""-"")
</div>
<div>State: @scrollState | Going: @scrollDirection</div>";
    private readonly string example10CsharpCode = @"
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

    private readonly string example11RazorCode = @"
<BitNumberField Label=""ReachOffset (px)"" Min=""0"" Step=""20"" @bind-Value=""reachOffset"" />

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

<div>Last edge reached: <b>@reachedEdge</b> | rows: <b>@endlessRows.Count</b></div>";
    private readonly string example11CsharpCode = @"
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

    private readonly string example12RazorCode = @"
<BitToggle @bind-Value=""smooth"" Label=""Smooth"" />

<BitButton OnClick=""() => scrollablePane?.ScrollToStart()"">To start</BitButton>
<BitButton OnClick=""() => scrollablePane?.ScrollToEnd()"">To end</BitButton>
<BitButton OnClick=""() => scrollablePane?.ScrollTo(null, 200)"">To 200px</BitButton>
<BitButton OnClick=""() => scrollablePane?.ScrollBy(0, 100)"">Down 100px</BitButton>
<BitButton OnClick='() => scrollablePane?.ScrollToElement(""scp-row-15"")'>To row 15</BitButton>
<BitButton OnClick='() => scrollablePane?.ScrollToElement(""scp-row-15"", alignment: BitScrollAlignment.Center)'>To row 15, centered</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ReadScrollOffset"">Read position</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => scrollablePane?.FocusAsync()"">Focus the pane</BitButton>

<style>
    .sticky-head { position: sticky; top: 0; z-index: 1; padding: 0.25rem 0.5rem; background-color: #4A4A4A; color: #fff; }
</style>

<BitScrollablePane @ref=""scrollablePane"" Height=""12rem"" Class=""pane"" Smooth=""smooth""
                   ScrollPadding=""2.5rem"" Focusable Role=""region"" AriaLabel=""Rows"">
    <div class=""sticky-head"">A header of the pane's own, and 2.5rem of ScrollPadding under it</div>
    @for (var i = 1; i <= 25; i++)
    {
        <div class=""item"" id=""@($""scp-row-{i}"")"">Row @i</div>
    }
</BitScrollablePane>

<div>@readPosition</div>

<div>A pane that opened 250px down, without ever having been at the top:</div>

<BitScrollablePane Height=""8rem"" Class=""pane"" InitialScrollTop=""250"">
    @for (var i = 1; i <= 25; i++)
    {
        <div class=""item"">Row @i</div>
    }
</BitScrollablePane>";
    private readonly string example12CsharpCode = @"
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

    private readonly string example13RazorCode = @"
<BitNumberField Label=""AutoScrollThreshold (px)"" Min=""0"" Step=""10"" @bind-Value=""autoScrollThreshold"" />

<BitButton OnClick=""AddAutoScrollContent"" IsEnabled=""@(autoScrollRunning is false)"">Add lines periodically</BitButton>

<BitScrollablePane Height=""14rem"" Class=""pane"" AutoScroll AutoScrollThreshold=""(int)autoScrollThreshold"">
    <div class=""item"">The log starts here.</div>
    @foreach (var line in autoScrollLines)
    {
        <div class=""item"">@line</div>
    }
</BitScrollablePane>";
    private readonly string example13CsharpCode = @"
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

    private readonly string example14RazorCode = @"
<BitToggle @bind-Value=""preserveScroll"" Label=""PreserveScroll"" />

<BitScrollablePane Height=""14rem"" Class=""pane no-anchor"" Fade
                   PreserveScroll=""preserveScroll""
                   ReachOffset=""60""
                   OnReachedTop=""LoadOlderMessages"">
    @if (loadingOlder)
    {
        <div class=""item"">Loading older messages...</div>
    }
    @* Keyed, so the older messages are new elements at the TOP rather than new text in the elements
       that were already there - which is what there is a place to keep for. *@
    @foreach (var message in conversation)
    {
        <div @key=""message"" class=""item"">@message</div>
    }
</BitScrollablePane>

<div>Oldest message loaded: <b>@oldestMessage</b> | messages: <b>@conversation.Count</b></div>";
    private readonly string example14CsharpCode = @"
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

    private readonly string example15RazorCode = @"
<BitToggle @bind-Value=""focusable"" Label=""Focusable"" />

<BitScrollablePane Height=""10rem"" Class=""pane""
                   Focusable=""focusable""
                   Role=""region""
                   AriaLabel=""Release notes"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
    <p>
        Whether it is a tale of adventure, a reflection of truth, or an idea that sparks change, these lines are
        yours to fill, to shape, and to make uniquely yours. The journey begins here, in this quiet moment where
        everything is possible.
    </p>
    <p>
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the
        symphony, the foundation upon which your creativity will build. Soon, this space will hold your thoughts,
        your visions, and your voice.
    </p>
</BitScrollablePane>";
    private readonly string example15CsharpCode = @"
private bool focusable = true;";

    private readonly string example16RazorCode = @"
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

<style>
    .snap-card { display: inline-block; width: 8rem; height: 4rem; margin: 0.5rem 0.5rem 0.5rem 0; padding: 0.5rem; color: #fff; background-color: #777; }
</style>

<BitScrollablePane Horizontal Width=""22rem"" Class=""pane"" Modern
                   Snap=""snap"" SnapAlign=""snapAlign"" SnapStop=""snapStop"">
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""snap-card"">Card @i</div>
    }
</BitScrollablePane>";
    private readonly string example16CsharpCode = @"
private bool snapStop = true;
private BitScrollSnap snap = BitScrollSnap.Mandatory;
private BitScrollSnapAlign snapAlign = BitScrollSnapAlign.Start;";

    private readonly string example17RazorCode = @"
<BitToggle @bind-Value=""dragScroll"" Label=""DragScroll"" />
<BitToggle @bind-Value=""dragMomentum"" Label=""DragMomentum"" />
<BitToggle @bind-Value=""horizontalWheel"" Label=""HorizontalWheel"" />

<style>
    .snap-card { display: inline-block; width: 8rem; height: 4rem; margin: 0.5rem 0.5rem 0.5rem 0; padding: 0.5rem; color: #fff; background-color: #777; }
</style>

<BitScrollablePane Horizontal Width=""22rem"" Class=""pane"" Modern
                   DragScroll=""dragScroll"" DragMomentum=""dragMomentum""
                   HorizontalWheel=""horizontalWheel"">
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""snap-card"">Card @i</div>
    }
</BitScrollablePane>";
    private readonly string example17CsharpCode = @"
private bool dragScroll = true;
private bool dragMomentum = true;
private bool horizontalWheel = true;";

    private readonly string example18RazorCode = @"
<style>
    .custom-pane {
        color: #fff;
        padding: 0.5rem;
        border-radius: 0.5rem;
        background-color: #4A4A4A;
        --bit-scp-sbs: 0.5rem;
        --bit-scp-sbc: #9FD5FF;
        --bit-scp-sbch: #C6E6FF;
        --bit-scp-sbca: #FFFFFF;
    }
</style>

<BitScrollablePane Height=""8rem""
                   Style=""border:2px solid #0078D4; border-radius:0.5rem; padding:0.5rem; background:#DEECF933"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
</BitScrollablePane>

<BitScrollablePane Height=""8rem"" Class=""custom-pane"" Modern>
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take
        shape. Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>
    <p>
        In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity
        waits to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the
        infinite possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now
        with the vibrant narratives of tomorrow.
    </p>
    <p>
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely
        and each word has the power to transform into something extraordinary. Here lies the start of something new,
        an opportunity to craft, inspire, and create.
    </p>
</BitScrollablePane>";

    private readonly string example19RazorCode = @"
<BitScrollablePane Dir=""BitDir.Rtl"" lang=""fa"" Height=""10rem"" Class=""pane"" Modern>
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
    <p>
        هر داستانی با بومی سفید آغاز می‌شود؛ فضایی آرام که منتظر پر شدن با ایده‌ها، احساس‌ها و رویاهاست.
        این واژه‌های موقت نشانهٔ آغازند؛ لحظه‌ای از امکان که هنوز خلاقیت در آن شکل نگرفته است.
    </p>
    <p>
        فعلاً این سطرها اینجا هستند تا زیبایی آغازها را به یاد بیاورند. آن‌ها سکوت پیش از سمفونی‌اند،
        بنیادی که خلاقیت شما بر آن ساخته خواهد شد.
    </p>
</BitScrollablePane>

<BitScrollablePane Dir=""BitDir.Rtl"" lang=""fa"" Horizontal Width=""20rem"" Class=""pane"" Fade>
    داستان‌ها روزگاری پیوند میان مردم را می‌بافتند، سمفونی‌ای از صداها که رویاهای مشترک را می‌ساخت.
</BitScrollablePane>";
}
