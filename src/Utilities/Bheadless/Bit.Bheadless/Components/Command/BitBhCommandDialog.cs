using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A command palette: a <see cref="BitBhCommand"/> inside a modal <see cref="BitBhDialog"/>,
/// opened with a global shortcut (⌘K / Ctrl+K by default). Selecting an item
/// closes the palette; Escape and outside clicks dismiss it; focus returns to
/// where it was.
///
/// <code>
/// &lt;BitBhCommandDialog @bind-Open="paletteOpen" Label="Command palette"
///                  OverlayClass="overlay" ContentClass="palette"&gt;
///     &lt;BitBhCommandInput placeholder="Search…" /&gt;
///     &lt;BitBhCommandList&gt;…&lt;/BitBhCommandList&gt;
/// &lt;/BitBhCommandDialog&gt;
/// </code>
/// </summary>
public class BitBhCommandDialog : ComponentBase
{
    private bool _open;

    /// <summary>Whether the palette is open. Supplying it makes it controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Global shortcut that toggles the palette (comma-separated alternatives). Null disables it. Default: <c>"mod+k"</c>.</summary>
    [Parameter]
    public string? Shortcut { get; set; } = "mod+k";

    /// <summary>Accessible label of the palette.</summary>
    [Parameter]
    public string Label { get; set; } = "Command palette";

    /// <summary>Class of the overlay behind the palette.</summary>
    [Parameter]
    public string? OverlayClass { get; set; }

    /// <summary>Class of the dialog panel.</summary>
    [Parameter]
    public string? ContentClass { get; set; }

    /// <summary>Class of the <see cref="BitBhCommand"/> root.</summary>
    [Parameter]
    public string? CommandClass { get; set; }

    /// <summary>Command options forwarded to <see cref="BitBhCommand"/>.</summary>
    [Parameter]
    public bool ShouldFilter { get; set; } = true;

    /// <summary>Order results by relevance. See <see cref="BitBhCommand.Sort"/>.</summary>
    [Parameter]
    public bool Sort { get; set; }

    /// <summary>Whether arrow keys wrap.</summary>
    [Parameter]
    public bool Loop { get; set; }

    /// <summary>The search text (controlled when supplied). See <see cref="BitBhCommand.Search"/>.</summary>
    [Parameter]
    public string? Search { get; set; }

    /// <summary>Fires when the search text changes (e.g. to query a server with <see cref="ShouldFilter"/> off).</summary>
    [Parameter]
    public EventCallback<string> SearchChanged { get; set; }

    /// <summary>The value of the highlighted item. See <see cref="BitBhCommand.Value"/>.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Fires when the highlighted item changes.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>The current page (controlled when supplied). See <see cref="BitBhCommand.Page"/>.</summary>
    [Parameter]
    public string? Page { get; set; }

    /// <summary>Fires when the page changes.</summary>
    [Parameter]
    public EventCallback<string?> PageChanged { get; set; }

    /// <summary>Custom scorer. See <see cref="BitBhCommand.Filter"/>.</summary>
    [Parameter]
    public Func<string, string, IReadOnlyList<string>, double>? Filter { get; set; }

    /// <summary>Announcement of the result count. See <see cref="BitBhCommand.ResultsAnnouncement"/>.</summary>
    [Parameter]
    public Func<int, string>? ResultsAnnouncement { get; set; }

    /// <summary>Close the palette after an item is selected (navigating to a page keeps it open). Default: true.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>Input, list and items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool _controlled;
    private readonly HashSet<string> _provided = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _controlled = parameters.TryGetValue<bool>(nameof(Open), out _);
        _provided.Clear();
        foreach (var parameter in parameters) _provided.Add(parameter.Name);
        return base.SetParametersAsync(parameters);
    }

    private bool IsOpen => _controlled ? Open : _open;

    private async Task SetOpenAsync(bool value)
    {
        if (value == IsOpen) return;
        if (!_controlled) _open = value;
        await OpenChanged.InvokeAsync(value);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(Shortcut))
        {
            builder.OpenComponent<BitBhHotkey>(0);
            builder.AddComponentParameter(1, nameof(BitBhHotkey.Keys), Shortcut);
            builder.AddComponentParameter(2, nameof(BitBhHotkey.OnTriggered), EventCallback.Factory.Create(this, () => SetOpenAsync(!IsOpen)));
            builder.AddComponentParameter(3, nameof(BitBhHotkey.AllowInInputs), true);
            builder.CloseComponent();
        }

        builder.OpenComponent<BitBhDialog>(10);
        builder.AddComponentParameter(11, nameof(BitBhDialog.Open), IsOpen);
        builder.AddComponentParameter(12, nameof(BitBhDialog.OpenChanged), EventCallback.Factory.Create<bool>(this, SetOpenAsync));
        builder.AddComponentParameter(13, nameof(BitBhDialog.ChildContent), (RenderFragment)(dialog =>
        {
            dialog.OpenComponent<BitBhDialogBackdrop>(0);
            if (OverlayClass is not null) dialog.AddComponentParameter(1, "class", OverlayClass);
            dialog.CloseComponent();

            dialog.OpenComponent<BitBhDialogPanel>(2);
            if (ContentClass is not null) dialog.AddComponentParameter(3, "class", ContentClass);
            dialog.AddComponentParameter(4, "aria-label", Label);
            dialog.AddComponentParameter(5, nameof(BitBhDialogPanel.ChildContent), (RenderFragment)(panel =>
            {
                panel.OpenComponent<BitBhCommand>(0);
                panel.AddComponentParameter(1, nameof(BitBhCommand.Label), Label);
                panel.AddComponentParameter(2, nameof(BitBhCommand.ShouldFilter), ShouldFilter);
                panel.AddComponentParameter(3, nameof(BitBhCommand.Sort), Sort);
                panel.AddComponentParameter(4, nameof(BitBhCommand.Loop), Loop);
                if (CommandClass is not null) panel.AddComponentParameter(5, "class", CommandClass);
                panel.AddComponentParameter(6, nameof(BitBhCommand.ChildContent), ChildContent);
                panel.AddComponentParameter(7, nameof(BitBhCommand.CloseOnSelect), CloseOnSelect);
                // Controlled values are forwarded only when supplied, so the command stays uncontrolled otherwise.
                if (_provided.Contains(nameof(Search))) panel.AddComponentParameter(8, nameof(BitBhCommand.Search), Search);
                if (SearchChanged.HasDelegate) panel.AddComponentParameter(9, nameof(BitBhCommand.SearchChanged), SearchChanged);
                if (_provided.Contains(nameof(Value))) panel.AddComponentParameter(10, nameof(BitBhCommand.Value), Value);
                if (ValueChanged.HasDelegate) panel.AddComponentParameter(11, nameof(BitBhCommand.ValueChanged), ValueChanged);
                if (_provided.Contains(nameof(Page))) panel.AddComponentParameter(12, nameof(BitBhCommand.Page), Page);
                if (PageChanged.HasDelegate) panel.AddComponentParameter(13, nameof(BitBhCommand.PageChanged), PageChanged);
                if (Filter is not null) panel.AddComponentParameter(14, nameof(BitBhCommand.Filter), Filter);
                if (ResultsAnnouncement is not null) panel.AddComponentParameter(15, nameof(BitBhCommand.ResultsAnnouncement), ResultsAnnouncement);
                panel.CloseComponent();
            }));
            dialog.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
