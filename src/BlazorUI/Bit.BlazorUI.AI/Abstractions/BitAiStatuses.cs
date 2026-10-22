namespace Bit.BlazorUI;

/// <summary>
/// Lifecycle of a single message.
/// </summary>
public enum BitAiMessageStatus
{
    /// <summary>Composed locally and not yet handed to a transport.</summary>
    Draft = 0,

    /// <summary>Submitted; the framework is waiting for the first event.</summary>
    Pending = 1,

    /// <summary>Events are arriving and the message is growing.</summary>
    Streaming = 2,

    /// <summary>The message finished normally.</summary>
    Complete = 3,

    /// <summary>The message was cancelled by the user or the host.</summary>
    Cancelled = 4,

    /// <summary>The message terminated with an error.</summary>
    Failed = 5,
}

/// <summary>
/// Lifecycle of a tool call, from the model proposing it through to a result.
/// </summary>
public enum BitAiToolCallStatus
{
    /// <summary>The model has named the tool but arguments are still streaming.</summary>
    Streaming = 0,

    /// <summary>Arguments are complete; the call has not started executing.</summary>
    Pending = 1,

    /// <summary>Execution is blocked until a human approves or rejects the call.</summary>
    AwaitingApproval = 2,

    /// <summary>A human rejected the call; it will not execute.</summary>
    Rejected = 3,

    /// <summary>The tool is executing.</summary>
    Running = 4,

    /// <summary>The tool produced a result.</summary>
    Completed = 5,

    /// <summary>The tool threw or returned an error.</summary>
    Failed = 6,

    /// <summary>Execution was cancelled before a result was produced.</summary>
    Cancelled = 7,
}

/// <summary>
/// Lifecycle of one step within an agent run.
/// </summary>
public enum BitAiAgentStepStatus
{
    /// <summary>Planned but not started.</summary>
    Pending = 0,

    /// <summary>Currently executing.</summary>
    Running = 1,

    /// <summary>Paused pending human input or approval.</summary>
    Blocked = 2,

    /// <summary>Finished successfully.</summary>
    Completed = 3,

    /// <summary>Finished unsuccessfully.</summary>
    Failed = 4,

    /// <summary>Abandoned because the run ended or the plan changed.</summary>
    Skipped = 5,
}

/// <summary>
/// Lifecycle of a whole agent run.
/// </summary>
public enum BitAiAgentRunStatus
{
    /// <summary>Created but not started.</summary>
    Idle = 0,

    /// <summary>Deciding what to do before producing visible output.</summary>
    Planning = 1,

    /// <summary>Executing steps.</summary>
    Running = 2,

    /// <summary>Waiting on the user: an approval, a question, or missing input.</summary>
    WaitingForInput = 3,

    /// <summary>Completed successfully.</summary>
    Completed = 4,

    /// <summary>Ended with an error.</summary>
    Failed = 5,

    /// <summary>Cancelled before completion.</summary>
    Cancelled = 6,
}

/// <summary>
/// The reason a generation stopped.
/// </summary>
public enum BitAiFinishReason
{
    /// <summary>The reason was not reported by the provider.</summary>
    Unknown = 0,

    /// <summary>The model reached a natural end.</summary>
    Stop = 1,

    /// <summary>The output hit the token limit.</summary>
    Length = 2,

    /// <summary>The model stopped to call one or more tools.</summary>
    ToolCalls = 3,

    /// <summary>The provider's safety system stopped the generation.</summary>
    ContentFilter = 4,

    /// <summary>The caller cancelled the generation.</summary>
    Cancelled = 5,

    /// <summary>The generation failed.</summary>
    Error = 6,
}

/// <summary>
/// A human decision on an approval request.
/// </summary>
public enum BitAiApprovalDecision
{
    /// <summary>No decision has been made yet.</summary>
    Undecided = 0,

    /// <summary>Approved for this invocation only.</summary>
    Approved = 1,

    /// <summary>Rejected.</summary>
    Rejected = 2,

    /// <summary>Approved, and future invocations of the same tool should not ask again.</summary>
    ApprovedAlways = 3,
}

/// <summary>
/// User sentiment attached to an assistant message.
/// </summary>
public enum BitAiFeedbackKind
{
    /// <summary>No feedback given.</summary>
    None = 0,

    /// <summary>The response was helpful.</summary>
    Positive = 1,

    /// <summary>The response was not helpful.</summary>
    Negative = 2,
}
