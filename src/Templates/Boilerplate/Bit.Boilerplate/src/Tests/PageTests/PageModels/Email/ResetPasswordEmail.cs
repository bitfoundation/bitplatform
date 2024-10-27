using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Resources;
using Boilerplate.Tests.PageTests.PageModels.Identity;

namespace Boilerplate.Tests.PageTests.PageModels.Email;

public partial class ResetPasswordEmail(IBrowserContext context, Uri serverAddress)
    : TokenMagicLinkEmail<ResetPasswordPage>(context, serverAddress)
{
    protected override bool WaitForRedirectOnMagicLink => false;
    protected override string EmailSubject => EmailStrings.ResetPasswordEmailSubject.Replace("{0}", @"\b\d{6}\b");

    protected override async Task AssertContentCore()
    {
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(new Regex(EmailStrings.ResetPasswordTitle.Replace("{0}", ".*")));
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordSubtitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordBody);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordTokenMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordLinkMessage);
    }
}
