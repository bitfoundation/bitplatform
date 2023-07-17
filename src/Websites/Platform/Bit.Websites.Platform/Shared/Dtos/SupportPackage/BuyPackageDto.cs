namespace Bit.Websites.Platform.Shared.Dtos.SupportPackage;

public class BuyPackageDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string SalePackageTitle { get; set; } = default!;

    public string Message { get; set; } = default!;
}
