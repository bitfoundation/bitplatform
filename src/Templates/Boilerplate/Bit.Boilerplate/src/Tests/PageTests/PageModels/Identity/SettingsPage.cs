using Boilerplate.Tests.PageTests.PageModels.Layout;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SettingsPage(IPage page, Uri serverAddress)
    : IdentityLayout(page, serverAddress)
{
    private string newEmail = QueryHelpers.ParseQuery(new Uri(page.Url).Query).GetValueOrDefault("email").ToString();
    private string newPhone;
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
        var accountButton = Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.AccountSubtitle });
        var isExpanded = (await accountButton.GetAttributeAsync("aria-expanded")) is not null;
        if (isExpanded is false)
            await accountButton.ClickAsync();
    }

    public async Task AssertExpandAccount(string userEmail = TestData.DefaultTestEmail)
    {
        await AssertEmailTab(userEmail);
    }
}
