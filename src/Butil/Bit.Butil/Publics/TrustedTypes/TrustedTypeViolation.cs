namespace Bit.Butil;

/// <summary>
/// One Trusted Types violation, from the document's
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SecurityPolicyViolationEvent">securitypolicyviolation</see>
/// event.
/// </summary>
/// <remarks>
/// This is how an app finds the sinks it still writes to as plain strings, which is why a rollout
/// starts with the CSP in report-only mode: every violation reported there is a call site to fix
/// before the policy is enforced.
/// </remarks>
public class TrustedTypeViolation
{
    /// <summary>The directive that was violated, e.g. <c>require-trusted-types-for</c>.</summary>
    public string Directive { get; set; } = string.Empty;

    /// <summary>A short prefix of the offending value - enough to recognize it, truncated by the browser.</summary>
    public string Sample { get; set; } = string.Empty;

    /// <summary>The script file the assignment came from.</summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>The line in <see cref="SourceFile"/>.</summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// <c>"enforce"</c> when the assignment was blocked, <c>"report"</c> when the policy is
    /// report-only and it went through.
    /// </summary>
    public string Disposition { get; set; } = string.Empty;
}
