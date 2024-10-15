using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class ConfirmPage(IPage page, Uri serverAddress, string? emailAddress)
    : MainLayout(page, serverAddress, Urls.ConfirmPage, AppStrings.ConfirmTitle)
{
    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmTitle);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmEmailSubtitle);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmEmailMessage);
        var emailInput = page.GetByPlaceholder(AppStrings.EmailPlaceholder);
        if (emailAddress is null)
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeEnabledAsync();
            await Assertions.Expect(emailInput).ToBeEditableAsync();
        }
        else
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeDisabledAsync();
            await Assertions.Expect(emailInput).ToBeEditableAsync(new() { Editable = false });
        }
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.NotReceivedConfirmationEmailMessage);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.CheckSpamMailMessage);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResendEmailTokenButtonText })).ToBeVisibleAsync();
    }

    public async Task<IdentityLayout> ConfirmByToken(string? email, string token)
    {
        Assert.IsTrue(emailAddress is not null || email is not null, "Either email address from query string or email input is required");
        Assert.IsTrue(emailAddress is null || email is null, "Both email address from query string and email input cannot be used at the same time");

        if (email is not null)
            await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);

        await page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder).FillAsync(token);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText }).ClickAsync();

        return new(page, serverAddress, Urls.HomePage, AppStrings.HomeTitle);
    }
}
