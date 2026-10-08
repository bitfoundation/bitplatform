namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitOverlay"/> component.
/// </summary>
/// <remarks>
/// What each overlay holds for itself - whether it is open (<see cref="BitOverlay.IsOpen"/>,
/// <see cref="BitOverlay.DefaultIsOpen"/>), its content, the element reference of its scroller
/// (<see cref="BitOverlay.ScrollerElement"/>) and its callbacks - is not a group default, so it is not carried here.
/// </remarks>
public class BitOverlayParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitOverlay"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitOverlay value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitOverlay)}";



    public string Name => ParamName;



    /// <summary>
    /// Positions the overlay absolute instead of fixed, so that it covers the element it was declared inside of rather
    /// than the screen.
    /// </summary>
    public bool? AbsolutePosition { get; set; }

    /// <summary>
    /// Takes the scrolling away from the scroller behind the overlay while it is open and hands it back once it closes.
    /// </summary>
    public bool? AutoToggleScroll { get; set; }

    /// <summary>
    /// Prevents the overlay from being light dismissed by clicking on the layer or by pressing the Escape key.
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// Dims what the overlay covers with the theme's overlay background color.
    /// </summary>
    public bool? ModeFull { get; set; }

    /// <summary>
    /// Prevents the overlay from being dismissed by pressing the Escape key, while a click on the layer still dismisses it.
    /// </summary>
    public bool? NoDismissOnEscape { get; set; }

    /// <summary>
    /// Where the content is placed on the layer.
    /// </summary>
    public BitPosition? Position { get; set; }

    /// <summary>
    /// The CSS selector of the scroller element whose scrolling is taken away while the overlay is open.
    /// </summary>
    public string? ScrollerSelector { get; set; }

    /// <summary>
    /// The layer the overlay is stacked at, which takes over from the one the whole library shares.
    /// </summary>
    public int? ZIndex { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitOverlay"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitOverlay"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitOverlay"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitOverlay"/>.
    /// </remarks>
    /// <param name="bitOverlay">
    /// The <see cref="BitOverlay"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitOverlay bitOverlay)
    {
        if (bitOverlay is null) return;

        UpdateBaseParameters(bitOverlay);

        if (AbsolutePosition.HasValue && bitOverlay.HasNotBeenSet(nameof(AbsolutePosition)) && bitOverlay.AbsolutePosition != AbsolutePosition)
        {
            bitOverlay.AbsolutePosition = AbsolutePosition.Value;

            bitOverlay.ClassBuilder.Reset();
            bitOverlay.StyleBuilder.Reset();
        }

        if (AutoToggleScroll.HasValue && bitOverlay.HasNotBeenSet(nameof(AutoToggleScroll)))
        {
            bitOverlay.AutoToggleScroll = AutoToggleScroll.Value;
        }

        if (Blocking.HasValue && bitOverlay.HasNotBeenSet(nameof(Blocking)))
        {
            bitOverlay.Blocking = Blocking.Value;
        }

        if (ModeFull.HasValue && bitOverlay.HasNotBeenSet(nameof(ModeFull)) && bitOverlay.ModeFull != ModeFull)
        {
            bitOverlay.ModeFull = ModeFull.Value;

            bitOverlay.ClassBuilder.Reset();
        }

        if (NoDismissOnEscape.HasValue && bitOverlay.HasNotBeenSet(nameof(NoDismissOnEscape)))
        {
            bitOverlay.NoDismissOnEscape = NoDismissOnEscape.Value;
        }

        if (Position.HasValue && bitOverlay.HasNotBeenSet(nameof(Position)) && bitOverlay.Position != Position)
        {
            bitOverlay.Position = Position.Value;

            bitOverlay.ClassBuilder.Reset();
        }

        if (ScrollerSelector.HasValue() && bitOverlay.HasNotBeenSet(nameof(ScrollerSelector)))
        {
            bitOverlay.ScrollerSelector = ScrollerSelector;
        }

        if (ZIndex.HasValue && bitOverlay.HasNotBeenSet(nameof(ZIndex)) && bitOverlay.ZIndex != ZIndex)
        {
            bitOverlay.ZIndex = ZIndex.Value;

            bitOverlay.StyleBuilder.Reset();
        }
    }
}
