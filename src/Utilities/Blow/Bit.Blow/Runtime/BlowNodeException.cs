namespace Bit.Blow;

/// <summary>
/// A failure whose message is meant to be shown: it becomes the node's run message, where other exceptions' messages
/// are kept out (see <see cref="BlowRunnerOptions.DescribeError"/>).
/// </summary>
public class BlowNodeException : Exception
{
    /// <summary>A failure with a message to show.</summary>
    public BlowNodeException(string message) : base(message) { }

    /// <summary>A failure with a message to show, and the exception behind it, kept for the application but not shown.</summary>
    public BlowNodeException(string message, Exception? innerException) : base(message, innerException) { }

    /// <summary>False for a failure another attempt would not fix - bad input, a refusal - so a retry policy skips the retries.</summary>
    public bool IsTransient { get; init; } = true;
}
