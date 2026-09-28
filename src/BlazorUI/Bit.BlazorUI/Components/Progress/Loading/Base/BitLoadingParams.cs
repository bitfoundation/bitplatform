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

        // Each value goes through BitLoadingBase.Cascade, which remembers what the loader held before the cascade
        // wrote it - so a value the cascade later drops is put back without a list of defaults kept here - and
        // only resets the class or style builder when the value really changes.

        if (AriaLive.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(AriaLive)))
        {
            bitLoading.Cascade(nameof(AriaLive), AriaLive, static l => l.AriaLive, static (l, v) => l.AriaLive = v);
        }

        if (Classes is not null && bitLoading.HasNotBeenSetOnLoading(nameof(Classes)))
        {
            bitLoading.Cascade(nameof(Classes), Classes, static l => l.Classes, static (l, v) => l.Classes = v, resetClass: true);
        }

        // A theme role outranks a custom color, so a cascaded one would silently override the CustomColor written on
        // the loader itself - which is the loader's own choice, and so the one that has to win.
        if (Color.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Color)) && bitLoading.HasNotBeenSetOnLoading(nameof(CustomColor)))
        {
            bitLoading.Cascade(nameof(Color), Color, static l => l.Color, static (l, v) => l.Color = v, resetStyle: true);
        }

        if (CustomColor.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(CustomColor)))
        {
            bitLoading.Cascade(nameof(CustomColor), CustomColor, static l => l.CustomColor, static (l, v) => l.CustomColor = v, resetStyle: true);
        }

        // Inline is a sizing choice as well - an unsized inline loader is drawn at the size of its text - so a loader
        // that asked for it itself keeps it against a cascaded size, the way its own CustomColor is kept above.
        var ownInline = bitLoading.Inline && bitLoading.HasNotBeenSetOnLoading(nameof(Inline)) is false;

        if (CustomSize.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(CustomSize)) && ownInline is false)
        {
            bitLoading.Cascade(nameof(CustomSize), CustomSize, static l => l.CustomSize, static (l, v) => l.CustomSize = v, resetClass: true, resetStyle: true);
        }

        if (Delay.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Delay)))
        {
            bitLoading.Cascade(nameof(Delay), Delay.Value, static l => l.Delay, static (l, v) => l.Delay = v);
        }

        if (Inline.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Inline)))
        {
            bitLoading.Cascade(nameof(Inline), Inline.Value, static l => l.Inline, static (l, v) => l.Inline = v, resetClass: true);
        }

        if (Label.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(Label)))
        {
            bitLoading.Cascade(nameof(Label), Label, static l => l.Label, static (l, v) => l.Label = v);
        }

        if (LabelPosition.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(LabelPosition)))
        {
            bitLoading.Cascade(nameof(LabelPosition), LabelPosition, static l => l.LabelPosition, static (l, v) => l.LabelPosition = v, resetClass: true);
        }

        if (Paused.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Paused)))
        {
            bitLoading.Cascade(nameof(Paused), Paused.Value, static l => l.Paused, static (l, v) => l.Paused = v, resetClass: true);
        }

        if (Role.HasValue() && bitLoading.HasNotBeenSetOnLoading(nameof(Role)))
        {
            bitLoading.Cascade(nameof(Role), Role, static l => l.Role, static (l, v) => l.Role = v);
        }

        // The same holds between a cascaded Size and the CustomSize or the Inline written on the loader itself.
        if (Size.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Size)) && bitLoading.HasNotBeenSetOnLoading(nameof(CustomSize)) && ownInline is false)
        {
            bitLoading.Cascade(nameof(Size), Size, static l => l.Size, static (l, v) => l.Size = v, resetClass: true, resetStyle: true);
        }

        if (Speed.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Speed)))
        {
            bitLoading.Cascade(nameof(Speed), Speed, static l => l.Speed, static (l, v) => l.Speed = v, resetStyle: true);
        }

        if (Styles is not null && bitLoading.HasNotBeenSetOnLoading(nameof(Styles)))
        {
            bitLoading.Cascade(nameof(Styles), Styles, static l => l.Styles, static (l, v) => l.Styles = v, resetStyle: true);
        }

        if (Thickness.HasValue && bitLoading.HasNotBeenSetOnLoading(nameof(Thickness)))
        {
            bitLoading.Cascade(nameof(Thickness), Thickness, static l => l.Thickness, static (l, v) => l.Thickness = v, resetStyle: true);
        }
    }
}
