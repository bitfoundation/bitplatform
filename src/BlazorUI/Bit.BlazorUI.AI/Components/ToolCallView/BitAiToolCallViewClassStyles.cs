namespace Bit.BlazorUI;

public class BitAiToolCallViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiToolCallView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header (the button that expands and collapses the card) of the BitAiToolCallView.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the expander chevron of the BitAiToolCallView.
    /// </summary>
    public string? ExpanderIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the tool icon of the BitAiToolCallView.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title of the BitAiToolCallView.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the status (the busy dots, or the duration of a finished call) of the BitAiToolCallView.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the busy dots (a BitBouncingDotsLoading) of a running call of the BitAiToolCallView.
    /// </summary>
    public BitLoadingClassStyles? Spinner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the progress bar (a BitProgress) of the BitAiToolCallView.
    /// </summary>
    public BitProgressClassStyles? Progress { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of the BitAiToolCallView.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the progress message in the body of the BitAiToolCallView.
    /// </summary>
    public string? ProgressMessage { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each section (the arguments, the result) of the body of the BitAiToolCallView.
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label (the heading) of each section of the BitAiToolCallView.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the raw text of the arguments or of a textual result of the BitAiToolCallView.
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the approval prompt (a BitAiToolApproval) of the BitAiToolCallView.
    /// </summary>
    public BitAiToolApprovalClassStyles? Approval { get; set; }
}
