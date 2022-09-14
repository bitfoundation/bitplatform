using System.ComponentModel.DataAnnotations;

namespace Bit.Websites.Platform.Shared.Dtos.ContactUs;
public class ContactUsDto
{
    [EmailAddress]
    [Required]
    public string Email { get; set; }

    [Required]
    public string Message { get; set; }
}
