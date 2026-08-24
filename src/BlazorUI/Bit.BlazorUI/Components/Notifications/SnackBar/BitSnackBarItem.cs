namespace Bit.BlazorUI;

/// <summary>
/// A class to represent each snack bar item.
/// </summary>
/// <remarks>
/// An item is handed to <see cref="BitSnackBar.Show(BitSnackBarItem)"/> and stays the handle of the snack bar
/// it opened: it is what <see cref="BitSnackBar.Close(BitSnackBarItem)"/>, <see cref="BitSnackBar.Update(BitSnackBarItem)"/>,
/// <see cref="BitSnackBar.Pause(BitSnackBarItem)"/> and <see cref="BitSnackBar.Resume(BitSnackBarItem)"/> take, and what
/// the <see cref="BitSnackBar.OnDismiss"/> callback reports back.
/// <br />
/// Every member here overrides the matching parameter of the host <see cref="BitSnackBar"/> for this one item only,
/// so a single snack bar host can show items that differ in color, icon, lifetime and content.
/// </remarks>
public class BitSnackBarItem
{
    /// <summary>
    /// The unique identifier of the snack bar item.
    /// </summary>
    public readonly Guid Id = Guid.NewGuid();

    /// <summary>
    /// The content of the action area of this snack bar item, rendered under its body.
    /// </summary>
    /// <remarks>
    /// This takes the place of the host's <see cref="BitSnackBar.ActionsTemplate"/> for this item.
    /// An action is the point of a snack bar that reports something the user can still act on ("Undo", "Retry"),
    /// which is why the interactive content belongs here rather than inside the announced text.
    /// </remarks>
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// How long it takes to automatically dismiss this specific snack bar item.
    /// </summary>
    /// <remarks>
    /// Overrides the <see cref="BitSnackBar.AutoDismissTime"/> of the host for this item only, and is only in
    /// effect while the host has <see cref="BitSnackBar.AutoDismiss"/> enabled and neither the host nor this item
    /// is persistent.
    /// </remarks>
    public TimeSpan? AutoDismissTime { get; set; }

    /// <summary>
    /// The body text of the snack bar item.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// The color theme of the snack bar item.
    /// </summary>
    /// <remarks>
    /// The color also decides the default icon and the default live-region role of the item: the colors that
    /// report a problem (Warning, SevereWarning, Error) are announced as an <c>alert</c>, everything else as a
    /// <c>status</c>.
    /// </remarks>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Custom CSS class to apply to the snack bar item.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Custom CSS style to apply to the snack bar item.
    /// </summary>
    public string? CssStyle { get; set; }

    /// <summary>
    /// An arbitrary payload to carry along with the snack bar item.
    /// </summary>
    /// <remarks>
    /// Nothing in the component reads this value. It is a place to keep whatever the callbacks of the item need
    /// (the entity the notification is about, a correlation id, ...) without a lookup table on the consumer side.
    /// </remarks>
    public object? Data { get; set; }

    /// <summary>
    /// Prevents rendering the leading icon of this specific snack bar item.
    /// </summary>
    /// <remarks>
    /// Only relevant while the host has <see cref="BitSnackBar.ShowIcon"/> enabled.
    /// </remarks>
    public bool HideIcon { get; set; }

    /// <summary>
    /// The leading icon of this snack bar item using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <example>
    /// Bootstrap: <c>Icon = BitIconInfo.Bi("check-circle-fill")</c>
    /// FontAwesome: <c>Icon = BitIconInfo.Fa("solid circle-check")</c>
    /// Custom CSS: <c>Icon = BitIconInfo.Css("my-icon-class")</c>
    /// </example>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the leading icon of this snack bar item from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// If unset, the icon is selected automatically based on <see cref="Color"/>.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// A callback that is invoked when this snack bar item is clicked.
    /// </summary>
    /// <remarks>
    /// Runs before the host's <see cref="BitSnackBar.OnItemClick"/>, and before the item is dismissed when the
    /// host has <see cref="BitSnackBar.DismissOnClick"/> enabled.
    /// </remarks>
    public Func<BitSnackBarItem, Task>? OnClick { get; set; }

    /// <summary>
    /// A callback that is invoked when this snack bar item is dismissed.
    /// </summary>
    /// <remarks>
    /// Runs before the host's <see cref="BitSnackBar.OnDismiss"/>, whatever dismissed the item: the dismiss
    /// button, the Escape key, a click, the auto-dismiss countdown, a <see cref="BitSnackBar.Close"/> call, or
    /// the host making room for a newer item.
    /// </remarks>
    public Func<BitSnackBarItem, Task>? OnDismiss { get; set; }

    /// <summary>
    /// Makes this specific snack bar item non-dismissible and removes its dismiss button.
    /// </summary>
    /// <remarks>
    /// A persistent item also opts out of the auto-dismiss countdown and of its progress bar, so it stays until
    /// the code that opened it closes it through <see cref="BitSnackBar.Close(BitSnackBarItem)"/>.
    /// </remarks>
    public bool Persistent { get; set; }

    /// <summary>
    /// A custom ARIA role for this snack bar item, overriding the one its <see cref="Color"/> implies.
    /// </summary>
    /// <remarks>
    /// The two roles that make a snack bar announce itself are <c>status</c> (polite - waits for a pause in what
    /// the screen reader is saying) and <c>alert</c> (assertive - interrupts it). Reserve <c>alert</c> for the
    /// notifications the user has to hear about right now.
    /// </remarks>
    public string? Role { get; set; }

    /// <summary>
    /// The title text of the snack bar item.
    /// </summary>
    public string Title { get; set; } = default!;



    /// <summary>
    /// The countdown state of the item, owned by the host snack bar.
    /// </summary>
    /// <remarks>
    /// The countdown is a cancellable delay rather than a timer, so the host can pause it (cancel it and keep
    /// what was left of it) and resume it later without leaking a callback into a component that is already gone.
    /// </remarks>
    internal CancellationTokenSource? _cts;
    internal bool _paused;
    internal bool _hovered;
    internal bool _dismissing;
    internal TimeSpan _remaining;
    internal DateTimeOffset _dueAt;

    /// <summary>
    /// Counts the countdowns this item has been given, and keys the progress bar that draws the current one.
    /// </summary>
    /// <remarks>
    /// A CSS animation does not start over because the element it runs on was re-rendered, so a restarted countdown
    /// would otherwise be drawn by a bar still standing wherever the previous one had got to. Bumping this replaces
    /// the element, which is what starts its animation again.
    /// </remarks>
    internal int _generation;
}
