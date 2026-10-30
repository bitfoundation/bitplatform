using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiAgentStatusBadge"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the badges of an application, or of one part of it, agree on: their look. The status is
/// left out on purpose: it is what makes one badge the one it is.
/// </remarks>
public class BitAiAgentStatusBadgeParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiAgentStatusBadge"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiAgentStatusBadge value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiAgentStatusBadge)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiAgentStatusBadgeParams))]
    public BitAiAgentStatusBadgeParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the agent status badge.
    /// </summary>
    public BitAiAgentStatusBadgeClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the agent status badge.
    /// </summary>
    public BitAiAgentStatusBadgeClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiAgentStatusBadge"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiAgentStatusBadge"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiAgentStatusBadge"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiAgentStatusBadge"/>.
    /// </remarks>
    /// <param name="bitAiAgentStatusBadge">
    /// The <see cref="BitAiAgentStatusBadge"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiAgentStatusBadge bitAiAgentStatusBadge)
    {
        if (bitAiAgentStatusBadge is null) return;

        UpdateBaseParameters(bitAiAgentStatusBadge);

        if (Classes is not null)
        {
            bitAiAgentStatusBadge.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiAgentStatusBadge.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }
    }
}
