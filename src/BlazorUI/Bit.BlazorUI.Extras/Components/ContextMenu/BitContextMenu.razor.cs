using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A context menu: the menu of a <see cref="BitMenuButton{TItem}"/> opened over an area of the page rather than from a
/// button - where the area is right-clicked, or, from the keyboard, with Shift+F10 or the Menu key while the focus is
/// inside it.
/// </summary>
/// <remarks>
/// The area is the <see cref="ChildContent"/>, and the menu is declared the way a menu button's is: through
/// <see cref="Items"/> (with <see cref="NameSelectors"/> for items of a class of the app's own), or through
/// <see cref="BitMenuButtonOption"/> children in <see cref="Options"/>. The menu keeps everything a menu button's has -
/// the arrow keys, typeahead, submenus, check and radio items - and Escape, Tab or an item hands the focus back to where
/// it was in the area.
/// <br />
/// While the menu is open, a right-click on the area moves it to the new point, and a click or a right-click anywhere
/// else on the page dismisses it, the page getting that click as well. A <see cref="BitComponentBase.Disabled"/> context
/// menu leaves the area to the browser's own menu.
/// </remarks>
public partial class BitContextMenu<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem> : BitComponentBase where TItem : class
{
    private const string DefaultAriaLabel = "Context menu";



    [Inject] private IJSRuntime _js { get; set; } = default!;

    private BitMenuButton<TItem> _menuButton = default!;
    private string _menuButtonId = default!;

    // Set by the Shift+F10 or Menu key that opened the menu, for the contextmenu event the browser dispatches for the
    // same key - Chromium does, at the middle of the focused element - which must not open the menu a second time. Every
    // other key clears it, and so does that event, the one it is waiting for.
    private bool _openedByKey;



    /// <summary>
    /// The default values of the parameters of the context menu, cascaded through a <see cref="BitParams"/>. A value set
    /// on the context menu itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitContextMenuParams.ParamName)]
    public BitContextMenuParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the area the menu is opened over.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the menu closes once one of its items is clicked. Default: true.
    /// </summary>
    [Parameter] public bool CloseOnItemClick { get; set; } = true;

    /// <summary>
    /// Whether the menu is open.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The items of the menu.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The template the items of the menu are rendered with.
    /// </summary>
    [Parameter] public RenderFragment<TItem?>? ItemTemplate { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the menu. Those of the parts of the menu button's own button do
    /// nothing here, since the button is never shown.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? MenuClasses { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the menu. Those of the parts of the menu button's own button do
    /// nothing here, since the button is never shown.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? MenuStyles { get; set; }

    /// <summary>
    /// Names the members of a custom item class to read the text, icon, children and the rest of an item from.
    /// </summary>
    [Parameter] public BitMenuButtonNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback for when an item of the menu is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// The items of the menu, declared as <see cref="BitMenuButtonOption"/> components.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }



    /// <summary>
    /// Opens the menu at the given point of the viewport, in CSS pixels, or moves it there when it is open already. It
    /// is still placed to stay on the screen, and the focus is taken into it.
    /// </summary>
    public Task OpenAt(double x, double y) => Disabled ? Task.CompletedTask : _menuButton.OpenAt(x, y);

    /// <summary>
    /// Opens the menu at the point of a mouse event (its clientX and clientY), or moves it there when it is open
    /// already.
    /// </summary>
    public Task OpenAt(MouseEventArgs e) => OpenAt(e.ClientX, e.ClientY);



    protected override string RootElementClass => "bit-ctm";

    protected override void OnInitialized()
    {
        _menuButtonId = $"{_Id}-mnb";

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    private async Task HandleOnContextMenu(MouseEventArgs e)
    {
        if (Disabled) return;

        // The browser's own event for the key the menu was just opened with: the menu is already where the key put
        // it. A right-click (the secondary button) is the user's, and moves it as any other does.
        if (_openedByKey)
        {
            _openedByKey = false;

            if (e.Button != 2) return;
        }

        await OpenAt(e);
    }

    // Shift+F10 and the Menu key are what opens a context menu from the keyboard. Not every browser dispatches a
    // contextmenu event for them (none does on macOS, which has no such key), so they are taken here, and the menu is
    // opened at the element that has the focus.
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        _openedByKey = Disabled is false && (e.Key == "ContextMenu" || (e.Key == "F10" && e.ShiftKey));

        if (_openedByKey is false) return;

        var point = await _js.BitExtrasGetContextMenuPoint(RootElement);

        if (point is not { Length: 2 } || IsDisposed) return;

        await OpenAt(point[0], point[1]);
    }

    private async Task HandleIsOpenChanged(bool isOpen)
    {
        await AssignIsOpen(isOpen);
    }

    // The menu button raises its OnClick for its own button too, which is never shown here, so only an item is passed on.
    private async Task HandleOnItemClick(TItem? item)
    {
        if (item is null) return;

        await OnItemClick.InvokeAsync(item);
    }
}
