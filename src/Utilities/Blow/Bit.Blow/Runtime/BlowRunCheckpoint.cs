namespace Bit.Blow;

/// <summary>
/// What a run left behind for a later one to continue from: the results of the nodes that succeeded. Their values are
/// whatever the handlers returned; saving a checkpoint is the application's, as only it knows their types.
/// </summary>
/// <param name="RunId">The run it was taken from.</param>
/// <param name="Results">The results of the nodes that succeeded, by node id.</param>
public sealed record BlowRunCheckpoint(string RunId, IReadOnlyDictionary<string, BlowNodeResult> Results);
