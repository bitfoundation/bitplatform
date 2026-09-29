namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSplitter"/> component.
/// </summary>
/// <remarks>
/// What each splitter holds for itself - the content of its panels, where its gutter stands (<see cref="BitSplitter.Percent"/>),
/// whether its panel is folded away (<see cref="BitSplitter.Collapsed"/>), the key it remembers that under
/// (<see cref="BitSplitter.PersistKey"/>) and its callbacks - is not a group default, so it is not carried here.
/// </remarks>
public class BitSplitterParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSplitter"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSplitter value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSplitter)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the splitter.
    /// </summary>
    public BitSplitterClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon of the collapse button while the panel that folds is open, using <see cref="BitIconInfo"/> for external
    /// icon library support. Takes precedence over <see cref="CollapseIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CollapseIcon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon shown on the collapse button while the panel that folds is open.
    /// </summary>
    public string? CollapseIconName { get; set; }

    /// <summary>
    /// The size, in pixels, the folded panel is held at while it is collapsed.
    /// </summary>
    public int? CollapsedSize { get; set; }

    /// <summary>
    /// Lets the panel that folds be collapsed from the gutter: by Enter, by Ctrl with an arrow key, by the collapse button
    /// and by a drag that snaps it shut.
    /// </summary>
    public bool? Collapsible { get; set; }

    /// <summary>
    /// Folds the second panel away rather than the first one.
    /// </summary>
    public bool? CollapseSecondPanel { get; set; }

    /// <summary>
    /// The share of the splitter, as a percentage, the first panel starts at and is reset to.
    /// </summary>
    public double? DefaultPercent { get; set; }

    /// <summary>
    /// The grid, in pixels, a drag of the gutter moves the split along. 0 is no grid at all.
    /// </summary>
    public int? DragStep { get; set; }

    /// <summary>
    /// The icon of the collapse button while the panel that folds is away, using <see cref="BitIconInfo"/> for external
    /// icon library support. Takes precedence over <see cref="ExpandIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ExpandIcon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon shown on the collapse button while the panel that folds is away.
    /// </summary>
    public string? ExpandIconName { get; set; }

    /// <summary>
    /// The max size of the first panel in pixels.
    /// </summary>
    public int? FirstPanelMaxSize { get; set; }

    /// <summary>
    /// The min size of the first panel in pixels.
    /// </summary>
    public int? FirstPanelMinSize { get; set; }

    /// <summary>
    /// The initial size of the first panel in pixels.
    /// </summary>
    public int? FirstPanelSize { get; set; }

    /// <summary>
    /// The smallest strip, in pixels, a pointer has to land in to take hold of the gutter.
    /// </summary>
    public int? GutterHitSize { get; set; }

    /// <summary>
    /// The icon of the gutter using <see cref="BitIconInfo"/> for external icon library support.
    /// Takes precedence over <see cref="GutterIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? GutterIcon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon to render in the gutter.
    /// </summary>
    public string? GutterIconName { get; set; }

    /// <summary>
    /// The size of the gutter in pixels.
    /// </summary>
    public int? GutterSize { get; set; }

    /// <summary>
    /// The custom content of the gutter, in place of the icon or of the default grip indicator.
    /// </summary>
    public RenderFragment? GutterTemplate { get; set; }

    /// <summary>
    /// How far, in pixels, one press of an arrow key on the gutter moves the split.
    /// </summary>
    public int? KeyboardStep { get; set; }

    /// <summary>
    /// Moves a line rather than the panels while the gutter is being dragged, and lays the panels out once the drag is over.
    /// </summary>
    public bool? LazyResize { get; set; }

    /// <summary>
    /// Keeps a double-click on the gutter from resetting the splitter to the sizes its parameters declare.
    /// </summary>
    public bool? NoResetOnDoubleClick { get; set; }

    /// <summary>
    /// Keeps what PersistKey remembers in the session storage rather than the local storage.
    /// </summary>
    public bool? PersistInSessionStorage { get; set; }

    /// <summary>
    /// Keeps the splitter as it is: the gutter is still shown but cannot be dragged or moved from the keyboard.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// The max size of the second panel in pixels.
    /// </summary>
    public int? SecondPanelMaxSize { get; set; }

    /// <summary>
    /// The min size of the second panel in pixels.
    /// </summary>
    public int? SecondPanelMinSize { get; set; }

    /// <summary>
    /// The initial size of the second panel in pixels.
    /// </summary>
    public int? SecondPanelSize { get; set; }

    /// <summary>
    /// Draws a control on the gutter of a collapsible splitter that folds the panel away and brings it back.
    /// </summary>
    public bool? ShowCollapseButton { get; set; }

    /// <summary>
    /// How small, in pixels, a drag has to leave the panel that folds for it to snap shut. 0 works it out from the
    /// minimum size of the panel.
    /// </summary>
    public int? SnapSize { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the splitter.
    /// </summary>
    public BitSplitterClassStyles? Styles { get; set; }

    /// <summary>
    /// Stacks the two panels instead of placing them side by side.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSplitter"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSplitter"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSplitter"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitSplitter"/>.
    /// </remarks>
    /// <param name="bitSplitter">
    /// The <see cref="BitSplitter"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSplitter bitSplitter)
    {
        if (bitSplitter is null) return;

        UpdateBaseParameters(bitSplitter);

        if (Classes is not null && bitSplitter.HasNotBeenSet(nameof(Classes)))
        {
            bitSplitter.Classes = Classes;

            bitSplitter.ClassBuilder.Reset();
        }

        if (CollapseIcon is not null && bitSplitter.HasNotBeenSet(nameof(CollapseIcon)))
        {
            bitSplitter.CollapseIcon = CollapseIcon;
        }

        if (CollapseIconName.HasValue() && bitSplitter.HasNotBeenSet(nameof(CollapseIconName)))
        {
            bitSplitter.CollapseIconName = CollapseIconName;
        }

        if (CollapsedSize.HasValue && bitSplitter.HasNotBeenSet(nameof(CollapsedSize)))
        {
            bitSplitter.CollapsedSize = CollapsedSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (Collapsible.HasValue && bitSplitter.HasNotBeenSet(nameof(Collapsible)))
        {
            bitSplitter.Collapsible = Collapsible.Value;

            bitSplitter.ClassBuilder.Reset();
        }

        if (CollapseSecondPanel.HasValue && bitSplitter.HasNotBeenSet(nameof(CollapseSecondPanel)))
        {
            bitSplitter.CollapseSecondPanel = CollapseSecondPanel.Value;

            bitSplitter.ClassBuilder.Reset();
        }

        if (DefaultPercent.HasValue && bitSplitter.HasNotBeenSet(nameof(DefaultPercent)))
        {
            bitSplitter.DefaultPercent = DefaultPercent.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (DragStep.HasValue && bitSplitter.HasNotBeenSet(nameof(DragStep)))
        {
            bitSplitter.DragStep = DragStep.Value;
        }

        if (ExpandIcon is not null && bitSplitter.HasNotBeenSet(nameof(ExpandIcon)))
        {
            bitSplitter.ExpandIcon = ExpandIcon;
        }

        if (ExpandIconName.HasValue() && bitSplitter.HasNotBeenSet(nameof(ExpandIconName)))
        {
            bitSplitter.ExpandIconName = ExpandIconName;
        }

        if (FirstPanelMaxSize.HasValue && bitSplitter.HasNotBeenSet(nameof(FirstPanelMaxSize)))
        {
            bitSplitter.FirstPanelMaxSize = FirstPanelMaxSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (FirstPanelMinSize.HasValue && bitSplitter.HasNotBeenSet(nameof(FirstPanelMinSize)))
        {
            bitSplitter.FirstPanelMinSize = FirstPanelMinSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (FirstPanelSize.HasValue && bitSplitter.HasNotBeenSet(nameof(FirstPanelSize)))
        {
            bitSplitter.FirstPanelSize = FirstPanelSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (GutterHitSize.HasValue && bitSplitter.HasNotBeenSet(nameof(GutterHitSize)))
        {
            bitSplitter.GutterHitSize = GutterHitSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (GutterIcon is not null && bitSplitter.HasNotBeenSet(nameof(GutterIcon)))
        {
            bitSplitter.GutterIcon = GutterIcon;
        }

        if (GutterIconName.HasValue() && bitSplitter.HasNotBeenSet(nameof(GutterIconName)))
        {
            bitSplitter.GutterIconName = GutterIconName;
        }

        if (GutterSize.HasValue && bitSplitter.HasNotBeenSet(nameof(GutterSize)))
        {
            bitSplitter.GutterSize = GutterSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (GutterTemplate is not null && bitSplitter.HasNotBeenSet(nameof(GutterTemplate)))
        {
            bitSplitter.GutterTemplate = GutterTemplate;
        }

        if (KeyboardStep.HasValue && bitSplitter.HasNotBeenSet(nameof(KeyboardStep)))
        {
            bitSplitter.KeyboardStep = KeyboardStep.Value;
        }

        if (LazyResize.HasValue && bitSplitter.HasNotBeenSet(nameof(LazyResize)))
        {
            bitSplitter.LazyResize = LazyResize.Value;
        }

        if (NoResetOnDoubleClick.HasValue && bitSplitter.HasNotBeenSet(nameof(NoResetOnDoubleClick)))
        {
            bitSplitter.NoResetOnDoubleClick = NoResetOnDoubleClick.Value;
        }

        if (PersistInSessionStorage.HasValue && bitSplitter.HasNotBeenSet(nameof(PersistInSessionStorage)))
        {
            bitSplitter.PersistInSessionStorage = PersistInSessionStorage.Value;
        }

        if (ReadOnly.HasValue && bitSplitter.HasNotBeenSet(nameof(ReadOnly)))
        {
            bitSplitter.ReadOnly = ReadOnly.Value;

            bitSplitter.ClassBuilder.Reset();
        }

        if (SecondPanelMaxSize.HasValue && bitSplitter.HasNotBeenSet(nameof(SecondPanelMaxSize)))
        {
            bitSplitter.SecondPanelMaxSize = SecondPanelMaxSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (SecondPanelMinSize.HasValue && bitSplitter.HasNotBeenSet(nameof(SecondPanelMinSize)))
        {
            bitSplitter.SecondPanelMinSize = SecondPanelMinSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (SecondPanelSize.HasValue && bitSplitter.HasNotBeenSet(nameof(SecondPanelSize)))
        {
            bitSplitter.SecondPanelSize = SecondPanelSize.Value;

            bitSplitter.StyleBuilder.Reset();
        }

        if (ShowCollapseButton.HasValue && bitSplitter.HasNotBeenSet(nameof(ShowCollapseButton)))
        {
            bitSplitter.ShowCollapseButton = ShowCollapseButton.Value;

            bitSplitter.ClassBuilder.Reset();
        }

        if (SnapSize.HasValue && bitSplitter.HasNotBeenSet(nameof(SnapSize)))
        {
            bitSplitter.SnapSize = SnapSize.Value;
        }

        if (Styles is not null && bitSplitter.HasNotBeenSet(nameof(Styles)))
        {
            bitSplitter.Styles = Styles;

            bitSplitter.StyleBuilder.Reset();
        }

        if (Vertical.HasValue && bitSplitter.HasNotBeenSet(nameof(Vertical)))
        {
            bitSplitter.Vertical = Vertical.Value;

            bitSplitter.ClassBuilder.Reset();
        }
    }
}
