namespace Bit.BlazorUI;

/// <summary>
/// A capability the model can invoke.
/// </summary>
/// <remarks>
/// Implementations should be stateless and resolve per-call dependencies from
/// <see cref="BitAiToolInvocation.Services"/>, because a single instance is shared across conversations.
/// </remarks>
public interface IBitAiTool
{
    /// <summary>What the tool is called, what it does, and what it accepts.</summary>
    BitAiToolDefinition Definition { get; }

    /// <summary>
    /// Executes one call.
    /// </summary>
    /// <param name="invocation">The call to execute.</param>
    /// <param name="cancellationToken">Cancels the call. Honour it: users can stop a run mid-tool.</param>
    /// <returns>The outcome. Prefer returning a failed <see cref="BitAiToolOutcome"/> over throwing.</returns>
    ValueTask<BitAiToolOutcome> InvokeAsync(BitAiToolInvocation invocation, CancellationToken cancellationToken = default);
}

/// <summary>
/// The set of tools available to a conversation.
/// </summary>
/// <remarks>
/// Resolution is by name and case-insensitive, matching what models actually emit.
/// </remarks>
public interface IBitAiToolRegistry
{
    /// <summary>All registered tools.</summary>
    IReadOnlyCollection<IBitAiTool> Tools { get; }

    /// <summary>
    /// Finds a tool by the name the model used.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <returns>The tool, or <see langword="null"/> when nothing is registered under that name.</returns>
    IBitAiTool? Find(string name);

    /// <summary>
    /// Adds or replaces a tool.
    /// </summary>
    /// <param name="tool">The tool to register.</param>
    void Register(IBitAiTool tool);

    /// <summary>
    /// Removes a tool.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <returns><see langword="true"/> when a tool was removed.</returns>
    bool Unregister(string name);
}

/// <summary>
/// Decides whether a proposed tool call needs a human to approve it before it runs.
/// </summary>
/// <remarks>
/// Separated from the tool itself so that policy can depend on the arguments, the caller, or the
/// current user, not only on the tool identity. The default implementation approves anything
/// declared <see cref="BitAiToolRisk.Destructive"/> and honours an explicit
/// <see cref="BitAiToolDefinition.RequiresApproval"/>.
/// </remarks>
public interface IBitAiToolApprovalPolicy
{
    /// <summary>
    /// Decides whether the call needs approval.
    /// </summary>
    /// <param name="definition">The tool being called.</param>
    /// <param name="invocation">The proposed call, including its arguments.</param>
    /// <param name="cancellationToken">Cancels the decision.</param>
    /// <returns><see langword="true"/> when a human must approve before the tool runs.</returns>
    ValueTask<bool> RequiresApprovalAsync(
        BitAiToolDefinition definition,
        BitAiToolInvocation invocation,
        CancellationToken cancellationToken = default);
}
