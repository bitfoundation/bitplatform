namespace Bit.Butil;

/// <summary>
/// The per-call options bag the task-specific AI sessions take: context for this one input, on top
/// of the session's shared context.
/// </summary>
internal class AiRunJsOptions
{
    public string? Context { get; set; }
}
