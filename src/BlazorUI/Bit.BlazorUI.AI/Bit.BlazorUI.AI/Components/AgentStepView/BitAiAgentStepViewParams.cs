using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiAgentStepView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the steps of an application, or of one part of it, agree on: their look. The step and the
/// conversation are left out on purpose: they are what makes one step the one it is.
/// </remarks>
public class BitAiAgentStepViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiAgentStepView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiAgentStepView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiAgentStepView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiAgentStepViewParams))]
    public BitAiAgentStepViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the agent step view.
    /// </summary>
    public BitAiAgentStepViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the agent step view.
    /// </summary>
    public BitAiAgentStepViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiAgentStepView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiAgentStepView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiAgentStepView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiAgentStepView"/>.
    /// </remarks>
    /// <param name="bitAiAgentStepView">
    /// The <see cref="BitAiAgentStepView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiAgentStepView bitAiAgentStepView)
    {
        if (bitAiAgentStepView is null) return;

        UpdateBaseParameters(bitAiAgentStepView);

        if (Classes is not null)
        {
            bitAiAgentStepView.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiAgentStepView.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }
    }
}
