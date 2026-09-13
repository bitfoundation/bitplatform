using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components.Pages.Settings.Account;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Which tab of the Account section is mounted when the user arrives from a change-identifier e-mail.
/// <para>
/// This is a bUnit test rather than an API one because the defect is entirely about which component gets INSTANTIATED.
/// <c>BitPivot</c> renders only the selected item's body (<c>MountAll</c> defaults to false), and it falls back to the
/// FIRST registered item when no key is supplied - which is Passwordless. So a pivot with no <c>DefaultSelectedKey</c>
/// means <c>ChangeEmailTab</c> is never constructed, its <c>OnInitAsync</c> never runs, and the token in the url the
/// server just mailed is silently never consumed. Nothing errors; the feature simply does nothing.
/// </para>
/// <para>
/// Note the contrast with the accordion one level up: <c>BitAccordion</c> renders collapsed bodies unconditionally
/// (expand/collapse is CSS only), so the two containers on this one page behave oppositely. Asserting on the rendered
/// markup is therefore the right instrument here - it is the only thing that distinguishes "mounted" from "not".
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class AccountSectionDeepLinkTests
{
    /// <summary>
    /// The finding: the url <c>UserController.SendChangeEmailToken</c> mails must land on the Email tab.
    /// </summary>
    [TestMethod]
    public async Task AccountSection_Should_SelectTheEmailTab_WhenAnEmailTokenIsInTheQuery()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(CancellationToken.None);

        await using var ctx = server.CreateBunitContext();

        // bUnit refuses to have [SupplyParameterFromQuery] properties set as ordinary parameters, and driving the query
        // through NavigationManager is also the only form that exercises the real binding. This is the exact shape of
        // the link the server sends: /settings/account?email=..&emailToken=..
        ctx.Services.GetRequiredService<NavigationManager>()
           .NavigateTo($"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}?email=someone%40example.com&emailToken=123456");

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters.AddChildContent<AccountSection>());

        Assert.IsTrue(cut.Markup.Contains(AppStrings.EmailTokenConfirmButtonText, StringComparison.Ordinal),
            "The Email tab's body must be mounted, otherwise ChangeEmailTab.OnInitAsync never runs and the token in the " +
            $"url is never consumed - the change-email link does nothing at all. Markup was:{Environment.NewLine}{cut.Markup}");
    }

    /// <summary>
    /// The default the fix must not disturb, and it is load-bearing for another test:
    /// <c>WebAuthnPasswordlessUITests</c> navigates to <c>/settings/account</c> with no query and immediately clicks
    /// "Enable passwordless", relying on Passwordless being the first (default) tab.
    /// </summary>
    [TestMethod]
    public async Task AccountSection_Should_SelectPasswordless_WhenThereIsNoToken()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(CancellationToken.None);

        await using var ctx = server.CreateBunitContext();

        ctx.Services.GetRequiredService<NavigationManager>()
           .NavigateTo($"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}");

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters.AddChildContent<AccountSection>());

        Assert.IsTrue(cut.Markup.Contains(AppStrings.PasswordlessTitle, StringComparison.Ordinal),
            "With no token in the query the pivot must still default to Passwordless - WebAuthnPasswordlessUITests " +
            $"depends on it. Markup was:{Environment.NewLine}{cut.Markup}");
    }

    /// <summary>
    /// The phone half of the same deep link.
    /// <para>
    /// Scope, because it is easy to over-read: this renders <c>AccountSection</c> directly, so it pins the PIVOT half
    /// only. The accordion one level up reads its <c>DefaultExpandedKey</c> once at option registration, so navigating
    /// to a section from WITHIN /settings still leaves the wrong one expanded - that is a separate, still-open defect
    /// this test deliberately cannot see and must not be read as covering.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task AccountSection_Should_SelectThePhoneTab_WhenAPhoneTokenIsInTheQuery()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(CancellationToken.None);

        await using var ctx = server.CreateBunitContext();

        ctx.Services.GetRequiredService<NavigationManager>()
           .NavigateTo($"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}?phoneNumber=%2B15550100&phoneToken=123456");

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters.AddChildContent<AccountSection>());

        Assert.IsTrue(cut.Markup.Contains(AppStrings.PhoneTokenConfirmButtonText, StringComparison.Ordinal),
            $"The Phone tab's body must be mounted. Markup was:{Environment.NewLine}{cut.Markup}");
    }
}
