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
    /// <remarks>
    /// A prefix is matched as a scope, not as text: the URL is resolved against the document, has to
    /// carry the same scheme and origin, and its path has to match on a segment boundary. So
    /// <c>"https://cdn.example.com"</c> does not admit <c>https://cdn.example.com.evil.test/x</c>, and
    /// <c>"/_content/"</c> does not admit <c>/_content/../evil.js</c>. Only <c>http</c> and
    /// <c>https</c> URLs can be allowed - the schemes with an origin to compare - and what the policy
    /// hands back is the resolved URL, so what the sink loads is what was checked.
    /// </remarks>
    public string[] AllowedScriptUrlPrefixes { get; set; } = [];

    /// <summary>
    /// Whether the policy will produce inline script text at all. False by default, and worth
    /// leaving that way: this is the sink Trusted Types exists to close.
    /// </summary>
    public bool AllowScript { get; set; }
}
