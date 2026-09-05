namespace Bit.Butil;

/// <summary>
/// What one <see cref="FedCm.Get"/> asks for: the providers that may be used, and how the browser
/// should present the choice.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get#identity">CredentialsContainer.get() identity option</see>
/// </summary>
public class FedCmOptions
{
    /// <summary>
    /// The identity providers on offer. Chromium currently honours only the first entry, so order
    /// it by preference rather than counting on a chooser across providers.
    /// </summary>
    public required FedCmProvider[] Providers { get; set; }

    /// <summary>
    /// The wording of the browser's dialog: <c>"signin"</c> (the default), <c>"signup"</c>,
    /// <c>"use"</c> or <c>"continue"</c>. It changes the sentence the user reads, nothing else.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// How much UI the browser may show. <see cref="CredentialMediation.Optional"/> allows the
    /// silent auto-reauthentication FedCM is built around; <see cref="CredentialMediation.Required"/>
    /// forces the account chooser.
    /// </summary>
    public CredentialMediation Mediation { get; set; } = CredentialMediation.Optional;
}
