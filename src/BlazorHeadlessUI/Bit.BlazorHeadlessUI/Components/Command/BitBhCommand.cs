using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless command menu / command palette (in the spirit of cmdk): a search
/// input over a filterable, keyboard-navigable list of actions, with groups,
/// nested pages and an empty state. Use it inline or inside a
/// <see cref="BitBhCommandDialog"/> (⌘K / Ctrl+K palette).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Built-in fuzzy filtering and ranking</b> (<see cref="BitBhFilter.Score"/>), or plug in <see cref="Filter"/>, or turn it off (<see cref="ShouldFilter"/>) for server-side search.</item>
///   <item><b>Keyword aliases</b> per item, <b>groups</b> that hide when empty, separators hidden while searching.</item>
///   <item><b>Nested command menus</b> — <see cref="BitBhCommandPage"/> + <c>PushPageAsync</c>; Backspace on an empty search goes back.</item>
///   <item><b>Keyboard</b> — ArrowUp / ArrowDown, Ctrl+N / Ctrl+P, Alt+ArrowUp / Alt+ArrowDown (between groups), PageUp / PageDown, Home / End (with Ctrl), Enter.</item>
///   <item><b>ARIA</b> — combobox input + listbox with <c>aria-activedescendant</c>; result counts are announced.</item>
/// </list>
///
/// <code>
/// &lt;BitBhCommand Label="Command menu"&gt;
///     &lt;BitBhCommandInput placeholder="Type a command…" /&gt;
///     &lt;BitBhCommandList&gt;
///         &lt;BitBhCommandEmpty&gt;No results.&lt;/BitBhCommandEmpty&gt;
///         &lt;BitBhCommandGroup Heading="Suggestions"&gt;
///             &lt;BitBhCommandItem OnSelect="OpenCalendar"&gt;Calendar&lt;/BitBhCommandItem&gt;
///             &lt;BitBhCommandItem Keywords="@(["emoji"])"&gt;Search emoji&lt;/BitBhCommandItem&gt;
///         &lt;/BitBhCommandGroup&gt;
///     &lt;/BitBhCommandList&gt;
/// &lt;/BitBhCommand&gt;
/// </code>
/// </summary>
public class BitBhCommand : BitBhComponentBase, IDisposable
{
    private BitBhCommandContext? _context;
    private string _search = string.Empty;
    // The page stack, kept in both modes: a controlled Page tells only where the palette is, and
    // Backspace has to know where it came from.
    private readonly List<string> _pages = new();
    private BitBhDelayedAction? _announce;

    [CascadingParameter]
    private IBitBhCloseContext? CloseContext { get; set; }

    /// <summary>Accessible label of the command menu (input and list).</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The search text. Supplying it makes the search controlled (use <c>@bind-Search</c>).</summary>
    [Parameter]
    public string? Search { get; set; }

    /// <summary>Two-way binding callback for <see cref="Search"/>.</summary>
    [Parameter]
    public EventCallback<string> SearchChanged { get; set; }

    /// <summary>The value of the highlighted item (use <c>@bind-Value</c> to observe / drive it).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>The current page. Supplying it makes paging controlled (use <c>@bind-Page</c>).</summary>
    [Parameter]
    public string? Page { get; set; }

    /// <summary>Two-way binding callback for <see cref="Page"/>.</summary>
    [Parameter]
    public EventCallback<string?> PageChanged { get; set; }

    /// <summary>Custom scorer: (item value, search, keywords) → score (0 hides the item).</summary>
    [Parameter]
    public Func<string, string, IReadOnlyList<string>, double>? Filter { get; set; }

    /// <summary>Filter items by the search. Turn off when you filter yourself (e.g. server-side). Default: true.</summary>
    [Parameter]
    public bool ShouldFilter { get; set; } = true;

    /// <summary>
    /// Order results by relevance (via the CSS <c>order</c> property — make the list and
    /// groups flex columns). Default: false (author order).
    /// </summary>
    [Parameter]
    public bool Sort { get; set; }

    /// <summary>Whether arrow keys wrap. Default: false.</summary>
    [Parameter]
    public bool Loop { get; set; }

    /// <summary>Text announced to screen readers when the results change. Default: "{n} results".</summary>
    [Parameter]
    public Func<int, string>? ResultsAnnouncement { get; set; }

    /// <summary>Close an enclosing dialog / popover after an item is selected. Default: true.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>Input, list, groups, items, pages.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal BitBhCommandContext Context => _context ??= new BitBhCommandContext(this, Interop, ComponentId);

    internal string CurrentSearch => IsProvided(nameof(Search)) ? Search ?? string.Empty : _search;

    internal string? CurrentPage => IsProvided(nameof(Page)) ? Page : _pages.LastOrDefault();

    internal IReadOnlyList<string> PageStack => _pages;

    internal double ScoreItem(string value, string search, IReadOnlyList<string> keywords)
        => Filter is not null ? Filter(value, search, keywords) : BitBhFilter.Score(value, search, keywords);

    private string? _lastSearch;

