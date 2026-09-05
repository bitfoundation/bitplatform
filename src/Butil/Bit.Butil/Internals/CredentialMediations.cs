namespace Bit.Butil;

/// <summary>
/// The wire spelling of a <see cref="CredentialMediation"/> - the string
/// <c>navigator.credentials.get()</c> expects.
/// </summary>
/// <remarks>
/// Its own type rather than a member of <see cref="Credentials"/>, because the publish-time module
/// scanner works over a call closure: a service that called into <see cref="Credentials"/> for this
/// one mapping would be treated as reaching everything <see cref="Credentials"/> reaches, and a
/// consumer injecting only <see cref="FedCm"/> would be shipped <c>credentials.js</c> as well. Nothing
/// here names a JavaScript module, so reaching it costs nothing.
/// </remarks>
internal static class CredentialMediations
{
    internal static string ToName(CredentialMediation mediation) => mediation switch
    {
        CredentialMediation.Silent => "silent",
        CredentialMediation.Required => "required",
        CredentialMediation.Conditional => "conditional",
        _ => "optional"
    };
}
