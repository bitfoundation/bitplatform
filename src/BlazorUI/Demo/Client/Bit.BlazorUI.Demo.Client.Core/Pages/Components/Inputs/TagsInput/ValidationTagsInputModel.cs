using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TagsInput;

public class ValidationTagsInputModel
{
    [Required(ErrorMessage = "At least one tag is required.")]
    [MinLength(1, ErrorMessage = "At least one tag is required.")]
    public ICollection<string>? Tags { get; set; }
}
