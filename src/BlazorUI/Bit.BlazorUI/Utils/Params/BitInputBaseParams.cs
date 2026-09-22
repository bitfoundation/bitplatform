namespace Bit.BlazorUI;

/// <summary>
/// The parameters every input component inherits from <see cref="BitInputBase{TValue}"/>, on top of the ones
/// <see cref="BitComponentBaseParams"/> already carries.
/// </summary>
/// <remarks>
/// Only what a <see cref="BitParams"/> cascade can reasonably say about a whole group of inputs is here:
/// a panel made read-only, a fieldset of required questions, a section taken out of validation, a set of
/// attributes put on every underlying input element. A value, a name, a display name or a change callback
/// belongs to one field rather than to the group around it, so none of those is cascadable.
/// </remarks>
public abstract class BitInputBaseParams : BitComponentBaseParams
{
    /// <summary>
    /// Additional HTML attributes to apply to the underlying input element of the component.
    /// <br />
    /// <see cref="BitInputBase{TValue}.InputHtmlAttributes"/>.
    /// </summary>
    public Dictionary<string, object>? InputHtmlAttributes { get; set; }

    /// <summary>
    /// Takes the component out of the validation of the EditForm it sits in.
    /// <br />
    /// <see cref="BitInputBase{TValue}.NoValidate"/>.
    /// </summary>
    public bool? NoValidate { get; set; }

    /// <summary>
    /// Makes the component read-only: still focusable and announced, but refusing every change.
    /// <br />
    /// <see cref="BitInputBase{TValue}.ReadOnly"/>.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Marks the component as required, which is announced through <c>aria-required</c>.
    /// <br />
    /// <see cref="BitInputBase{TValue}.Required"/>.
    /// </summary>
    public bool? Required { get; set; }



    /// <summary>
    /// Updates the inherited input parameters of the given component with any values set on this object,
    /// leaving alone every parameter the markup of that component has already written itself.
    /// </summary>
    /// <param name="input">The component whose parameters will be updated. Cannot be null.</param>
    protected void UpdateInputParameters<TValue>(BitInputBase<TValue> input)
    {
        if (input is null) return;

        UpdateBaseParameters(input);

        if (InputHtmlAttributes is not null && input.InputParameterHasNotBeenSet(nameof(InputHtmlAttributes)))
        {
            input.InputHtmlAttributes = InputHtmlAttributes;
        }

        if (NoValidate.HasValue && input.InputParameterHasNotBeenSet(nameof(NoValidate)))
        {
            input.NoValidate = NoValidate.Value;
        }

        if (ReadOnly.HasValue && input.InputParameterHasNotBeenSet(nameof(ReadOnly)))
        {
            input.ReadOnly = ReadOnly.Value;

            input.ClassBuilder.Reset();
        }

        if (Required.HasValue && input.InputParameterHasNotBeenSet(nameof(Required)))
        {
            input.Required = Required.Value;

            input.ClassBuilder.Reset();
        }
    }
}
