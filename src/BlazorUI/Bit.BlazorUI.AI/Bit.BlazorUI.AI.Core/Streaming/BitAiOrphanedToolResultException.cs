using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Describes a tool result that arrived before any call of its id, and was dropped once no such call had started within
/// <see cref="BitAiConversationStreamProcessor.ToolResultHoldTime"/>.
/// </summary>
/// <remarks>
/// A result is matched to its call by <see cref="ToolCallId"/>. A transport that reports a result ahead of the call it
/// answers - parallel streams merged out of order - has it held until the call starts, and given to the call then. One
/// whose call never starts points at nothing the conversation can show, so it is dropped: the processor raises
/// <see cref="BitAiConversationStreamProcessor.ToolResultDropped"/> with this exception, and adds an error part carrying
/// it (code <see cref="ErrorCode"/>) to the message the result arrived for. Nothing throws it: the turn goes on.
/// </remarks>
public sealed class BitAiOrphanedToolResultException : Exception
{
    /// <summary>
    /// The <see cref="BitAiError.Code"/> of the error part a dropped result leaves on its message.
    /// </summary>
    public const string ErrorCode = "orphaned_tool_result";

    /// <summary>Creates the exception of a dropped result.</summary>
    /// <param name="toolCallId">The id of the call the result was for.</param>
    /// <param name="outcome">The result that was dropped.</param>
    /// <param name="holdTime">How long it was held for its call.</param>
    public BitAiOrphanedToolResultException(string toolCallId, BitAiToolOutcome outcome, TimeSpan holdTime)
        : base(string.Create(CultureInfo.InvariantCulture, $"A result arrived for the tool call '{toolCallId}', but no call with that id started within {holdTime.TotalSeconds:0.###} second{(holdTime == TimeSpan.FromSeconds(1) ? "" : "s")}, so the result was dropped."))
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentNullException.ThrowIfNull(outcome);

        ToolCallId = toolCallId;
        Outcome = outcome;
        HoldTime = holdTime;
    }

    /// <summary>The id of the call the result was for.</summary>
    public string ToolCallId { get; }

    /// <summary>The result that was dropped.</summary>
    public BitAiToolOutcome Outcome { get; }

    /// <summary>How long the result was held for its call before it was dropped.</summary>
    public TimeSpan HoldTime { get; }
}
