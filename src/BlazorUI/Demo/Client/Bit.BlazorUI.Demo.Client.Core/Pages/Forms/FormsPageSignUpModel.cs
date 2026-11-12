using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Forms;

public class FormsPageSignUpModel
{
    [Required(ErrorMessage = "Enter your name.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter an email address, like name@example.com.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Enter your age.")]
    [Range(18, 120, ErrorMessage = "You have to be 18 or older.")]
    public int? Age { get; set; }

    [Required(ErrorMessage = "Pick a team.")]
    public string? Team { get; set; }

    [Required(ErrorMessage = "Pick a start date.")]
    public DateTimeOffset? StartDate { get; set; }

    [Required(ErrorMessage = "Pick a plan.")]
    public string? Plan { get; set; }

    [AllowedValues(true, ErrorMessage = "Accept the terms to create the account.")]
    public bool AcceptTerms { get; set; }
}
