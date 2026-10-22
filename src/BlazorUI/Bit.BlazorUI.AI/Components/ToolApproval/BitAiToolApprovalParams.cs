namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiToolApproval"/> component.
/// </summary>
/// <remarks>
/// What belongs here is the look every approval prompt of a region shares. The call, the session and the decision
/// callback are left out on purpose: they are what makes one prompt the one it is.
/// </remarks>
public class BitAiToolApprovalParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiToolApproval"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiToolApproval value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiToolApproval)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the tool approval.
    /// </summary>
    public BitAiToolApprovalClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tool approval.
    /// </summary>
    public BitAiToolApprovalClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiToolApproval"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiToolApproval"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiToolApproval"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiToolApproval"/>.
    /// </remarks>
    /// <param name="bitAiToolApproval">
    /// The <see cref="BitAiToolApproval"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiToolApproval bitAiToolApproval)
    {
        if (bitAiToolApproval is null) return;

        UpdateBaseParameters(bitAiToolApproval);

        if (Classes is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(Classes), Classes, static a => a.Classes, static (a, v) => a.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(Styles), Styles, static a => a.Styles, static (a, v) => a.Styles = v);
        }
    }
}
