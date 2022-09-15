using System.ComponentModel.DataAnnotations;

namespace Bit.Websites.Platform.Shared.Dtos.ContactUs;
public class ContactUsDto
{
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Message { get; set; }
}
