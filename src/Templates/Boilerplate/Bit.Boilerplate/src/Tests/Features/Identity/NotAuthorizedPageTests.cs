using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components.Pages;
using Boilerplate.Client.Core.Components.Pages.Management;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public class NotAuthorizedPageTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task NotAuthorizedPage_Should_PointAUserWithoutATenant_ToTheirTenants()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        await using (var client = server.CreateAppClient())
        {
            var identityController = client.GetController<IIdentityController>();
            var email = MagicLinkSignInUtils.NewTestEmail();

            await Assert.ThrowsExactlyAsync<BadRequestException>(
                () => identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken));

            var captured = await server.WaitForCapturedEmail(email,
                capturedEmail => capturedEmail.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

            var tokens = await identityController.ConfirmEmail(new() { Email = email, Token = captured.Token }, TestContext.CancellationToken);

            await ctx.Services.GetRequiredService<AuthManager>().StoreTokens(tokens);
        }

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<NotAuthorizedPage>(page => page.Add(p => p.PageType, typeof(UsersPage))));

        cut.WaitForAssertion(() => Assert.Contains(AppStrings.SelectTenantMessage, cut.Find("section").TextContent),
            timeout: TimeSpan.FromSeconds(30));

        Assert.Contains(AppStrings.ManageMyTenants, cut.Find("section").TextContent);
        Assert.DoesNotContain(AppStrings.TryRemovingOtherSessions, cut.Find("section").TextContent,
            "A brand-new account's first session is privileged, so that is not why the page was refused.");
    }
}
