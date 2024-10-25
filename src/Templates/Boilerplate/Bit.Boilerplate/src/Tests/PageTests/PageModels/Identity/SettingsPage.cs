using Boilerplate.Tests.PageTests.PageModels.Email;
using Boilerplate.Tests.PageTests.PageModels.Layout;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SettingsPage(IPage page, Uri serverAddress)
    : IdentityLayout(page, serverAddress)
{
    private string newEmail = QueryHelpers.ParseQuery(new Uri(page.Url).Query).GetValueOrDefault("email").ToString();
    public override string PagePath => Urls.SettingsPage;
    public override string PageTitle => AppStrings.Settings;

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ProfileSubtitle })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.AccountSubtitle })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaSubtitle })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SessionsSubtitle })).ToBeVisibleAsync();
    }

    public async Task ExpandAccount()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.AccountSubtitle }).ClickAsync();
    }

    public async Task AssertExpandAccount(string userEmail = "test@bitplatform.dev")
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
        await Assertions.Expect(emailInput).ToBeEditableAsync(new() { Editable = false });
        await Assertions.Expect(emailInput).ToHaveValueAsync(newEmail);
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.NotReceivedEmailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.CheckSpamMailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResendEmailTokenButtonText })).ToBeVisibleAsync();
    }

    public async Task<ConfirmationEmail<SettingsPage>> OpenConfirmationEmail()
    {
        Assert.IsNotNull(newEmail, $"Call {nameof(ChangeEmail)} method first.");

        var confirmationEmail = new ConfirmationEmail<SettingsPage>(Page.Context, WebAppServerAddress);
        await confirmationEmail.Open(newEmail);
        return confirmationEmail;
    }

    public async Task ConfirmByToken(string token)
    {
        await Page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder).FillAsync(token);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText }).ClickAsync();
    }

    public async Task AssertConfirmSuccess()
    {
        //TODO: Remove the two lines below when the problem with refreshing page is solved.
        await Page.RunAndWaitForNavigationAsync(() => Page.ReloadAsync(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(Page.Locator(".bit-prs.persona").Last).ToContainTextAsync(newEmail);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.AccountSubtitle }).ClickAsync();
        var emailInput = Page.GetByLabel(AppStrings.Email, new() { Exact = true }).Locator("span");
        await Assertions.Expect(emailInput).ToBeVisibleAsync();
        await Assertions.Expect(emailInput).ToContainTextAsync(newEmail);
    }

    public async Task AssertInvalidToken()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.InvalidToken)).ToBeVisibleAsync();
    }
}
