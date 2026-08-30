//+:cnd:noEmit
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Server.Api.Features.Identity.Resources;

namespace Boilerplate.Tests.Features.Tenants;

/// <summary>
/// The tenant invitation is the one identity e-mail whose recipient is NOT the caller, so rendering it under the
/// ambient request culture would send it in the INVITER's language - an English-speaking admin inviting a
/// Persian-speaking member would mail them English. <c>IdentityEmailService.SendTenantInvitation</c> therefore
/// prefers the culture the recipient's most recent session reported (See <c>UserController.UpdateSession</c>), and
/// restores the inviter's culture afterwards so the rest of their request (the API response, the invitation SMS
/// composed right next to this call) stays theirs.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Localization")]
public partial class TenantInvitationEmailCultureTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TenantInvitationEmail_Should_PreferTheRecipientsSessionCulture_OverTheInvitersRequestCulture()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("There is no per-recipient language to prefer on an invariant globalization build.");
        }

        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        // A per-run recipient, so nothing another test does to the shared seeded account's sessions can shadow the
        // culture arranged here (See TestAccountUtils' rationale).
        await using var recipientScope = server.WebApp.Services.CreateAsyncScope();
        var (recipientEmail, recipientUserId) = await Identity.TestAccountUtils.CreateAndSignIn(server, recipientScope, TestContext.CancellationToken);

        // The recipient's client reports fa-IR through the real write path (See UserController.UpdateSession and
        // AppClientCoordinator.UpdateUserSession).
        await recipientScope.ServiceProvider.GetRequiredService<IUserController>().UpdateSession(new()
        {
            CultureName = "fa-IR",
            AppVersion = "1.0.0-test",
            DeviceInfo = "test-device",
            PlatformType = AppPlatform.Type
        }, TestContext.CancellationToken);

        await using var inviterScope = server.WebApp.Services.CreateAsyncScope();

        // The e-mail templates read HttpContext.Request.GetWebAppUrl() for the footer links, and there is no real
        // request in a bare DI scope.
        SetCurrentHttpContext(inviterScope.ServiceProvider, server.WebAppServerAddress);

        var recipient = await inviterScope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Users.SingleAsync(user => user.Id == recipientUserId, TestContext.CancellationToken);

        // The REAL IdentityEmailService (tests normally replace it with the capture-only TestIdentityEmailService,
        // which skips rendering entirely), with only Hangfire faked - the rendered subject and body are read off the
        // enqueued delivery job, the exact hand-off point to SMTP.
        Job? enqueuedJob = null;
        var backgroundJobClient = A.Fake<IBackgroundJobClient>();
        A.CallTo(() => backgroundJobClient.Create(A<Job>._, A<IState>._))
            .Invokes((Job job, IState _) => enqueuedJob = job)
            .Returns("captured-job-id");

        var emailService = ActivatorUtilities.CreateInstance<IdentityEmailService>(inviterScope.ServiceProvider, backgroundJobClient);

        // The inviter's own request culture - the language the e-mail must NOT be rendered in.
        var originalCulture = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        var inviterCulture = CultureInfoManager.GetCultureInfo("sv-SE")!;

        try
        {
            CultureInfo.CurrentCulture = inviterCulture;
            CultureInfo.CurrentUICulture = inviterCulture;

            await emailService.SendTenantInvitation(recipient, "Inviter Adminsson", "Contoso", server.WebAppServerAddress, TestContext.CancellationToken);

            Assert.AreEqual("sv-SE", CultureInfo.CurrentUICulture.Name,
                "SendTenantInvitation must restore the inviter's culture: the rest of their request is still theirs.");
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = originalCulture;
        }

        Assert.IsNotNull(enqueuedJob, "No e-mail delivery job was enqueued.");

        // EmailServiceJobsRunner.SendEmailJob(toEmailAddress, toName, subject, body)
        Assert.AreEqual(recipientEmail, (string?)enqueuedJob.Args[0]);
        var subject = (string?)enqueuedJob.Args[2];
        // Decoded because HtmlRenderer emits non-ASCII (Persian) text as HTML entities.
        var body = System.Net.WebUtility.HtmlDecode((string?)enqueuedJob.Args[3]);

        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;
        var expectedSubject = string.Format(EmailStrings.ResourceManager.GetString(nameof(EmailStrings.TenantInvitationEmailSubject), faCulture)!, "Contoso");

        Assert.AreEqual(expectedSubject, subject,
            "The invitation's subject must be in the RECIPIENT's language - the culture their most recent session reported - not the inviter's.");
        Assert.Contains(EmailStrings.ResourceManager.GetString(nameof(EmailStrings.TenantInvitationLinkMessage), faCulture)!, body,
            "The invitation's body must be rendered in the recipient's language.");
        Assert.Contains("lang=\"fa-IR\"", body, "The e-mail document must declare the language it is actually written in.");
    }

    private static void SetCurrentHttpContext(IServiceProvider scopedServices, Uri serverAddress)
    {
        var httpContext = new DefaultHttpContext { RequestServices = scopedServices };
        httpContext.Request.Scheme = serverAddress.Scheme;
        httpContext.Request.Host = new HostString(serverAddress.Authority);

        scopedServices.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
    }
}
