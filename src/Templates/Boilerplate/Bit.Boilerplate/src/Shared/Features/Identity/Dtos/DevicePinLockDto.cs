namespace Boilerplate.Shared.Features.Identity.Dtos;

public partial class DevicePinLockDto
{
    public byte[] Salt { get; set; } = [];

    public byte[] Iv { get; set; } = [];

    public byte[] PrivateKey { get; set; } = [];

    public byte[] PublicKey { get; set; } = [];
}