    protected override void OnParametersSet()
    {
        if (CurrentSearch != _lastSearch)
        {
            _lastSearch = CurrentSearch;
            Context.Refilter();
        }
        if (IsProvided(nameof(Page)) && Page != _pages.LastOrDefault())
        {
            var at = Page is null ? -1 : _pages.LastIndexOf(Page);
            if (Page is null) _pages.Clear();
            else if (at >= 0) _pages.RemoveRange(at + 1, _pages.Count - at - 1);
            else _pages.Add(Page);
        }
        // An item the controlled Value names may not have registered yet (first render, a page
        // change): OnAfterRenderAsync highlights it once it has, rather than the first item.
        if (IsProvided(nameof(Value)) && Value is not null && Value != Context.ActiveItem?.ItemValue)
        {
            var match = Context.Items.Items.FirstOrDefault(i => i.ItemValue == Value);
            if (match is not null) Context.SetActive(match);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await Context.Items.EnsureOrderAsync(Interop);
        if (Context.ActiveItem is null)
        {
            var wanted = IsProvided(nameof(Value)) && Value is not null
                ? Context.NavigationOrder().FirstOrDefault(i => i.ItemValue == Value)
                : null;
            Context.SetActive(wanted ?? Context.NavigationOrder().FirstOrDefault(), scroll: false);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);

        if (!string.IsNullOrEmpty(Label))
        {
            builder.OpenElement(40, "label");
            builder.AddAttribute(41, "id", Context.LabelId);
            builder.AddAttribute(42, "for", Context.InputId);
            builder.AddAttribute(43, "style", BitBhVisuallyHidden.Style);
            builder.AddContent(44, Label);
            builder.CloseElement();
        }

        builder.OpenComponent<CascadingValue<BitBhCommandContext>>(50);
        builder.AddComponentParameter(51, "Value", Context);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-bit-bh-command"] = "";
        if (CurrentPage is { } page) attrs["data-page"] = page;
        return attrs;
    }

    // ── State changes ────────────────────────────────────────────────────────

    internal async Task SetSearchAsync(string search)
    {
        if (search == CurrentSearch) return;
        if (!IsProvided(nameof(Search))) _search = search;
        await SearchChanged.InvokeAsync(search);
        _lastSearch = CurrentSearch;
        Context.Refilter();
        ScheduleAnnouncement();
    }

    private void ScheduleAnnouncement()
    {
        _announce ??= new BitBhDelayedAction(a => InvokeAsync(a));
        _announce.Schedule(600, async () =>
        {
            var count = Context.VisibleCount;
            var text = ResultsAnnouncement?.Invoke(count) ?? (count == 1 ? "1 result" : $"{count} results");
            var announcer = GetService<IBitBhAnnouncer>() ?? new BitBhAnnouncer(Interop);
            await announcer.AnnounceAsync(text);
        });
    }

    internal void OnActiveChanged(BitBhCommandItem? item)
    {
        var value = item?.ItemValue;
        // A highlight the controlled Value asked for is not reported back to it.
        if (ValueChanged.HasDelegate && (IsProvided(nameof(Value)) is false || value != Value))
            _ = ValueChanged.InvokeAsync(value);
    }

    internal async Task PushPageAsync(string page)
    {
        _pages.Add(page);
        await PageChanged.InvokeAsync(page);
        await SetSearchAsync(string.Empty);
        Context.SetActive(null);
        StateHasChanged();
    }

    internal async Task PopPageAsync()
    {
        if (CurrentPage is null) return;
        if (_pages.Count > 0) _pages.RemoveAt(_pages.Count - 1);
        await PageChanged.InvokeAsync(_pages.LastOrDefault());
        Context.SetActive(null);
        StateHasChanged();
    }

    internal async Task OnItemSelectedAsync(string? pageBefore)
    {
        // An item that navigated to another page isn't a final choice: stay open.
        if (CurrentPage != pageBefore) return;
        if (CloseOnSelect && CloseContext is not null)
            await CloseContext.CloseAsync();
    }

    internal async Task HandleInputKeyDownAsync(KeyboardEventArgs e)
    {
        var ctx = Context;
        switch (e.Key)
        {
            case "ArrowDown" when e.AltKey:
                ctx.MoveToGroup(+1);
                break;
            case "ArrowUp" when e.AltKey:
                ctx.MoveToGroup(-1);
                break;
            case "ArrowDown":
                ctx.Move(+1, Loop);
                break;
            case "ArrowUp":
                ctx.Move(-1, Loop);
                break;
            case "n" or "j" when e.CtrlKey:
                ctx.Move(+1, Loop);
                break;
            case "p" or "k" when e.CtrlKey:
                ctx.Move(-1, Loop);
                break;
            case "PageDown":
                ctx.Move(+10, loop: false);
                break;
            case "PageUp":
                ctx.Move(-10, loop: false);
                break;
            case "Home" when e.CtrlKey || e.MetaKey:
                ctx.First();
                break;
            case "End" when e.CtrlKey || e.MetaKey:
                ctx.Last();
                break;
            case "Enter":
                await ctx.SelectActiveAsync();
                break;
            case "Backspace" when string.IsNullOrEmpty(CurrentSearch):
                await PopPageAsync();
                break;
        }
    }

    public void Dispose() => _announce?.Dispose();
}
