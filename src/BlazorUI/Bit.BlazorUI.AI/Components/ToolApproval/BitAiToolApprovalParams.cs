using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiToolApproval"/> component.
/// </summary>
/// <remarks>
/// What belongs here is the look every approval prompt of a region shares - its texts, whether it offers to allow a
/// tool for good, and the detail it shows of a call. The call, the session and the decision callback are left out on
/// purpose: they are what makes one prompt the one it is. This is how an app reaches the prompts a
/// <see cref="BitAiChat"/> draws inside its tool-call cards.
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



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiToolApprovalParams))]
    public BitAiToolApprovalParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The text of the button that allows every later call of the tool as well.
    /// </summary>
    public string? ApproveAlwaysText { get; set; }

    /// <summary>
    /// The text of the button that allows the call once.
    /// </summary>
    public string? ApproveText { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the tool approval.
    /// </summary>
    public BitAiToolApprovalClassStyles? Classes { get; set; }

    /// <summary>
    /// What is shown under the question and above the buttons, given the call.
    /// </summary>
    public RenderFragment<BitAiToolCallPart>? DetailTemplate { get; set; }

    /// <summary>
    /// The text of the button that declines the call.
    /// </summary>
    public string? RejectText { get; set; }

    /// <summary>
    /// Offers to allow every later call of the tool as well.
    /// </summary>
    public bool? ShowApproveAlways { get; set; }

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

        if (ApproveAlwaysText is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(ApproveAlwaysText), ApproveAlwaysText, static a => a.ApproveAlwaysText, static (a, v) => a.ApproveAlwaysText = v);
        }

        if (ApproveText is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(ApproveText), ApproveText, static a => a.ApproveText, static (a, v) => a.ApproveText = v);
        }

        if (Classes is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(Classes), Classes, static a => a.Classes, static (a, v) => a.Classes = v);
        }

        if (DetailTemplate is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(DetailTemplate), DetailTemplate, static a => a.DetailTemplate, static (a, v) => a.DetailTemplate = v);
        }

        if (RejectText is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(RejectText), RejectText, static a => a.RejectText, static (a, v) => a.RejectText = v);
        }

        if (ShowApproveAlways.HasValue)
        {
            bitAiToolApproval.TakeFromCascade(nameof(ShowApproveAlways), ShowApproveAlways.Value, static a => a.ShowApproveAlways, static (a, v) => a.ShowApproveAlways = v);
        }

        if (Styles is not null)
        {
            bitAiToolApproval.TakeFromCascade(nameof(Styles), Styles, static a => a.Styles, static (a, v) => a.Styles = v);
        }
    }
}
