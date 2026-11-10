namespace Bit.BlazorUI;

/// <summary>
/// How a <see cref="BitAiToolCallView"/> shows a tool call (<see cref="BitAiUiOptions.ToolCallDisplay"/>).
/// </summary>
public enum BitAiToolCallDisplay
{
    /// <summary>The card: a header that expands to the arguments and the result.</summary>
    Card = 0,

    /// <summary>One line saying what the call is doing or how it ended, with no arguments or result to expand.</summary>
    Status = 1,

    /// <summary>Nothing at all, for a call that is plumbing the reader has no use for.</summary>
    Hidden = 2,
}

internal static class BitAiToolCallDisplays
{
    /// <summary>
    /// How a call is shown under the options in force. A call that needs the reader - one waiting for a decision, or one
    /// that failed - is shown as a card whatever the options say, so its question or its failure is never hidden.
    /// </summary>
    internal static BitAiToolCallDisplay Resolve(BitAiUiOptions options, BitAiToolCallPart call)
    {
        if (call.Status is BitAiToolCallStatus.AwaitingApproval or BitAiToolCallStatus.Failed) return BitAiToolCallDisplay.Card;

        return options.ToolCallDisplay?.Invoke(call) ?? BitAiToolCallDisplay.Card;
    }
}
