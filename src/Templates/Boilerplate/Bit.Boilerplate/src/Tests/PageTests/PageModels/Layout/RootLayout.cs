using Boilerplate.Tests.Extensions;

namespace Boilerplate.Tests.PageTests.PageModels.Layout;

public abstract partial class RootLayout(IPage page, Uri serverAddress)
{
    public abstract string PagePath { get; }
    public abstract string PageTitle { get; }
    public IPage Page => page;
    public Uri WebAppServerAddress => serverAddress;

    public virtual async Task Open()
    {
        await Page.GotoAsync(new Uri(WebAppServerAddress, PagePath).ToString());
        await Page.WaitForHydrationToComplete();
    }

    public virtual async Task AssertOpen()
    {
        await Assertions.Expect(Page).ToHaveTitleAsync(PageTitle);
    }
}
