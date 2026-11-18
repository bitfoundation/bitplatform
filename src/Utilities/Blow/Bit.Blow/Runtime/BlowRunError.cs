namespace Bit.Blow;

/// <summary>What a failed node sends along its <see cref="BlowNodeRunOptions.ErrorOutput"/>.</summary>
/// <param name="NodeId">The node that failed.</param>
/// <param name="Message">What to say about it, as safe to show as the node's run message.</param>
/// <param name="Exception">The exception, for the handler that deals with it. Not shown anywhere by the library.</param>
public sealed record BlowRunError(string NodeId, string Message, Exception? Exception);
