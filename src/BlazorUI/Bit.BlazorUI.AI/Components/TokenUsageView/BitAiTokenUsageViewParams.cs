namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiTokenUsageView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the readouts of an application, or of one part of it, agree on: which figures they show
/// and their look. The usage and the metadata are left out on purpose: they are what makes one readout the one it is.
/// </remarks>
public class BitAiTokenUsageViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiTokenUsageView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiTokenUsageView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiTokenUsageView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the token usage view.
    /// </summary>
    public BitAiTokenUsageViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Shows the time to the first token, when the metadata carries it.
    /// </summary>
    public bool? ShowLatency { get; set; }

    /// <summary>
    /// Shows the throughput in tokens per second, when the metadata carries it.
    /// </summary>
    public bool? ShowSpeed { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the token usage view.
    /// </summary>
    public BitAiTokenUsageViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiTokenUsageView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiTokenUsageView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiTokenUsageView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiTokenUsageView"/>.
    /// </remarks>
    /// <param name="bitAiTokenUsageView">
    /// The <see cref="BitAiTokenUsageView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiTokenUsageView bitAiTokenUsageView)
    {
        if (bitAiTokenUsageView is null) return;

        UpdateBaseParameters(bitAiTokenUsageView);

        if (Classes is not null)
        {
            bitAiTokenUsageView.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (ShowLatency.HasValue)
        {
            bitAiTokenUsageView.TakeFromCascade(nameof(ShowLatency), ShowLatency.Value, static t => t.ShowLatency, static (t, v) => t.ShowLatency = v);
        }

        if (ShowSpeed.HasValue)
        {
            bitAiTokenUsageView.TakeFromCascade(nameof(ShowSpeed), ShowSpeed.Value, static t => t.ShowSpeed, static (t, v) => t.ShowSpeed = v);
        }

        if (Styles is not null)
        {
            bitAiTokenUsageView.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }
    }
}
