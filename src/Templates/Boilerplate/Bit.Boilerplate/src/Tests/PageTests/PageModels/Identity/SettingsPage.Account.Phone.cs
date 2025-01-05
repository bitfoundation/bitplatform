using System.Text.RegularExpressions;
using Boilerplate.Tests.Services;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SettingsPage
{
    public async Task ClickOnPhoneTab()
    {
        await Page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Phone }).ClickAsync();
    }

    public async Task AssertPhoneTab(string? phone)
    {
        var phoneInput = Page.GetByLabel(AppStrings.Phone, new() { Exact = true }).Locator("span");
        if (phone is null)
        {
            await Assertions.Expect(phoneInput).ToBeHiddenAsync();
        }
        else
        {
            await Assertions.Expect(phoneInput).ToBeVisibleAsync();
            await Assertions.Expect(phoneInput).ToContainTextAsync(phone);
        }
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.NewPhoneNumberPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmMessageInProfile);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Confirm })).ToBeVisibleAsync();
    }

    public async Task ChangePhone(string newPhone)
    {
        this.newPhone = newPhone;
        await Page.GetByPlaceholder(AppStrings.NewPhoneNumberPlaceholder).FillAsync(newPhone);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit }).ClickAsync();
    }

    public async Task AssertChangePhone()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.SuccessfulSendChangePhoneNumberTokenMessage)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmPhoneSubtitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmPhoneMessage);
        var phoneInput = Page.GetByPlaceholder(AppStrings.PhoneNumberPlaceholder);
        await Assertions.Expect(phoneInput).ToBeVisibleAsync();
        await Assertions.Expect(phoneInput).ToBeDisabledAsync();
        await Assertions.Expect(phoneInput).Not.ToBeEditableAsync();
        await Assertions.Expect(phoneInput).ToHaveValueAsync(newPhone);
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.PhoneTokenPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.PhoneTokenConfirmButtonText })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.NotReceivedPhoneMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResendPhoneTokenButtonText })).ToBeVisibleAsync();
    }

    public async Task AssertTooManyRequestsForChangePhone()
    {
        var pattern = new Regex(AppStrings.WaitForPhoneNumberTokenRequestResendDelay.Replace("{0}", ".*"));
        await Assertions.Expect(Page.GetByText(pattern)).ToBeVisibleAsync();
    }

    public string GetPhoneToken()
    {
        var pattern = AppStrings.ChangePhoneNumberTokenShortText.Replace("{0}", @"\b\d{6}\b");
        return FakePhoneService.GetLastOtpFor(newPhone, pattern);
    }

    public async Task ConfirmPhoneByToken(string token)
    {
        await Page.GetByPlaceholder(AppStrings.PhoneTokenPlaceholder).FillAsync(token);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.PhoneTokenConfirmButtonText }).ClickAsync();
    }

    public async Task AssertConfirmPhoneSuccess()
    {
        await Page.WaitForTimeoutAsync(1000); //Wait for redirection to complete
        await ExpandAccount();
        await ClickOnPhoneTab();
        await AssertPhoneTab(newPhone);
    }

    public async Task AssertPhoneInvalidToken()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.ResourceValidationException)).ToBeVisibleAsync();
    }
}
