using Fido2NetLib.Objects;

namespace Boilerplate.Server.Api.Models.Identity;

public class WebAuthnCredential
{
    public required byte[] Id { get; set; }
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public byte[]? PublicKey { get; set; }

    public uint SignCount { get; set; }

    public AuthenticatorTransport[]? Transports { get; set; }

    public bool IsBackupEligible { get; set; }

    public bool IsBackedUp { get; set; }

    public byte[]? AttestationObject { get; set; }

    public byte[]? AttestationClientDataJson { get; set; }

    public byte[]? UserHandle { get; set; }

    public string? AttestationFormat { get; set; }

    public DateTimeOffset RegDate { get; set; }

    public Guid AaGuid { get; set; }
}

