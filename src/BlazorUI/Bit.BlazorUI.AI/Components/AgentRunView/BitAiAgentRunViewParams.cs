using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiAgentRunView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the run views of an application, or of one part of it, agree on: the pace of the clock and
/// the look. The run, the session and the callback are left out on purpose: they are what makes one view the one it is.
/// </remarks>
public class BitAiAgentRunViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiAgentRunView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiAgentRunView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiAgentRunView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiAgentRunViewParams))]
    public BitAiAgentRunViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the agent run view.
    /// </summary>
    public BitAiAgentRunViewClassStyles? Classes { get; set; }

    /// <summary>
    /// How often the elapsed time is refreshed while the run is active.
    /// </summary>
    public TimeSpan? ClockInterval { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the agent run view.
    /// </summary>
    public BitAiAgentRunViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiAgentRunView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiAgentRunView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiAgentRunView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiAgentRunView"/>.
    /// </remarks>
    /// <param name="bitAiAgentRunView">
    /// The <see cref="BitAiAgentRunView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiAgentRunView bitAiAgentRunView)
    {
        if (bitAiAgentRunView is null) return;

        UpdateBaseParameters(bitAiAgentRunView);

        if (Classes is not null)
        {
            bitAiAgentRunView.TakeFromCascade(nameof(Classes), Classes, static r => r.Classes, static (r, v) => r.Classes = v);
        }

        if (ClockInterval.HasValue)
        {
            bitAiAgentRunView.TakeFromCascade(nameof(ClockInterval), ClockInterval.Value, static r => r.ClockInterval, static (r, v) => r.ClockInterval = v);
        }

        if (Styles is not null)
        {
            bitAiAgentRunView.TakeFromCascade(nameof(Styles), Styles, static r => r.Styles, static (r, v) => r.Styles = v);
        }
    }
}
