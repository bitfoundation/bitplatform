namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the loading components: every one of <see cref="BitBarsLoading"/>,
/// <see cref="BitRingLoading"/>, <see cref="BitSpinnerLoading"/> and the rest of the family that derives from
/// <see cref="BitLoadingBase"/>.
/// </summary>
/// <remarks>
/// The loaders share one API, so they share one cascade: a <see cref="BitParams"/> holding these reaches every
/// loader under it, whichever animation it draws. <see cref="BitLoadingBase.LabelTemplate"/> is left out, since
/// a render fragment belongs to the markup it is written in.
/// </remarks>
public class BitLoadingParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the loading components' cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitLoading value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitLoading";



    public string Name => ParamName;



    /// <summary>
    /// How insistently the live region of the loading components announces itself.
    /// <br />
    /// <see cref="BitLoadingBase.AriaLive"/>.
    /// </summary>
    public string? AriaLive { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the loading components.
    /// <br />
    /// <see cref="BitLoadingBase.Classes"/>.
    /// </summary>
    public BitLoadingClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the loading components.
    /// <br />
    /// <see cref="BitLoadingBase.Color"/>.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom css color of the loading components, which only applies while Color is left unset.
    /// <br />
    /// <see cref="BitLoadingBase.CustomColor"/>.
    /// </summary>
    public string? CustomColor { get; set; }

    /// <summary>
    /// The custom size of the loading components in px, which only applies while Size is left unset.
    /// <br />
    /// <see cref="BitLoadingBase.CustomSize"/>.
    /// </summary>
    public int? CustomSize { get; set; }

    /// <summary>
    /// How long, in milliseconds, the loading components wait before they show anything.
    /// <br />
    /// <see cref="BitLoadingBase.Delay"/>.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// Lays the loading components out inline, aligned to the middle of the current line.
    /// <br />
    /// <see cref="BitLoadingBase.Inline"/>.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// The text of the label of the loading components, which is also what a screen reader announces.
    /// <br />
    /// <see cref="BitLoadingBase.Label"/>.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The position of the label of the loading components.
    /// <br />
    /// <see cref="BitLoadingBase.LabelPosition"/>.
    /// </summary>
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Holds the animation of the loading components at the frame it had reached.
    /// <br />
    /// <see cref="BitLoadingBase.Paused"/>.
    /// </summary>
    public bool? Paused { get; set; }

    /// <summary>
    /// The ARIA role of the root element of the loading components.
    /// <br />
    /// <see cref="BitLoadingBase.Role"/>.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The size of the loading components.
    /// <br />
    /// <see cref="BitLoadingBase.Size"/>.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// How fast the animation of the loading components runs, as a multiplier of its normal speed.
    /// <br />
    /// <see cref="BitLoadingBase.Speed"/>.
    /// </summary>
    public double? Speed { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the loading components.
    /// <br />
    /// <see cref="BitLoadingBase.Styles"/>.
    /// </summary>
    public BitLoadingClassStyles? Styles { get; set; }

    /// <summary>
    /// The thickness, in px, of the stroke of the loading components drawn with one.
    /// <br />
    /// <see cref="BitLoadingBase.Thickness"/>.
    /// </summary>
    public int? Thickness { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitLoadingBase"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the loading component.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitLoading"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitLoading"/>.
    /// </remarks>
    /// <param name="bitLoading">
    /// The <see cref="BitLoadingBase"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitLoadingBase bitLoading)
    {
        if (bitLoading is null) return;

        UpdateBaseParameters(bitLoading);

        if (AriaLive.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(AriaLive)))
        {
            bitLoading.AriaLive = AriaLive;
        }

        if (Classes is not null && bitLoading.HasNotBeenSetOnLoading(nameof(Classes)))
        {
            bitLoading.Classes = Classes;

            bitLoading.ClassBuilder.Reset();
        }

        // A theme role outranks a custom color, so a cascaded one would silently override the CustomColor written on
        // the loader itself - which is the loader's own choice, and so the one that has to win.
        if (Color.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Color)) && bitLoading.HasNotBeenSetOnLoading(nameof(CustomColor)))
        {
            bitLoading.Color = Color.Value;

            bitLoading.StyleBuilder.Reset();
        }

        if (CustomColor.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(CustomColor)))
        {
            bitLoading.CustomColor = CustomColor;

            bitLoading.StyleBuilder.Reset();
        }

        if (CustomSize.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(CustomSize)))
        {
            bitLoading.CustomSize = CustomSize.Value;

            bitLoading.ClassBuilder.Reset();
            bitLoading.StyleBuilder.Reset();
        }

        if (Delay.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Delay)))
        {
            bitLoading.Delay = Delay.Value;
        }

        if (Inline.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Inline)))
        {
            bitLoading.Inline = Inline.Value;

            bitLoading.ClassBuilder.Reset();
        }

        if (Label.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(Label)))
        {
            bitLoading.Label = Label;
        }

        if (LabelPosition.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(LabelPosition)))
        {
            bitLoading.LabelPosition = LabelPosition.Value;

            bitLoading.ClassBuilder.Reset();
        }

        if (Paused.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Paused)))
        {
            bitLoading.Paused = Paused.Value;

            bitLoading.ClassBuilder.Reset();
        }

        if (Role.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(Role)))
        {
            bitLoading.Role = Role;
        }

        // The same holds between a cascaded Size and the CustomSize written on the loader itself.
        if (Size.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Size)) && bitLoading.HasNotBeenSetOnLoading(nameof(CustomSize)))
        {
            bitLoading.Size = Size.Value;

            bitLoading.ClassBuilder.Reset();
            bitLoading.StyleBuilder.Reset();
        }

        if (Speed.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Speed)))
        {
            bitLoading.Speed = Speed.Value;

            bitLoading.StyleBuilder.Reset();
        }

        if (Styles is not null && bitLoading.HasNotBeenSetOnLoading(nameof(Styles)))
        {
            bitLoading.Styles = Styles;

            bitLoading.StyleBuilder.Reset();
        }

        if (Thickness.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Thickness)))
        {
            bitLoading.Thickness = Thickness.Value;

            bitLoading.StyleBuilder.Reset();
        }
    }
}
