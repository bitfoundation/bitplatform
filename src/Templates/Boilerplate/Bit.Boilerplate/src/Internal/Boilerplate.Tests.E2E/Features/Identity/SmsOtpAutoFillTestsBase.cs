using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Identity;

public abstract class SmsOtpAutoFillTestsBase : AppTestBase
{
    [TestMethod]
    public async Task NewUser_Should_SignIn_ByAllowingTheTextedCode()
    {
        await SkipWithoutGlobalAdminCredentials();

        var cancellationToken = TestContext.CancellationToken;
        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken)).McpClient!;
        var phoneNumber = NewTestPhoneNumber();

        RegisterForCleanup(() => DeleteAccounts([], [phoneNumber]));

        var page = await OpenApp(App.AdminPanel);
        await WaitUntilInteractive(page);
        await GoToWhenInteractive(page, PageUrls.SignIn);

        await page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Phone, Exact = true }).ClickAsync();
        await FillPhoneNumber(page.GetByPlaceholder(AppStrings.PhoneNumberPlaceholder), phoneNumber);

        var textedBefore = await mcp.HangfireJobIds(phoneNumber, cancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendOtpButtonText }).ClickAsync();
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var sms = await mcp.WaitForHangfireJob(phoneNumber, textedBefore, cancellationToken);
        await Playwright.SendAndroidSms(sms.Argument(1)!, sms.Argument(2)!);
        await Playwright.TapAndroidButton("Allow");

        await Expect(page).Not.ToHaveURLAsync(new Regex(Regex.Escape(PageUrls.SignIn), RegexOptions.IgnoreCase));
    }
}
