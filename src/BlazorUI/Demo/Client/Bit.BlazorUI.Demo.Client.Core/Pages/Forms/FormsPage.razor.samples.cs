namespace Bit.BlazorUI.Demo.Client.Core.Pages.Forms;

public partial class FormsPage
{
    private readonly string example1RazorCode = """
@if (done)
{
    <BitMessage Color="BitColor.Success">Account created for @model.Name.</BitMessage>
}
else
{
    <EditForm Model="model" OnValidSubmit="() => done = true" novalidate>
        <DataAnnotationsValidator />
        <BitFocusOnInvalidSubmit />

        <BitStack Gap="1rem">
            <BitTextField Label="Name" Required ShowValidationMessage @bind-Value="model.Name" />
            <BitTextField Label="Email" Type="BitInputType.Email" Required ShowValidationMessage @bind-Value="model.Email" />
            <BitNumberField Label="Age" Required ShowValidationMessage @bind-Value="model.Age" />
            <BitDropdown Label="Team" Placeholder="Pick a team" Required ShowValidationMessage Items="teamItems" @bind-Value="model.Team" />
            <BitDatePicker Label="Start date" Required ShowValidationMessage @bind-Value="model.StartDate" />
            <BitChoiceGroup Label="Plan" Horizontal Required ShowValidationMessage Items="planItems" @bind-Value="model.Plan" />
            <BitCheckbox Label="I accept the terms" ShowValidationMessage @bind-Value="model.AcceptTerms" />
            <BitButton ButtonType="BitButtonType.Submit">Create account</BitButton>
        </BitStack>
    </EditForm>
}
""";

    private readonly string example1CsharpCode = """
private SignUpModel model = new();
private bool done;

private readonly List<BitDropdownItem<string>> teamItems =
[
    new() { Text = "Design", Value = "design" },
    new() { Text = "Engineering", Value = "engineering" },
    new() { Text = "Sales", Value = "sales" },
];

private readonly List<BitChoiceGroupItem<string>> planItems =
[
    new() { Text = "Free", Value = "free" },
    new() { Text = "Team", Value = "team" },
    new() { Text = "Enterprise", Value = "enterprise" },
];
""";

    private const string example1ModelCode = """
using System.ComponentModel.DataAnnotations;

public class SignUpModel
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
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("SignUpModel.cs", example1ModelCode),
    ];

    private readonly string example2RazorCode = """
<EditForm EditContext="editContext" OnValidSubmit="Save" novalidate>
    <DataAnnotationsValidator />
    <BitFocusOnInvalidSubmit />

    <BitStack Gap="1rem">
        <BitTextField @ref="userNameField"
                      Label="User name"
                      Required
                      ShowValidationMessage
                      @bind-Value="model.UserName" />
        <BitButton ButtonType="BitButtonType.Submit" IsLoading="saving">Save</BitButton>
    </BitStack>
</EditForm>
""";

    private readonly string example2CsharpCode = """
private readonly AccountModel model = new();
private EditContext editContext = default!;
private ValidationMessageStore serverMessages = default!;
private BitTextField userNameField = default!;
private bool saving;

protected override void OnInitialized()
{
    editContext = new(model);
    serverMessages = new(editContext);

    // A message of the server is about the value it was sent: editing the field, or submitting again, drops it.
    editContext.OnFieldChanged += (_, e) => serverMessages.Clear(e.FieldIdentifier);
    editContext.OnValidationRequested += (_, _) => serverMessages.Clear();
}

private async Task Save()
{
    saving = true;

    var taken = await IsTakenOnTheServer(model.UserName!);

    saving = false;

    if (taken)
    {
        serverMessages.Add(editContext.Field(nameof(model.UserName)), $"\"{model.UserName}\" is taken. Try another user name.");
        editContext.NotifyValidationStateChanged();

        // After the notification, so the field already describes itself with the message when the focus arrives.
        await userNameField.FocusAsync();
    }
}

// Stands in for a call to the server.
private static async Task<bool> IsTakenOnTheServer(string userName)
{
    await Task.Delay(800);

    return userName.Trim().ToLowerInvariant() is "admin" or "root" or "bit";
}
""";

    private const string example2ModelCode = """
using System.ComponentModel.DataAnnotations;

public class AccountModel
{
    [Required(ErrorMessage = "Pick a user name.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "A user name has 3 to 20 characters.")]
    public string? UserName { get; set; }
}
""";

    private readonly DemoCodeFile[] example2CodeFiles =
    [
        new("AccountModel.cs", example2ModelCode),
    ];
}
