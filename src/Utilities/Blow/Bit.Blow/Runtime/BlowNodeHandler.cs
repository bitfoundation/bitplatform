namespace Bit.Blow;

/// <summary>
/// Runs one node: reads its inputs from <see cref="BlowNodeRunContext"/>, does the work, and says which of its outputs
/// carry a value on to the nodes after it. Throw to fail the node; throw <see cref="BlowNodeException"/> for a message
/// that may be shown to the user.
/// </summary>
public delegate Task<BlowNodeResult> BlowNodeHandler(BlowNodeRunContext context);
