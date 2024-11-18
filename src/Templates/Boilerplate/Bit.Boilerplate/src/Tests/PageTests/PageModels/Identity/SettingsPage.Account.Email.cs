using System.Text.RegularExpressions;
using Boilerplate.Tests.Extensions;
using Boilerplate.Tests.PageTests.PageModels.Email;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SettingsPage
{
    public async Task ClickOnEmailTab()
    {
        await Page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Email }).ClickAsync();
    }

    public async Task AssertEmailTab(string userEmail = TestData.DefaultTestEmail)
    {
        var emailInput = Page.GetByLabel(AppStrings.Email, new() { Exact = true }).Locator("span");
        await Assertions.Expect(emailInput).ToBeVisibleAsync();
        await Assertions.Expect(emailInput).ToContainTextAsync(userEmail);
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.NewEmailPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmMessageInProfile);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Confirm })).ToBeVisibleAsync();
    }

    public async Task ChangeEmail(string newEmail)
    {
        this.newEmail = newEmail;
        await Page.GetByPlaceholder(AppStrings.NewEmailPlaceholder).FillAsync(newEmail);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit }).ClickAsync();
    }

    public async Task AssertChangeEmail()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.SuccessfulSendChangeEmailTokenMessage)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmEmailSubtitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmEmailMessage);
        var emailInput = Page.GetByPlaceholder(AppStrings.EmailPlaceholder);
        await Assertions.Expect(emailInput).ToBeVisibleAsync();
        await Assertions.Expect(emailInput).ToBeDisabledAsync();
        await Assertions.Expect(emailInput).Not.ToBeEditableAsync();
        await Assertions.Expect(emailInput).ToHaveValueAsync(newEmail);
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.NotReceivedEmailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.CheckSpamMailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResendEmailTokenButtonText })).ToBeVisibleAsync();
    }

    public async Task AssertTooManyRequestsForChangeEmail()
    {
        var pattern = new Regex(AppStrings.WaitForEmailTokenRequestResendDelay.Replace("{0}", ".*"));
        await Assertions.Expect(Page.GetByText(pattern)).ToBeVisibleAsync();
    }

    public async Task<ConfirmationEmail<SettingsPage>> OpenConfirmationEmail()
    {
        Assert.IsNotNull(newEmail, $"Call {nameof(ChangeEmail)} method first.");

        var confirmationEmail = new ConfirmationEmail<SettingsPage>(Page.Context, WebAppServerAddress);
        await confirmationEmail.Open(newEmail);
        return confirmationEmail;
    }

    public async Task ConfirmEmailByToken(string token)
    {
        await Page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder).FillAsync(token);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText }).ClickAsync();
        await Page.WaitForHydrationToComplete();
    }

    public async Task AssertConfirmEmailSuccess()
    {
        await Assertions.Expect(Page.Locator(".bit-prs.persona").Last).ToContainTextAsync(newEmail);
        await ExpandAccount();
        await AssertExpandAccount(newEmail);
    }

    public async Task AssertEmailInvalidToken()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.InvalidToken)).ToBeVisibleAsync();
    }
}
