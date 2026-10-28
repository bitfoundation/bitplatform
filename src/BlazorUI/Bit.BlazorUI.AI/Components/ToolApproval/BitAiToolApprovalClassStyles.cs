namespace Bit.BlazorUI;

public class BitAiToolApprovalClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiToolApproval.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the question (or the reason the approval was asked for) of the BitAiToolApproval.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the row of the answer buttons of the BitAiToolApproval.
    /// </summary>
    public string? Actions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the decline button of the BitAiToolApproval.
    /// </summary>
    public BitButtonClassStyles? RejectButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the always-allow button of the BitAiToolApproval.
    /// </summary>
    public BitButtonClassStyles? ApproveAlwaysButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the allow-once button of the BitAiToolApproval.
    /// </summary>
    public BitButtonClassStyles? ApproveButton { get; set; }
}
