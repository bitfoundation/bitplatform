using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Forms;

public partial class FormsPage
{
    private FormsPageSignUpModel signUpModel = new();
    private bool signUpDone;

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

    private readonly FormsPageAccountModel accountModel = new();
    private EditContext accountEditContext = default!;
    private ValidationMessageStore accountServerMessages = default!;
    private BitTextField userNameField = default!;
    private bool accountSaving;
    private string? savedUserName;



    protected override Task OnInitAsync()
    {
        accountEditContext = new(accountModel);
        accountServerMessages = new(accountEditContext);

        // A message of the server is about the value it was sent: editing the field, or submitting again, drops it.
        accountEditContext.OnFieldChanged += (_, e) => accountServerMessages.Clear(e.FieldIdentifier);
        accountEditContext.OnValidationRequested += (_, _) => accountServerMessages.Clear();

        return base.OnInitAsync();
    }



    private void ResetSignUp()
    {
        signUpModel = new();
        signUpDone = false;
    }

    private async Task SaveAccount()
    {
        savedUserName = null;
        accountSaving = true;

        var taken = await IsTakenOnTheServer(accountModel.UserName!);

        accountSaving = false;

        if (taken)
        {
            accountServerMessages.Add(accountEditContext.Field(nameof(accountModel.UserName)), $"\"{accountModel.UserName}\" is taken. Try another user name.");
            accountEditContext.NotifyValidationStateChanged();

            // After the notification, so the field already describes itself with the message when the focus arrives.
            await userNameField.FocusAsync();
            return;
        }

        savedUserName = accountModel.UserName;
    }

    // Stands in for a call to the server.
    private static async Task<bool> IsTakenOnTheServer(string userName)
    {
        await Task.Delay(800);

        return userName.Trim().ToLowerInvariant() is "admin" or "root" or "bit";
    }
}
