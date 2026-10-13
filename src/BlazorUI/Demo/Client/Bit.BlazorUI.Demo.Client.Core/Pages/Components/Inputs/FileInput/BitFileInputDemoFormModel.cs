namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileInput;

public class BitFileInputDemoFormModel
{
    [Required(ErrorMessage = "Attach your resume")]
    public string? Resume { get; set; }
}
