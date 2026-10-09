namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSnackBar"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that shape how a snack bar looks and behaves, so one object can give every snack bar host
/// under it the same position, lifetime and look. The templates and the callbacks stay on the snack bar itself: they
/// belong to the code around the one host that shows them.
/// </remarks>
public class BitSnackBarParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSnackBar"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSnackBar value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSnackBar)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether or not automatically dismiss the snack bar.
    /// </summary>
    public bool? AutoDismiss { get; set; }

    /// <summary>
    /// How long does it take to automatically dismiss the snack bar.
    /// </summary>
    public TimeSpan? AutoDismissTime { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the snack bar.
    /// </summary>
    public BitSnackBarClassStyles? Classes { get; set; }

    /// <summary>
    /// Closes every snack bar item as soon as the app navigates somewhere else.
    /// </summary>
    public bool? ClearOnNavigation { get; set; }

    /// <summary>
    /// The accessible label of the dismiss button.
    /// </summary>
    public string? DismissAriaLabel { get; set; }

    /// <summary>
    /// The icon of the dismiss button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// The icon name of the dismiss button from the built-in Fluent UI icons.
    /// </summary>
    public string? DismissIconName { get; set; }

    /// <summary>
    /// Dismisses a snack bar item when anywhere inside it is clicked.
    /// </summary>
    public bool? DismissOnClick { get; set; }

    /// <summary>
    /// Prevents rendering the dismiss button of every snack bar item.
    /// </summary>
    public bool? HideDismiss { get; set; }

    /// <summary>
    /// Prevents rendering the countdown progress bar of the auto-dismissing snack bars.
    /// </summary>
    public bool? HideProgress { get; set; }

    /// <summary>
    /// The keyboard shortcut that moves the focus to the snack bar region, as a list of KeyboardEvent.code values.
    /// </summary>
    public string[]? Hotkey { get; set; }

    /// <summary>
    /// The leading icon of every snack bar item using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the leading icon of every snack bar item from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The maximum number of snack bar items to show at once.
    /// </summary>
    public int? MaxItems { get; set; }

    /// <summary>
    /// The maximum width of the snack bar items.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Enables the multiline mode of both title and body.
    /// </summary>
    public bool? Multiline { get; set; }

    /// <summary>
    /// Puts the newest snack bar item at the top of the stack instead of the bottom.
    /// </summary>
    public bool? NewestOnTop { get; set; }

    /// <summary>
    /// The distance of the stack from the edges of the screen.
    /// </summary>
    public string? Offset { get; set; }

    /// <summary>
    /// What happens to a new snack bar item that arrives while MaxItems is already reached.
    /// </summary>
    public BitSnackBarOverflowBehavior? OverflowBehavior { get; set; }

    /// <summary>
    /// Pauses the auto-dismiss countdown while the pointer or the keyboard focus is inside a snack bar item.
    /// </summary>
    public bool? PauseOnHover { get; set; }

    /// <summary>
    /// Pauses the auto-dismiss countdown of every snack bar item while the page is hidden.
    /// </summary>
    public bool? PauseOnPageHidden { get; set; }

    /// <summary>
    /// Pauses the auto-dismiss countdown of every snack bar item while the window does not have the focus.
    /// </summary>
    public bool? PauseOnWindowBlur { get; set; }

    /// <summary>
    /// Makes the snack bar non-dismissible in UI and removes the dismiss button.
    /// </summary>
    public bool? Persistent { get; set; }

    /// <summary>
    /// The position of the snack bars to show.
    /// </summary>
    public BitPosition? Position { get; set; }

    /// <summary>
    /// Skips showing a new snack bar while an identical one is already on screen.
    /// </summary>
    public bool? PreventDuplicates { get; set; }

    /// <summary>
    /// Draws the countdown progress bar depleting from full to empty instead of filling from empty to full.
    /// </summary>
    public bool? ReverseProgress { get; set; }

    /// <summary>
    /// A custom ARIA role for every snack bar item, overriding the one its color implies.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Renders a leading icon in each snack bar item, chosen from its color unless one is provided.
    /// </summary>
    public bool? ShowIcon { get; set; }

    /// <summary>
    /// The size of the snack bar items.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the snack bar.
    /// </summary>
    public BitSnackBarClassStyles? Styles { get; set; }

    /// <summary>
    /// Lets a snack bar item be dragged out of the way with the pointer, in either inline direction.
    /// </summary>
    public bool? SwipeToDismiss { get; set; }

    /// <summary>
    /// How far a snack bar item has to be dragged before it is dismissed, in pixels.
    /// </summary>
    public int? SwipeThreshold { get; set; }

    /// <summary>
    /// The duration in milliseconds of the enter and exit animations of the snack bar items.
    /// </summary>
    public int? TransitionDuration { get; set; }

    /// <summary>
    /// The visual variant of the snack bar items.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSnackBar"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSnackBar"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSnackBar"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitSnackBar"/>.
    /// </remarks>
    /// <param name="bitSnackBar">
    /// The <see cref="BitSnackBar"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSnackBar bitSnackBar)
    {
        if (bitSnackBar is null) return;

        UpdateBaseParameters(bitSnackBar);

        if (AutoDismiss.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(AutoDismiss), AutoDismiss.Value, static s => s.AutoDismiss, static (s, v) => s.AutoDismiss = v);
        }

        if (AutoDismissTime.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(AutoDismissTime), AutoDismissTime.Value, static s => s.AutoDismissTime, static (s, v) => s.AutoDismissTime = v);
        }

        if (Classes is not null)
        {
            bitSnackBar.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (ClearOnNavigation.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(ClearOnNavigation), ClearOnNavigation.Value, static s => s.ClearOnNavigation, static (s, v) => s.ClearOnNavigation = v);
        }

        if (DismissAriaLabel.HasValue())
        {
            bitSnackBar.TakeFromCascade(nameof(DismissAriaLabel), DismissAriaLabel, static s => s.DismissAriaLabel, static (s, v) => s.DismissAriaLabel = v);
        }

        var ownDismissIcon = bitSnackBar.HasSetAnyOf(nameof(DismissIcon), nameof(DismissIconName));

        if (DismissIcon is not null)
        {
            bitSnackBar.TakeFromCascade(nameof(DismissIcon), DismissIcon, static s => s.DismissIcon, static (s, v) => s.DismissIcon = v, outranked: ownDismissIcon);
        }

        if (DismissIconName.HasValue())
        {
            bitSnackBar.TakeFromCascade(nameof(DismissIconName), DismissIconName, static s => s.DismissIconName, static (s, v) => s.DismissIconName = v, outranked: ownDismissIcon);
        }

        if (DismissOnClick.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(DismissOnClick), DismissOnClick.Value, static s => s.DismissOnClick, static (s, v) => s.DismissOnClick = v);
        }

        if (HideDismiss.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(HideDismiss), HideDismiss.Value, static s => s.HideDismiss, static (s, v) => s.HideDismiss = v);
        }

        if (HideProgress.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(HideProgress), HideProgress.Value, static s => s.HideProgress, static (s, v) => s.HideProgress = v);
        }

        if (Hotkey is not null)
        {
            bitSnackBar.TakeFromCascade(nameof(Hotkey), Hotkey, static s => s.Hotkey, static (s, v) => s.Hotkey = v);
        }

        var ownIcon = bitSnackBar.HasSetAnyOf(nameof(Icon), nameof(IconName));

        if (Icon is not null)
        {
            bitSnackBar.TakeFromCascade(nameof(Icon), Icon, static s => s.Icon, static (s, v) => s.Icon = v, outranked: ownIcon);
        }

        if (IconName.HasValue())
        {
            bitSnackBar.TakeFromCascade(nameof(IconName), IconName, static s => s.IconName, static (s, v) => s.IconName = v, outranked: ownIcon);
        }

        if (MaxItems.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(MaxItems), MaxItems.Value, static s => s.MaxItems, static (s, v) => s.MaxItems = v);
        }

        if (MaxWidth.HasValue())
        {
            bitSnackBar.TakeFromCascade(nameof(MaxWidth), MaxWidth, static s => s.MaxWidth, static (s, v) => s.MaxWidth = v);
        }

        if (Multiline.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(Multiline), Multiline.Value, static s => s.Multiline, static (s, v) => s.Multiline = v);
        }

        if (NewestOnTop.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(NewestOnTop), NewestOnTop.Value, static s => s.NewestOnTop, static (s, v) => s.NewestOnTop = v);
        }

        if (Offset.HasValue())
        {
            bitSnackBar.TakeFromCascade(nameof(Offset), Offset, static s => s.Offset, static (s, v) => s.Offset = v);
        }

        if (OverflowBehavior.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(OverflowBehavior), OverflowBehavior.Value, static s => s.OverflowBehavior, static (s, v) => s.OverflowBehavior = v);
        }

        if (PauseOnHover.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(PauseOnHover), PauseOnHover.Value, static s => s.PauseOnHover, static (s, v) => s.PauseOnHover = v);
        }

        if (PauseOnPageHidden.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(PauseOnPageHidden), PauseOnPageHidden.Value, static s => s.PauseOnPageHidden, static (s, v) => s.PauseOnPageHidden = v);
        }

        if (PauseOnWindowBlur.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(PauseOnWindowBlur), PauseOnWindowBlur.Value, static s => s.PauseOnWindowBlur, static (s, v) => s.PauseOnWindowBlur = v);
        }

        if (Persistent.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(Persistent), Persistent.Value, static s => s.Persistent, static (s, v) => s.Persistent = v);
        }

        if (Position.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(Position), Position.Value, static s => s.Position, static (s, v) => s.Position = v);
        }

        if (PreventDuplicates.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(PreventDuplicates), PreventDuplicates.Value, static s => s.PreventDuplicates, static (s, v) => s.PreventDuplicates = v);
        }

        if (ReverseProgress.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(ReverseProgress), ReverseProgress.Value, static s => s.ReverseProgress, static (s, v) => s.ReverseProgress = v);
        }

        if (Role.HasValue())
        {
            bitSnackBar.TakeFromCascade(nameof(Role), Role, static s => s.Role, static (s, v) => s.Role = v);
        }

        if (ShowIcon.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(ShowIcon), ShowIcon.Value, static s => s.ShowIcon, static (s, v) => s.ShowIcon = v);
        }

        if (Size.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (Styles is not null)
        {
            bitSnackBar.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (SwipeToDismiss.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(SwipeToDismiss), SwipeToDismiss.Value, static s => s.SwipeToDismiss, static (s, v) => s.SwipeToDismiss = v);
        }

        if (SwipeThreshold.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(SwipeThreshold), SwipeThreshold.Value, static s => s.SwipeThreshold, static (s, v) => s.SwipeThreshold = v);
        }

        if (TransitionDuration.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(TransitionDuration), TransitionDuration.Value, static s => s.TransitionDuration, static (s, v) => s.TransitionDuration = v);
        }

        if (Variant.HasValue)
        {
            bitSnackBar.TakeFromCascade(nameof(Variant), Variant.Value, static s => s.Variant, static (s, v) => s.Variant = v);
        }
    }
}
