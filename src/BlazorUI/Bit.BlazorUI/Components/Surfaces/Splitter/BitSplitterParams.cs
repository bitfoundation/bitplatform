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
    /// This method does not overwrite existing values on <paramref name="bitSplitter"/>. What it supplies is recorded on
    /// the component, which puts back the value it replaced once this object stops supplying one.
    /// <br />
    /// <see cref="DefaultPercent"/>, <see cref="FirstPanelSize"/> and <see cref="SecondPanelSize"/> are supplied together or
    /// not at all: a splitter that sets any of the three for itself has decided where its split starts, and a cascaded
    /// one taken beside it would outrank it rather than fill a gap.
    /// </remarks>
    /// <param name="bitSplitter">
    /// The <see cref="BitSplitter"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSplitter bitSplitter)
    {
        if (bitSplitter is null) return;

        UpdateBaseParameters(bitSplitter);

        if (Classes is not null)
        {
            bitSplitter.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (CollapseIcon is not null)
        {
            bitSplitter.TakeFromCascade(nameof(CollapseIcon), CollapseIcon, static s => s.CollapseIcon, static (s, v) => s.CollapseIcon = v);
        }

        if (CollapseIconName.HasValue())
        {
            bitSplitter.TakeFromCascade(nameof(CollapseIconName), CollapseIconName, static s => s.CollapseIconName, static (s, v) => s.CollapseIconName = v);
        }

        if (CollapsedSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(CollapsedSize), CollapsedSize.Value, static s => s.CollapsedSize, static (s, v) => s.CollapsedSize = v);
        }

        if (Collapsible.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(Collapsible), Collapsible.Value, static s => s.Collapsible, static (s, v) => s.Collapsible = v);
        }

        if (CollapseSecondPanel.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(CollapseSecondPanel), CollapseSecondPanel.Value, static s => s.CollapseSecondPanel, static (s, v) => s.CollapseSecondPanel = v);
        }

        // Where the split starts is one decision across the three sizes, and DefaultPercent and FirstPanelSize outrank
        // SecondPanelSize: a cascaded one taken beside one the splitter set itself would override it rather than fill a
        // gap, so a splitter that sizes its panels at all is left to do it alone.
        if (bitSplitter.SizesItsOwnPanels is false)
        {
            if (DefaultPercent.HasValue)
            {
                bitSplitter.TakeFromCascade(nameof(DefaultPercent), DefaultPercent, static s => s.DefaultPercent, static (s, v) => s.DefaultPercent = v);
            }

            if (FirstPanelSize.HasValue)
            {
                bitSplitter.TakeFromCascade(nameof(FirstPanelSize), FirstPanelSize, static s => s.FirstPanelSize, static (s, v) => s.FirstPanelSize = v);
            }

            if (SecondPanelSize.HasValue)
            {
                bitSplitter.TakeFromCascade(nameof(SecondPanelSize), SecondPanelSize, static s => s.SecondPanelSize, static (s, v) => s.SecondPanelSize = v);
            }
        }

        if (DragStep.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(DragStep), DragStep.Value, static s => s.DragStep, static (s, v) => s.DragStep = v);
        }

        if (ExpandIcon is not null)
        {
            bitSplitter.TakeFromCascade(nameof(ExpandIcon), ExpandIcon, static s => s.ExpandIcon, static (s, v) => s.ExpandIcon = v);
        }

        if (ExpandIconName.HasValue())
        {
            bitSplitter.TakeFromCascade(nameof(ExpandIconName), ExpandIconName, static s => s.ExpandIconName, static (s, v) => s.ExpandIconName = v);
        }

        if (FirstPanelMaxSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(FirstPanelMaxSize), FirstPanelMaxSize, static s => s.FirstPanelMaxSize, static (s, v) => s.FirstPanelMaxSize = v);
        }

        if (FirstPanelMinSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(FirstPanelMinSize), FirstPanelMinSize, static s => s.FirstPanelMinSize, static (s, v) => s.FirstPanelMinSize = v);
        }

        if (GutterHitSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(GutterHitSize), GutterHitSize, static s => s.GutterHitSize, static (s, v) => s.GutterHitSize = v);
        }

        if (GutterIcon is not null)
        {
            bitSplitter.TakeFromCascade(nameof(GutterIcon), GutterIcon, static s => s.GutterIcon, static (s, v) => s.GutterIcon = v);
        }

        if (GutterIconName.HasValue())
        {
            bitSplitter.TakeFromCascade(nameof(GutterIconName), GutterIconName, static s => s.GutterIconName, static (s, v) => s.GutterIconName = v);
        }

        if (GutterSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(GutterSize), GutterSize, static s => s.GutterSize, static (s, v) => s.GutterSize = v);
        }

        if (GutterTemplate is not null)
        {
            bitSplitter.TakeFromCascade(nameof(GutterTemplate), GutterTemplate, static s => s.GutterTemplate, static (s, v) => s.GutterTemplate = v);
        }

        if (KeyboardStep.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(KeyboardStep), KeyboardStep.Value, static s => s.KeyboardStep, static (s, v) => s.KeyboardStep = v);
        }

        if (LazyResize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(LazyResize), LazyResize.Value, static s => s.LazyResize, static (s, v) => s.LazyResize = v);
        }

        if (NoResetOnDoubleClick.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(NoResetOnDoubleClick), NoResetOnDoubleClick.Value, static s => s.NoResetOnDoubleClick, static (s, v) => s.NoResetOnDoubleClick = v);
        }

        if (PersistInSessionStorage.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(PersistInSessionStorage), PersistInSessionStorage.Value, static s => s.PersistInSessionStorage, static (s, v) => s.PersistInSessionStorage = v);
        }

        if (ReadOnly.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(ReadOnly), ReadOnly.Value, static s => s.ReadOnly, static (s, v) => s.ReadOnly = v);
        }

        if (SecondPanelMaxSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(SecondPanelMaxSize), SecondPanelMaxSize, static s => s.SecondPanelMaxSize, static (s, v) => s.SecondPanelMaxSize = v);
        }

        if (SecondPanelMinSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(SecondPanelMinSize), SecondPanelMinSize, static s => s.SecondPanelMinSize, static (s, v) => s.SecondPanelMinSize = v);
        }

        if (ShowCollapseButton.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(ShowCollapseButton), ShowCollapseButton.Value, static s => s.ShowCollapseButton, static (s, v) => s.ShowCollapseButton = v);
        }

        if (SnapSize.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(SnapSize), SnapSize.Value, static s => s.SnapSize, static (s, v) => s.SnapSize = v);
        }

        if (Styles is not null)
        {
            bitSplitter.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (Vertical.HasValue)
        {
            bitSplitter.TakeFromCascade(nameof(Vertical), Vertical.Value, static s => s.Vertical, static (s, v) => s.Vertical = v);
        }
    }
}
