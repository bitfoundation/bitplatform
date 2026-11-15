namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.FocusOnInvalidSubmit;

public partial class BitFocusOnInvalidSubmitDemo
{
    private readonly string example1RazorCode = @"
<EditForm Model=""model"" OnValidSubmit=""HandleValidSubmit"" novalidate>
    <DataAnnotationsValidator />
    <BitFocusOnInvalidSubmit />

    <BitStack Gap=""1rem"">
        <BitTextField Label=""Name"" Required ShowValidationMessage @bind-Value=""model.Name"" />
        <BitTextField Label=""Email"" Required ShowValidationMessage @bind-Value=""model.Email"" />
        <BitChoiceGroup Label=""Plan"" Required ShowValidationMessage Items=""planItems"" @bind-Value=""model.Plan"" />
        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </BitStack>
</EditForm>";
    private readonly string example1CsharpCode = @"
public class BitFocusOnInvalidSubmitDemoModel
{
    [Required(ErrorMessage = ""Enter your name."")]
    public string? Name { get; set; } = ""Ada"";

    [Required(ErrorMessage = ""Enter your email."")]
    [EmailAddress(ErrorMessage = ""Enter an email address."")]
    public string? Email { get; set; }

    [Required(ErrorMessage = ""Pick a plan."")]
    public string? Plan { get; set; }
}

private BitFocusOnInvalidSubmitDemoModel model = new();

private readonly List<BitChoiceGroupItem<string>> planItems =
[
    new() { Text = ""Free"", Value = ""free"" },
    new() { Text = ""Team"", Value = ""team"" },
    new() { Text = ""Enterprise"", Value = ""enterprise"" },
];

private void HandleValidSubmit() { }";
}
