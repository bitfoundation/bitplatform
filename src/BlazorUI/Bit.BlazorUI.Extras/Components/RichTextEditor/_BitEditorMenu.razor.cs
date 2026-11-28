namespace Bit.BlazorUI;

/// <summary>
/// The menu an editor opens at a trigger it detects in its text (the slash commands, the mentions): a filter box that
/// takes the focus, and a list of what matches it. It is a combobox over a listbox: the arrows move the highlight and
/// wrap, Enter picks the highlighted item, a plain Escape closes it, and a click picks without taking the focus.
/// The editor owns what is listed, how the filter matches and what a pick does; the menu owns the highlight, the keys
/// and the ARIA, and draws with the classes it is handed, so each editor styles it in its own stylesheet.
/// </summary>
public partial class _BitEditorMenu<TItem> : ComponentBase
{
    private int _index;
    private IReadOnlyList<TItem>? _items;
    private ElementReference _inputRef;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>The root's classes: the editor's own and the app's.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The classes of the menu's parts.</summary>
    [Parameter, EditorRequired] public _BitEditorMenuClasses Classes { get; set; } = default!;

    /// <summary>What the list says when nothing matches.</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>The text in the filter box.</summary>
    [Parameter] public string? Filter { get; set; }

    /// <summary>The prefix of the ids of the list (<c>{Id}-list</c>) and its items (<c>{Id}-item-{n}</c>).</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = default!;

    /// <summary>What matches the filter. A new list moves the highlight back to its first item.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>What an item shows.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

    /// <summary>The accessible name of the filter box and the list.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Called on a plain Escape in the filter box.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Called with the filter box's text as it is typed.</summary>
    [Parameter] public EventCallback<string> OnFilterInput { get; set; }

    /// <summary>Called with the item picked, by Enter or a click.</summary>
    [Parameter] public EventCallback<TItem> OnPick { get; set; }

    /// <summary>The filter box's placeholder.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The root's inline style.</summary>
    [Parameter] public string? Style { get; set; }

    protected override void OnParametersSet()
    {
        // The highlight never points past the end of a list that shrank as the filter narrowed it.
        if (ReferenceEquals(Items, _items) is false)
        {
            _items = Items;
            _index = 0;
        }
    }

    /// <summary>
    /// Moves the focus into the filter box, so typing filters the list rather than going on in the document, and keeps
    /// the browser from acting on the keys that drive the menu there (the arrows, Enter).
    /// </summary>
    internal async Task FocusAsync()
    {
        try
        {
            await _inputRef.FocusSafelyAsync();
            await _js.InvokeVoid("BitBlazorUI.EditorMenu.bindKeys", _inputRef);
        }
        catch (JSDisconnectedException) { } // the circuit is gone: there is nothing to focus
        catch (JSException) { } // interop is unavailable: the menu still works by pointer
    }

    private Task HandleOnInput(ChangeEventArgs e) => OnFilterInput.InvokeAsync(e.Value?.ToString() ?? "");

    private async Task HandleOnKeyDownAsync(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                if (Items.Count > 0) _index = (_index + 1) % Items.Count;
                break;

            case "ArrowUp":
                if (Items.Count > 0) _index = (_index - 1 + Items.Count) % Items.Count;
                break;

            case "Enter":
                if (_index >= 0 && _index < Items.Count) await OnPick.InvokeAsync(Items[_index]);
                break;

            // Only the plain key, the one the box claims (data-bit-esc): an Escape with a modifier is the surrounding
            // surface's.
            case "Escape" when e.IsPlainEscape():
                await OnClose.InvokeAsync();
                break;
        }
    }
}

/// <summary>The classes <see cref="_BitEditorMenu{TItem}"/> draws its parts with.</summary>
public sealed record _BitEditorMenuClasses(string Input, string List, string Item, string SelectedItem, string Empty);
