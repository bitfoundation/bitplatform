using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Resources;
using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Email;

public partial class ConfirmationEmail<TPage>(IBrowserContext context, Uri serverAddress)
    : TokenMagicLinkEmail<TPage>(context, serverAddress)
    where TPage : RootLayout
{
    protected override bool WaitForRedirectOnMagicLink => true;
    protected override string EmailSubject => EmailStrings.ConfirmationEmailSubject.Replace("{0}", @"\b\d{6}\b");

    protected override async Task AssertContentCore()
    {
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.WelcomeToApp);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(new Regex(EmailStrings.EmailConfirmationMessageSubtitle.Replace("{0}", ".*")));
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyToken);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyLink);
    }
}
