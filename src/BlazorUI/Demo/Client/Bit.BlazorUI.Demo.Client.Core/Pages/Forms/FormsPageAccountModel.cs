using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Forms;

public class FormsPageAccountModel
{
    [Required(ErrorMessage = "Pick a user name.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "A user name has 3 to 20 characters.")]
    public string? UserName { get; set; }
}
