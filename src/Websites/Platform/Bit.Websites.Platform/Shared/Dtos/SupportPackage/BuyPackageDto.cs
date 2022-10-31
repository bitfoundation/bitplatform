using System.ComponentModel.DataAnnotations;

namespace Bit.Websites.Platform.Shared.Dtos.SupportPackage;
public class BuyPackageDto
{
    [EmailAddress]
    [Required]
    public string Email { get; set; }

    [Required]
    public string SalePackageTitle { get; set; }

    public string Message { get; set; }
}
