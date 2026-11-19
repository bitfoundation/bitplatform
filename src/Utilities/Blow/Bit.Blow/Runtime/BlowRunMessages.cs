namespace Bit.Blow;

/// <summary>The messages a runner gives nodes it skips, stops or times out. Replace them to translate.</summary>
public sealed class BlowRunMessages
{
    /// <summary>A node skipped because something before it failed.</summary>
    public string UpstreamFailed { get; set; } = "An earlier step failed.";

    /// <summary>A node skipped because no incoming edge carried a value (<see cref="BlowJoinMode.Any"/>).</summary>
    public string NoInput { get; set; } = "No input reached it.";

    /// <summary>A node skipped because some incoming edge carried nothing (<see cref="BlowJoinMode.All"/>).</summary>
    public string MissingInput { get; set; } = "Not every input reached it.";

    /// <summary>A node skipped because the run stopped before reaching it.</summary>
    public string Stopped { get; set; } = "The run stopped before reaching it.";

    /// <summary>A node that was working when the run was cancelled.</summary>
    public string Cancelled { get; set; } = "Cancelled.";

    /// <summary>A node skipped because it has no handler (<see cref="BlowMissingHandlerBehavior.Skip"/>).</summary>
    public string NoHandler { get; set; } = "Nothing runs this kind of step.";

    /// <summary>A failure whose exception is not meant to be shown (see <see cref="BlowRunnerOptions.DescribeError"/>).</summary>
    public string UnexpectedError { get; set; } = "Failed.";

    /// <summary>An attempt that took longer than its timeout, given the timeout.</summary>
    public Func<TimeSpan, string> TimedOut { get; set; } = t => $"Timed out after {Format(t)}.";

    /// <summary>A node that failed because the run made more attempts than <see cref="BlowRunnerOptions.MaxExecutions"/>.</summary>
    public string TooManyExecutions { get; set; } = "The run reached its limit of steps.";

    /// <summary>Retrying after a failure, given the failure's message and the next attempt.</summary>
    public Func<string, int, string> Retrying { get; set; } = (message, attempt) => $"{message.TrimEnd('.')}. Trying again (attempt {attempt}).";

    internal static string Format(TimeSpan t) => t.TotalSeconds >= 1
        ? $"{t.TotalSeconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} s"
        : $"{t.TotalMilliseconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture)} ms";
}
