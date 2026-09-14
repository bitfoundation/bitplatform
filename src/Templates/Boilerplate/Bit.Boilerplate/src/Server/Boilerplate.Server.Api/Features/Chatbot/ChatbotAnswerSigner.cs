using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Boilerplate.Server.Api.Features.Chatbot;

/// <summary>
/// Signs every answer the assistant writes, so an answer handed back by the client - to be read aloud, or as chat
/// history after a reconnect - can be told apart from one the caller made up.
/// </summary>
public class ChatbotAnswerSigner(IDataProtectionProvider dataProtectionProvider)
{
    // The Data Protection key ring is shared by every instance and outlives a restart (See Program.Services).
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("Boilerplate.Chatbot.AnswerSignature");

    /// <summary>Travels to the client on the answer's <c>SharedAppMessages.MESSAGE_PROCESS_SUCCESS</c> marker.</summary>
    public string Sign(string answer) => protector.Protect(Fingerprint(answer));

    /// <summary>Whether this app wrote <paramref name="answer"/> word for word.</summary>
    public bool Verify(string? answer, string? signature)
    {
        if (answer is null || string.IsNullOrEmpty(signature))
            return false;

        try
        {
            return protector.Unprotect(signature) == Fingerprint(answer);
        }
        catch (CryptographicException)
        {
            return false; // Forged, tampered with, or signed by a key ring this deployment no longer has.
        }
    }

    // A fingerprint rather than the answer itself: this travels with every message of the resent history.
    private static string Fingerprint(string answer) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(answer)));
}
