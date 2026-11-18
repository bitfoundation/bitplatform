namespace Bit.Blow;

/// <summary>
/// How a node runs: how long an attempt may take, how often it is tried again, and how it joins its inputs. Given per
/// node type when its handler is registered (<see cref="BlowRunner.Handle(string, BlowNodeHandler, BlowNodeRunOptions?)"/>)
/// and per node through <see cref="BlowRunnerOptions.NodeOptions"/>.
/// </summary>
public sealed record BlowNodeRunOptions
{
    /// <summary>The options a node has when nothing says otherwise: one attempt, no timeout, <see cref="BlowJoinMode.Any"/>.</summary>
    public static readonly BlowNodeRunOptions Default = new();

    /// <summary>
    /// The longest one attempt may take, or null for no limit. A slower attempt is cancelled and counts as a failure,
    /// whether or not its handler heeds the token. <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>, or longer
    /// than a timer can wait (about 49 days), is no limit.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>How often to try again after a failure. Null is <see cref="BlowRetryPolicy.None"/>.</summary>
    public BlowRetryPolicy? Retry { get; init; }

    /// <summary>Whether the node needs a value on every incoming edge or on any one of them.</summary>
    public BlowJoinMode Join { get; init; } = BlowJoinMode.Any;

    /// <summary>
    /// The id of a source handle that takes over when the node fails: its edges carry a <see cref="BlowRunError"/> to
    /// the nodes after it, which run as they would on a value, while the node's other edges carry nothing. Without
    /// one - or with one that no edge leaves from - a failure skips everything that depends on the node.
    /// </summary>
    public string? ErrorOutput { get; init; }
}
