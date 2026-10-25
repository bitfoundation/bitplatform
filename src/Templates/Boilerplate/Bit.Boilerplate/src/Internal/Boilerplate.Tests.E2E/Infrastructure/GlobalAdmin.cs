using OtpNet;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The deployment's global admin, whose email, password and authenticator key are in this project's user secrets
/// (GlobalAdminEmail, GlobalAdminPassword and GlobalAdminAuthenticatorKey).
/// </summary>
public static class GlobalAdmin
{
    /// <summary>
    /// The authenticator's shared key. Taken the way the app's two factor settings show it too - lowercase, in space
    /// separated groups - since that is where it gets copied from, while Base32 itself has no spaces.
    /// </summary>
    public static byte[] SharedKey(string authenticatorKey)
        => Base32Encoding.ToBytes(string.Concat(authenticatorKey.Where(character => char.IsWhiteSpace(character) is false)));

    /// <summary>The code the global admin's authenticator app shows right now.</summary>
    public static string TwoFactorCode(string authenticatorKey) => new Totp(SharedKey(authenticatorKey)).ComputeTotp();
}
