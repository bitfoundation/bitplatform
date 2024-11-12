using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class ConfirmPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    public override string PagePath => Urls.ConfirmPage;
    public override string PageTitle => AppStrings.ConfirmPageTitle;
    public string? EmailAddress { private get; init; }

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmTitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmEmailSubtitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ConfirmEmailMessage);
        var emailInput = Page.GetByPlaceholder(AppStrings.EmailPlaceholder);
        if (EmailAddress is null)
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeEnabledAsync();
            await Assertions.Expect(emailInput).ToBeEditableAsync();
        }
        else
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeDisabledAsync();
            await Assertions.Expect(emailInput).Not.ToBeEditableAsync();
        }
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.NotReceivedEmailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.CheckSpamMailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResendEmailTokenButtonText })).ToBeVisibleAsync();
    }

    public async Task<IdentityHomePage> ConfirmByToken(string token, string? email = null)
    {
        Assert.IsTrue(EmailAddress is not null || email is not null, "Either email address from query string or email input is required");
        Assert.IsTrue(EmailAddress is null || email is null, "Both email address from query string and email input cannot be used at the same time");

        if (email is not null)
            await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);

        await Page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder).FillAsync(token);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText }).ClickAsync();

        return new(Page, WebAppServerAddress);
    }

    public async Task AssertInvalidToken()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.InvalidToken)).ToBeVisibleAsync();
    }
}
