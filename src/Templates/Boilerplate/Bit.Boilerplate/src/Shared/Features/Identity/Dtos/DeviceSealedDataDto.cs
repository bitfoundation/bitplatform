namespace Boilerplate.Shared.Features.Identity.Dtos;

public partial class DeviceSealedDataDto
{
    public byte[] PublicKey { get; set; } = [];

    public byte[] Iv { get; set; } = [];

    public byte[] Data { get; set; } = [];
}
