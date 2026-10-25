using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileUpload;

public class BitFileUploadTestModel
{
    // The names of the files still part of the list, joined with commas: a list holding only rejected, failed,
    // canceled or removed files holds nothing a record could keep.
    [Required]
    public string? Value { get; set; }
}
