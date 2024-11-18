using System.Text.RegularExpressions;
using Boilerplate.Tests.Extensions;
using Boilerplate.Tests.PageTests.PageModels.Layout;
using Boilerplate.Tests.Services;

namespace Boilerplate.Tests.PageTests.PageModels.Email;

public abstract partial class TokenMagicLinkEmail<TPage>(IBrowserContext context, Uri serverAddress)
    where TPage : RootLayout
{
    private const string OpenEmailFirstMessage = $"You must call {nameof(Open)} method first";
    public Uri WebAppServerAddress => serverAddress;
    public IPage Page { get; private set; } = null!;
    public string DestinationPagePath => ((TPage)Activator.CreateInstance(typeof(TPage), Page, WebAppServerAddress)!).PagePath;
    protected abstract bool WaitForRedirectOnMagicLink { get; }
    protected abstract string EmailSubject { get; }

    public virtual async Task Open(string emailAddress)
    {
        var html = EmailReaderService.GetLastEmailFor(emailAddress, EmailSubject);
        Page = await context.NewPageAsync();
        await Page.SetContentAsync(html);
    }

    public virtual async Task AssertContent()
    {
        Assert.IsNotNull(Page, OpenEmailFirstMessage);

        await AssertContentCore();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = new Uri(WebAppServerAddress, DestinationPagePath).ToString() })).ToBeVisibleAsync();
    }

    protected abstract Task AssertContentCore();

    public virtual async Task<string> GetToken()
    {
        Assert.IsNotNull(Page, OpenEmailFirstMessage);

        var token = await Page.GetByText(new Regex(@"^\d{6}$")).TextContentAsync();
        Assert.IsNotNull(token, "Confirmation token not found in email");
        return token;
    }

    public async Task<TPage> OpenMagicLink()
    {
        return await OpenMagicLink<TPage>();
    }

    public virtual async Task<TFinalPage> OpenMagicLink<TFinalPage>()
        where TFinalPage : RootLayout
    {
        Assert.IsNotNull(Page, OpenEmailFirstMessage);

        var magicLink = Page.GetByRole(AriaRole.Link, new() { Name = new Uri(WebAppServerAddress, DestinationPagePath).ToString() });
        var href = await magicLink.GetAttributeAsync("href");
        await magicLink.ClickAsync();

        if (WaitForRedirectOnMagicLink)
        {
            //When opening the magic link, destination page usually redirects to itself or another page (e.g. SettingsPage)
            //So we must wait for the redirection to complete
            await Page.WaitForURLAsync(url => url != href);
        }

        await Page.WaitForHydrationToComplete();

        return (TFinalPage)Activator.CreateInstance(typeof(TFinalPage), Page, WebAppServerAddress)!;
    }
}
