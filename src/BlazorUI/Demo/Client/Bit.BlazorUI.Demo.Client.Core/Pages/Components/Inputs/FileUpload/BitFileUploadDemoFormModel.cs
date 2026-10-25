namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileUpload;

public class BitFileUploadDemoFormModel
{
    [Required(ErrorMessage = "Add at least one attachment")]
    public string? Attachments { get; set; }
}
