namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitElement"/> component.
/// </summary>
public class BitElementParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitElement"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitElement value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitElement)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets the custom html element used for the root node. The default is "div".
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only the content of the element is rendered, without the wrapping HTML tag.
    /// </summary>
    public bool? NoWrapper { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the default browser action of the click event of the element is prevented.
    /// </summary>
    public bool? PreventDefault { get; set; }

    /// <summary>
    /// Gets or sets the names of the events whose default browser action is prevented on the element.
    /// </summary>
    public IEnumerable<string>? PreventDefaultEvents { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the click event of the element is stopped from bubbling up to its ancestors.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Gets or sets the names of the events that are stopped from bubbling up from the element to its ancestors.
    /// </summary>
    public IEnumerable<string>? StopPropagationEvents { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitElement"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitElement"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitElement"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitElement"/>.
    /// </remarks>
    /// <param name="bitElement">
    /// The <see cref="BitElement"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitElement bitElement)
    {
        if (bitElement is null) return;

        UpdateBaseParameters(bitElement);

        if (Element.HasValue() && bitElement.HasNotBeenSet(nameof(Element)))
        {
            bitElement.Element = Element;
        }

        if (NoWrapper.HasValue && bitElement.HasNotBeenSet(nameof(NoWrapper)))
        {
            bitElement.NoWrapper = NoWrapper.Value;
        }

        if (PreventDefault.HasValue && bitElement.HasNotBeenSet(nameof(PreventDefault)))
        {
            bitElement.PreventDefault = PreventDefault.Value;
        }

        if (PreventDefaultEvents is not null && bitElement.HasNotBeenSet(nameof(PreventDefaultEvents)))
        {
            bitElement.PreventDefaultEvents = PreventDefaultEvents;
        }

        if (StopPropagation.HasValue && bitElement.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitElement.StopPropagation = StopPropagation.Value;
        }

        if (StopPropagationEvents is not null && bitElement.HasNotBeenSet(nameof(StopPropagationEvents)))
        {
            bitElement.StopPropagationEvents = StopPropagationEvents;
        }
    }
}
