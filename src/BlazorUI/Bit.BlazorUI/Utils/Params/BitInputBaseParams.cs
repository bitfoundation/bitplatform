namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitInputBase{TValue}"/> that a whole subtree of inputs can share.
/// </summary>
/// <remarks>
/// Only the parameters that describe how a group of inputs behaves are here. The ones that identify a single
/// field - <c>Name</c>, <c>DisplayName</c>, <c>Value</c> and the rest of the binding - belong to that one field
/// and would be wrong to share, and <c>NoValidate</c> is read before this object is consulted, since the input
/// wires itself to the <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/> as its parameters are set.
/// </remarks>
public abstract class BitInputBaseParams : BitComponentBaseParams
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
    /// Updates the input base properties of the specified <see cref="BitInputBase{TValue}"/> instance with any values
    /// that have been set on this object, if those properties have not already been set on the input itself.
    /// </summary>
    /// <param name="bitInputBase">
    /// The <see cref="BitInputBase{TValue}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateInputParameters<TValue>(BitInputBase<TValue> bitInputBase)
    {
        if (bitInputBase is null) return;

        UpdateBaseParameters(bitInputBase);

        if (ReadOnly.HasValue && bitInputBase.InputParameterHasNotBeenSet(nameof(ReadOnly)))
        {
            bitInputBase.ReadOnly = ReadOnly.Value;

            bitInputBase.ClassBuilder.Reset();
        }

        if (Required.HasValue && bitInputBase.InputParameterHasNotBeenSet(nameof(Required)))
        {
            bitInputBase.Required = Required.Value;

            bitInputBase.ClassBuilder.Reset();
        }
    }
}
