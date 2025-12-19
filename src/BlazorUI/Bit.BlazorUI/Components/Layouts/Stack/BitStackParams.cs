using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitStack"/> component.
/// </summary>
public class BitStackParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitStack"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitStack value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitStack)}";



    public string Name => ParamName;



    /// <summary>
    /// Defines whether to render Stack children both horizontally and vertically.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Make the height of the stack auto.
    /// </summary>
    public bool? AutoHeight { get; set; }

    /// <summary>
    /// Make the width and height of the stack auto.
    /// </summary>
    public bool? AutoSize { get; set; }

    /// <summary>
    /// Make the width of the stack auto.
    /// </summary>
    public bool? AutoWidth { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Expand the direct children to occupy all of the root element's width and height.
    /// </summary>
    public bool? FillContent { get; set; }

    /// <summary>
    /// Sets the height of the stack to fit its content.
    /// </summary>
    public bool? FitHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the stack to fit its content.
    /// </summary>
    public bool? FitSize { get; set; }

    /// <summary>
    /// Sets the width of the stack to fit its content.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// Defines the spacing between Stack children.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Defines how much to grow the Stack in proportion to its siblings.
    /// </summary>
    public string? Grow { get; set; }

    /// <summary>
    /// Defines how much to grow the Stack in proportion to its siblings.
    /// </summary>
    public bool? Grows { get; set; }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    public BitAlignment? HorizontalAlign { get; set; }

    /// <summary>
    /// Defines whether to render Stack children in the opposite direction (bottom-to-top if it's a vertical Stack and right-to-left if it's a horizontal Stack).
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// Defines whether to render Stack children vertically.
    /// </summary>
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Defines whether Stack children should wrap onto multiple rows or columns when they are about to overflow the size of the Stack.
    /// </summary>
    public bool? Wrap { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitStack"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitStack"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitStack"/> will be updated. 
    /// This method does not overwrite existing values on <paramref name="bitStack"/>.
    /// </remarks>
    /// <param name="bitStack">
    /// The <see cref="BitStack"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitStack bitStack)
    {
        if (bitStack is null) return;

        UpdateBaseParameters(bitStack);

        if (Alignment.HasValue && bitStack.HasNotBeenSet(nameof(Alignment)))
        {
            bitStack.Alignment = Alignment.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (AutoHeight.HasValue && bitStack.HasNotBeenSet(nameof(AutoHeight)))
        {
            bitStack.AutoHeight = AutoHeight.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (AutoSize.HasValue && bitStack.HasNotBeenSet(nameof(AutoSize)))
        {
            bitStack.AutoSize = AutoSize.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (AutoWidth.HasValue && bitStack.HasNotBeenSet(nameof(AutoWidth)))
        {
            bitStack.AutoWidth = AutoWidth.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Element.HasValue() && bitStack.HasNotBeenSet(nameof(Element)))
        {
            bitStack.Element = Element;
        }

        if (FillContent.HasValue && bitStack.HasNotBeenSet(nameof(FillContent)))
        {
            bitStack.FillContent = FillContent.Value;

            bitStack.ClassBuilder.Reset();
        }

        if (FitHeight.HasValue && bitStack.HasNotBeenSet(nameof(FitHeight)))
        {
            bitStack.FitHeight = FitHeight.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (FitSize.HasValue && bitStack.HasNotBeenSet(nameof(FitSize)))
        {
            bitStack.FitSize = FitSize.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (FitWidth.HasValue && bitStack.HasNotBeenSet(nameof(FitWidth)))
        {
            bitStack.FitWidth = FitWidth.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Gap.HasValue() && bitStack.HasNotBeenSet(nameof(Gap)))
        {
            bitStack.Gap = Gap;

            bitStack.StyleBuilder.Reset();
        }

        if (Grow.HasValue() && bitStack.HasNotBeenSet(nameof(Grow)))
        {
            bitStack.Grow = Grow;

            bitStack.StyleBuilder.Reset();
        }

        if (Grows.HasValue && bitStack.HasNotBeenSet(nameof(Grows)))
        {
            bitStack.Grows = Grows.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Horizontal.HasValue && bitStack.HasNotBeenSet(nameof(Horizontal)))
        {
            bitStack.Horizontal = Horizontal.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalAlign.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalAlign)))
        {
            bitStack.HorizontalAlign = HorizontalAlign.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Reversed.HasValue && bitStack.HasNotBeenSet(nameof(Reversed)))
        {
            bitStack.Reversed = Reversed.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (VerticalAlign.HasValue && bitStack.HasNotBeenSet(nameof(VerticalAlign)))
        {
            bitStack.VerticalAlign = VerticalAlign.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Wrap.HasValue && bitStack.HasNotBeenSet(nameof(Wrap)))
        {
            bitStack.Wrap = Wrap.Value;

            bitStack.StyleBuilder.Reset();
        }
    }
}


