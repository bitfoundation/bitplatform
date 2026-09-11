namespace Bit.Butil;

/// <summary>
/// How much of a session's input context is spent - what <see cref="AiSession.GetUsage"/> reports.
/// </summary>
/// <remarks>
/// A session that runs out of quota starts dropping the oldest turns of the conversation, so a long
/// chat wants this watched rather than discovered.
/// </remarks>
public class AiUsage
{
    /// <summary>Tokens the session has consumed so far.</summary>
    public double InputUsage { get; set; }

    /// <summary>The session's total token budget.</summary>
    public double InputQuota { get; set; }

    /// <summary>What is left, never negative.</summary>
    public double Remaining => InputQuota - InputUsage < 0 ? 0 : InputQuota - InputUsage;
}
