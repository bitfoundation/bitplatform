namespace Bit.BlazorUI;

/// <summary>
/// The parameters that every bit BlazorUI input component inherits from <see cref="BitInputBase{TValue}"/>,
/// which the parameters class of an input derives from so that a <see cref="BitParams"/> cascade carries them
/// along with the ones the component declares itself.
/// </summary>
/// <remarks>
/// Only the parameters that describe how a whole area of a form behaves are carried here. What identifies a
/// single field - its <see cref="BitInputBase{TValue}.Value"/>, its
/// <see cref="BitInputBase{TValue}.DefaultValue"/>, its <see cref="BitInputBase{TValue}.Name"/> and its
/// <see cref="BitInputBase{TValue}.DisplayName"/> - is deliberately left out, since a value shared by every
/// input under the cascade is never what a consumer means. So is
/// <see cref="BitInputBase{TValue}.InputHtmlAttributes"/>, a dictionary the components write into, which they
/// would end up sharing a single instance of, and <see cref="BitInputBase{TValue}.NoValidate"/>, which is read
/// while the parameters are still being set and so before a cascade has been applied.
/// </remarks>
public abstract class BitInputBaseParams<TValue> : BitComponentBaseParams
{
    /// <summary>
    /// Makes the input read-only.
    /// <br />
    /// <see cref="BitInputBase{TValue}.ReadOnly"/>.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Makes the input required.
    /// <br />
    /// <see cref="BitInputBase{TValue}.Required"/>.
    /// </summary>
    public bool? Required { get; set; }



    /// <summary>
    /// Updates the inherited input properties of the specified <see cref="BitInputBase{TValue}"/> instance with
    /// any values that have been set on this object, if those properties have not already been set on the
    /// component itself.
    /// </summary>
    /// <param name="bitInputBase">
    /// The <see cref="BitInputBase{TValue}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateInputBaseParameters(BitInputBase<TValue> bitInputBase)
    {
        if (bitInputBase is null) return;

        UpdateBaseParameters(bitInputBase);

        if (ReadOnly.HasValue && bitInputBase.HasNotBeenSetOnInput(nameof(ReadOnly)))
        {
            bitInputBase.ReadOnly = ReadOnly.Value;

            bitInputBase.ClassBuilder.Reset();
        }

        if (Required.HasValue && bitInputBase.HasNotBeenSetOnInput(nameof(Required)))
        {
            bitInputBase.Required = Required.Value;

            bitInputBase.ClassBuilder.Reset();
        }
    }
}
