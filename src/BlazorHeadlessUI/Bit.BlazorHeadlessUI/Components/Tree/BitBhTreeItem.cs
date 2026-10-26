using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A node of a <see cref="BitBhTree{TValue}"/> (<c>&lt;li role="treeitem"&gt;</c>). Put the
/// node's label first and its children in a <see cref="BitBhTreeGroup{TValue}"/>.
/// <c>aria-level</c>, <c>aria-setsize</c>, <c>aria-posinset</c>, <c>aria-expanded</c>,
/// <c>aria-selected</c> / <c>aria-checked</c> and a roving <c>tabindex</c> are managed for you.
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c> (expandable items),
/// <c>data-selected</c>, <c>data-focused</c>, <c>data-disabled</c>, <c>data-level</c>,
/// <c>data-checked="true|false|mixed"</c>, and <c>--bit-bh-tree-level</c> for indentation.</para>
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public class BitBhTreeItem<TValue> : BitBhComponentBase, IBitBhCollectionItem, IHandleEvent, IDisposable
{
    private BitBhTreeGroup<TValue>? _group;
    private RenderedState? _rendered;
    private bool _focusInside;
    private bool _selfFocusPending;
    private bool _quietEvent;

    [CascadingParameter]
    private BitBhTreeContext<TValue>? TreeContext { get; set; }

    [CascadingParameter]
    private BitBhTreeItem<TValue>? Parent { get; set; }

    /// <summary>The item's value.</summary>
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    /// <summary>Plain text used for typeahead. Defaults to the rendered text.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Disables the item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Mark the item expandable before its children are loaded (lazy loading).</summary>
    [Parameter]
    public bool HasChildren { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhTreeItemRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhTreeItemRenderContext<TValue>>? Template { get; set; }

    protected override string DefaultTag => "li";

    internal BitBhTreeItem<TValue>? ParentItem => Parent;

    private BitBhTree<TValue>? Tree => TreeContext?.Tree;

    /// <summary>Depth, starting at 1.</summary>
    public int Level => Parent is null ? 1 : Parent.Level + 1;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled || (Tree?.IsTreeDisabled ?? false);

    /// <inheritdoc />
    public string? ItemText => Label ?? TreeContext?.Items.GetRenderedText(ItemId) ?? Value?.ToString();

    /// <summary>
    /// Whether the item has registered children, <see cref="HasChildren"/>, or a group that removes its
    /// children while collapsed (<see cref="BitBhTreeGroup{TValue}.Unmount"/>): those are unknown until expanded.
    /// </summary>
    public bool IsExpandable => HasChildren || _group is { Unmount: true } || (TreeContext?.ChildrenOf(this).Any() ?? false);

    internal bool IsRegistered { get; private set; }

    /// <summary>Whether every ancestor is expanded (the item is reachable).</summary>
    public bool AncestorsExpanded
    {
        get
        {
            for (var p = Parent; p is not null; p = p.Parent)
                if (TreeContext is null || !TreeContext.IsExpanded(p.Value)) return false;
            return true;
        }
    }

    private bool IsExpanded => TreeContext?.IsExpanded(Value) ?? false;

    internal void Refresh() => StateHasChanged();

    internal void SetGroup(BitBhTreeGroup<TValue>? group)
    {
        var wasExpandable = IsExpandable;
        _group = group;
        if (IsExpandable != wasExpandable) Refresh();
    }

    internal BitBhTreeGroup<TValue>? Group => _group;

    // What the tree's position-dependent attributes were rendered from; the tree re-renders the items whose
    // values moved (siblings registering or unregistering, the browser re-sorting them, the tab stop moving).
    private readonly record struct RenderedState(int Position, int Size, bool TabStop, bool Expandable);

    private RenderedState CurrentState(BitBhTreeItem<TValue>? tabStop)
    {
        var (position, size) = TreeContext?.PositionOf(this) ?? (0, 0);
        return new RenderedState(position, size, ReferenceEquals(tabStop, this), IsExpandable);
    }

    internal bool IsStale(BitBhTreeItem<TValue>? tabStop) => _rendered is { } rendered && rendered != CurrentState(tabStop);

    protected override void OnInitialized()
    {
        TreeContext?.Items.Register(this);
        IsRegistered = TreeContext is not null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && TreeContext is not null)
            await TreeContext.Items.EnsureOrderAsync(Interop);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = TreeContext;
        var state = CurrentState(Tree?.TabStopItem());
        _rendered = state;
        var checkedState = context is { Checkboxes: true } ? context.CheckedState(this) : null;
        var render = new BitBhTreeItemRenderContext<TValue>
        {
            Value = Value,
            Level = Level,
            IsExpandable = state.Expandable,
            IsExpanded = IsExpanded,
            IsSelected = context?.IsSelected(Value) ?? false,
            Checked = checkedState,
            IsFocused = context is not null && ReferenceEquals(context.FocusedItem, this),
            Disabled = IsItemDisabled,
            IsLoading = Tree?.IsLoading(Value) ?? false,
            ToggleExpanded = () => Tree?.ToggleExpandedAsync(this) ?? Task.CompletedTask,
        };

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddEventStopPropagationAttribute(31, "onkeydown", true);
        builder.AddAttribute(32, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        builder.AddEventStopPropagationAttribute(33, "onclick", true);
        builder.AddAttribute(34, "onfocus",
            On<FocusEventArgs>("onfocus", HandleFocus));
        builder.AddEventStopPropagationAttribute(35, "onfocus", true);
        builder.AddAttribute(36, "onfocusin",
            On<FocusEventArgs>("onfocusin", HandleFocusIn));
        builder.AddElementReferenceCapture(40, CaptureElement);

        builder.OpenComponent<CascadingValue<BitBhTreeItem<TValue>>>(50);
        builder.AddComponentParameter(51, "Value", this);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", (RenderFragment)(inner =>
        {
            // The state changes every render: parts such as <BitBhTreeItemToggle /> re-render with it.
            inner.OpenComponent<CascadingValue<BitBhTreeItemRenderContext<TValue>>>(0);
            inner.AddComponentParameter(1, "Value", render);
            inner.AddComponentParameter(2, "ChildContent", BitBhContent.Pick(Template, ChildContent, render));
            inner.CloseComponent();
        }));
        builder.CloseComponent();

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var context = TreeContext;
        attrs["role"] = "treeitem";
        attrs["aria-level"] = Level.ToString(CultureInfo.InvariantCulture);

        if (context is not null)
        {
            var rendered = _rendered ?? CurrentState(Tree?.TabStopItem());
            attrs["aria-setsize"] = rendered.Size.ToString(CultureInfo.InvariantCulture);
            attrs["aria-posinset"] = rendered.Position.ToString(CultureInfo.InvariantCulture);
            attrs["data-bit-bh-item"] = context.Items.Key;
            attrs["tabindex"] = rendered.TabStop ? "0" : "-1";

            if (rendered.Expandable)
            {
                SetAriaBool(attrs, "aria-expanded", IsExpanded);
                SetDataState(attrs, IsExpanded);
            }

            var selected = context.IsSelected(Value);
            if (context.Checkboxes)
            {
                var state = context.CheckedState(this);
                attrs["aria-checked"] = state is null ? "mixed" : state.Value ? "true" : "false";
                attrs["data-checked"] = state is null ? "mixed" : state.Value ? "true" : "false";
            }
            else if (context.SelectionMode != BitBhTreeSelectionMode.None)
            {
                SetAriaBool(attrs, "aria-selected", selected);
            }
            SetDataFlag(attrs, "selected", selected);
            SetDataFlag(attrs, "focused", ReferenceEquals(context.FocusedItem, this));
            if (Tree?.IsLoading(Value) == true) attrs["aria-busy"] = "true";
        }

        if (Label is not null) attrs["data-bit-bh-text"] = Label;
        if (IsItemDisabled) attrs["aria-disabled"] = "true";
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowUp ArrowDown ArrowLeft ArrowRight Home End Space Enter Ctrl+a Meta+a";
        attrs["style"] = FormattableString.Invariant($"--bit-bh-tree-level:{Level}");
        SetDataValue(attrs, "level", Level.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetDataFlag(attrs, "disabled", IsItemDisabled);
        return attrs;
    }

    // Keys pressed in interactive content of the item (a button or input in its label) bubble to it too, but
    // they belong to that content. KeyboardEventArgs has no target, so the item follows where focus is: its
    // own focus event, or a focusin bubbling up from something inside it (browsers send focus, then focusin).
    private Task HandleKeyDown(KeyboardEventArgs e)
        => _focusInside ? Task.CompletedTask : Tree?.HandleKeyDownAsync(this, e) ?? Task.CompletedTask;

    private void HandleFocus()
    {
        _selfFocusPending = true;
        _focusInside = false;
        Tree?.OnItemFocused(this);
        _quietEvent = true;
    }

    private void HandleFocusIn()
    {
        _focusInside = !_selfFocusPending;
        _selfFocusPending = false;
        _quietEvent = true;
    }

    private Task HandleClick(MouseEventArgs e) => Tree?.HandleClickAsync(this, e) ?? Task.CompletedTask;

    // Focus bookkeeping changes nothing on screen (OnItemFocused refreshes the items whose tab stop moved), and
    // focusin reaches every ancestor item: re-rendering each of them, branch and all, on every focus move would
    // be wasted. Other events re-render the item as ComponentBase does.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        _quietEvent = false;
        var task = callback.InvokeAsync(arg);
        if (_quietEvent && task.Status == TaskStatus.RanToCompletion) return Task.CompletedTask;
        var shouldAwait = task.Status is not (TaskStatus.RanToCompletion or TaskStatus.Canceled);
        StateHasChanged();
        return shouldAwait ? RenderOnCompletionAsync(task) : Task.CompletedTask;
    }

    private async Task RenderOnCompletionAsync(Task task)
    {
        try
        {
            await task;
        }
        catch when (task.IsCanceled)
        {
            return;
        }
        StateHasChanged();
    }

    public void Dispose()
    {
        if (TreeContext is null) return;
        IsRegistered = false;
        if (ReferenceEquals(TreeContext.FocusedItem, this)) TreeContext.FocusedItem = Parent;
        TreeContext.Items.Unregister(this);
    }
}
