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

        if (AutoDismiss.HasValue && bitSnackBar.HasNotBeenSet(nameof(AutoDismiss)))
        {
            bitSnackBar.AutoDismiss = AutoDismiss.Value;
        }

        if (AutoDismissTime.HasValue && bitSnackBar.HasNotBeenSet(nameof(AutoDismissTime)))
        {
            bitSnackBar.AutoDismissTime = AutoDismissTime.Value;
        }

        if (Classes is not null && bitSnackBar.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitSnackBar.Classes, Classes) is false)
        {
            bitSnackBar.Classes = Classes;

            bitSnackBar.ClassBuilder.Reset();
        }

        if (ClearOnNavigation.HasValue && bitSnackBar.HasNotBeenSet(nameof(ClearOnNavigation)))
        {
            bitSnackBar.ClearOnNavigation = ClearOnNavigation.Value;
        }

        if (DismissAriaLabel.HasValue() && bitSnackBar.HasNotBeenSet(nameof(DismissAriaLabel)))
        {
            bitSnackBar.DismissAriaLabel = DismissAriaLabel;
        }

        if (DismissIcon is not null && bitSnackBar.HasNotBeenSet(nameof(DismissIcon)))
        {
            bitSnackBar.DismissIcon = DismissIcon;
        }

        if (DismissIconName.HasValue() && bitSnackBar.HasNotBeenSet(nameof(DismissIconName)))
        {
            bitSnackBar.DismissIconName = DismissIconName;
        }

        if (DismissOnClick.HasValue && bitSnackBar.HasNotBeenSet(nameof(DismissOnClick)))
        {
            bitSnackBar.DismissOnClick = DismissOnClick.Value;
        }

        if (HideDismiss.HasValue && bitSnackBar.HasNotBeenSet(nameof(HideDismiss)))
        {
            bitSnackBar.HideDismiss = HideDismiss.Value;
        }

        if (HideProgress.HasValue && bitSnackBar.HasNotBeenSet(nameof(HideProgress)))
        {
            bitSnackBar.HideProgress = HideProgress.Value;
        }

        if (Hotkey is not null && bitSnackBar.HasNotBeenSet(nameof(Hotkey)))
        {
            bitSnackBar.Hotkey = Hotkey;
        }

        if (Icon is not null && bitSnackBar.HasNotBeenSet(nameof(Icon)))
        {
            bitSnackBar.Icon = Icon;
        }

        if (IconName.HasValue() && bitSnackBar.HasNotBeenSet(nameof(IconName)))
        {
            bitSnackBar.IconName = IconName;
        }

        if (MaxItems.HasValue && bitSnackBar.HasNotBeenSet(nameof(MaxItems)))
        {
            bitSnackBar.MaxItems = MaxItems.Value;
        }

        if (MaxWidth.HasValue() && bitSnackBar.HasNotBeenSet(nameof(MaxWidth)) && bitSnackBar.MaxWidth != MaxWidth)
        {
            bitSnackBar.MaxWidth = MaxWidth;

            bitSnackBar.StyleBuilder.Reset();
        }

        if (Multiline.HasValue && bitSnackBar.HasNotBeenSet(nameof(Multiline)))
        {
            bitSnackBar.Multiline = Multiline.Value;
        }

        if (NewestOnTop.HasValue && bitSnackBar.HasNotBeenSet(nameof(NewestOnTop)))
        {
            bitSnackBar.NewestOnTop = NewestOnTop.Value;
        }

        if (Offset.HasValue() && bitSnackBar.HasNotBeenSet(nameof(Offset)) && bitSnackBar.Offset != Offset)
        {
            bitSnackBar.Offset = Offset;

            bitSnackBar.StyleBuilder.Reset();
        }

        if (OverflowBehavior.HasValue && bitSnackBar.HasNotBeenSet(nameof(OverflowBehavior)))
        {
            bitSnackBar.OverflowBehavior = OverflowBehavior.Value;
        }

        if (PauseOnHover.HasValue && bitSnackBar.HasNotBeenSet(nameof(PauseOnHover)))
        {
            bitSnackBar.PauseOnHover = PauseOnHover.Value;
        }

        if (PauseOnPageHidden.HasValue && bitSnackBar.HasNotBeenSet(nameof(PauseOnPageHidden)))
        {
            bitSnackBar.PauseOnPageHidden = PauseOnPageHidden.Value;
        }

        if (PauseOnWindowBlur.HasValue && bitSnackBar.HasNotBeenSet(nameof(PauseOnWindowBlur)))
        {
            bitSnackBar.PauseOnWindowBlur = PauseOnWindowBlur.Value;
        }

        if (Persistent.HasValue && bitSnackBar.HasNotBeenSet(nameof(Persistent)))
        {
            bitSnackBar.Persistent = Persistent.Value;
        }

        if (Position.HasValue && bitSnackBar.HasNotBeenSet(nameof(Position)) && bitSnackBar.Position != Position)
        {
            bitSnackBar.Position = Position.Value;

            bitSnackBar.ClassBuilder.Reset();
        }

        if (PreventDuplicates.HasValue && bitSnackBar.HasNotBeenSet(nameof(PreventDuplicates)))
        {
            bitSnackBar.PreventDuplicates = PreventDuplicates.Value;
        }

        if (ReverseProgress.HasValue && bitSnackBar.HasNotBeenSet(nameof(ReverseProgress)))
        {
            bitSnackBar.ReverseProgress = ReverseProgress.Value;
        }

        if (Role.HasValue() && bitSnackBar.HasNotBeenSet(nameof(Role)))
        {
            bitSnackBar.Role = Role;
        }

        if (ShowIcon.HasValue && bitSnackBar.HasNotBeenSet(nameof(ShowIcon)))
        {
            bitSnackBar.ShowIcon = ShowIcon.Value;
        }

        if (Size.HasValue && bitSnackBar.HasNotBeenSet(nameof(Size)))
        {
            bitSnackBar.Size = Size.Value;
        }

        if (Styles is not null && bitSnackBar.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitSnackBar.Styles, Styles) is false)
        {
            bitSnackBar.Styles = Styles;

            bitSnackBar.StyleBuilder.Reset();
        }

        if (SwipeToDismiss.HasValue && bitSnackBar.HasNotBeenSet(nameof(SwipeToDismiss)))
        {
            bitSnackBar.SwipeToDismiss = SwipeToDismiss.Value;
        }

        if (SwipeThreshold.HasValue && bitSnackBar.HasNotBeenSet(nameof(SwipeThreshold)))
        {
            bitSnackBar.SwipeThreshold = SwipeThreshold.Value;
        }

        if (TransitionDuration.HasValue && bitSnackBar.HasNotBeenSet(nameof(TransitionDuration)) && bitSnackBar.TransitionDuration != TransitionDuration)
        {
            bitSnackBar.TransitionDuration = TransitionDuration.Value;

            bitSnackBar.StyleBuilder.Reset();
        }

        if (Variant.HasValue && bitSnackBar.HasNotBeenSet(nameof(Variant)))
        {
            bitSnackBar.Variant = Variant.Value;
        }
    }
}
