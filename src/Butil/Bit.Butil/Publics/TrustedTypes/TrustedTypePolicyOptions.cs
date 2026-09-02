namespace Bit.Butil;

/// <summary>
/// The rules a <see cref="TrustedTypes.CreatePolicy"/> policy enforces.
/// </summary>
/// <remarks>
/// The rules are declared rather than written as callbacks, because a Trusted Types transform has to
/// run synchronously and every call back into .NET is asynchronous. What is here is what a
/// hand-written policy usually does anyway: sanitize the HTML, and allow script URLs only from
/// origins you named.
/// </remarks>
public class TrustedTypePolicyOptions
{
    /// <summary>
    /// Whether the policy sanitizes the HTML it is given (the default). When false the policy hands
    /// the markup through unchanged, which is only appropriate for markup your own code produced.
    /// </summary>
    /// <remarks>
    /// With sanitizing on, a browser that has no sanitizing sink makes the policy fail rather than
    /// pass the markup through - a policy that silently stopped sanitizing would be worse than no
    /// policy at all.
    /// </remarks>
    public bool SanitizeHtml { get; set; } = true;

    /// <summary>
    /// Script URLs the policy will produce, by prefix, e.g.
    /// <c>["https://cdn.example.com/", "/_content/"]</c>. Anything else is refused.
    /// Empty - the default - refuses every script URL.
    /// </summary>
    public string[] AllowedScriptUrlPrefixes { get; set; } = [];

    /// <summary>
    /// Whether the policy will produce inline script text at all. False by default, and worth
    /// leaving that way: this is the sink Trusted Types exists to close.
    /// </summary>
    public bool AllowScript { get; set; }
}
