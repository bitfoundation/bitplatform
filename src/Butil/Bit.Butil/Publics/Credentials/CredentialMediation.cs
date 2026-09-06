namespace Bit.Butil;

/// <summary>
/// How much the browser may involve the user when a credential is requested. The same setting
/// governs <see cref="Credentials"/>, <see cref="FedCm"/> and <see cref="DigitalCredentials"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get#mediation">CredentialsContainer.get() mediation</see>
/// </summary>
public enum CredentialMediation
{
    /// <summary>
    /// The browser decides: it may sign the user in silently, or show an account chooser. The
    /// default.
    /// </summary>
    Optional,

    /// <summary>
    /// No UI at all. Resolves with nothing rather than prompting - what a page uses to try
    /// auto-sign-in without disturbing a user who would have to choose.
    /// </summary>
    Silent,

    /// <summary>
    /// Always ask, even when there is exactly one credential. Use it after a sign-out, so the next
    /// sign-in is the user's choice again.
    /// </summary>
    Required,

    /// <summary>
    /// Offer the credential through the browser's autofill UI instead of a modal, leaving the page
    /// usable. Needs a form control marked up for it (<c>autocomplete="username webauthn"</c>).
    /// </summary>
    Conditional,
}
