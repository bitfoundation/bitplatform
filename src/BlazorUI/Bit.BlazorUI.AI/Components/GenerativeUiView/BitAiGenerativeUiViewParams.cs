namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiGenerativeUiView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every generative UI view of an app or a section of it agrees on - whether the binding
/// diagnostics are shown, and the classes and styles of its parts - rather than the part a single view renders.
/// </remarks>
public class BitAiGenerativeUiViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiGenerativeUiView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiGenerativeUiView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiGenerativeUiView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the generative UI view.
    /// </summary>
    public BitAiGenerativeUiViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Lists the properties that failed to bind and the ones ignored because the component does not declare them.
    /// </summary>
    public bool? ShowBindingErrors { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the generative UI view.
    /// </summary>
    public BitAiGenerativeUiViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiGenerativeUiView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiGenerativeUiView"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiGenerativeUiView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiGenerativeUiView"/>.
    /// </remarks>
    /// <param name="bitAiGenerativeUiView">
    /// The <see cref="BitAiGenerativeUiView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiGenerativeUiView bitAiGenerativeUiView)
    {
        if (bitAiGenerativeUiView is null) return;

        UpdateBaseParameters(bitAiGenerativeUiView);

        if (Classes is not null)
        {
            bitAiGenerativeUiView.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (ShowBindingErrors.HasValue)
        {
            bitAiGenerativeUiView.TakeFromCascade(nameof(ShowBindingErrors), ShowBindingErrors.Value, static c => c.ShowBindingErrors, static (c, v) => c.ShowBindingErrors = v);
        }

        if (Styles is not null)
        {
            bitAiGenerativeUiView.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
