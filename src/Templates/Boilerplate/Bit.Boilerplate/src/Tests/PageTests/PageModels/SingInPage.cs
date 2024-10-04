using Microsoft.Playwright;
using Microsoft.AspNetCore.Http;

namespace Boilerplate.Tests.PageTests.PageModels;

public partial class SingInPage(IPage page, Uri serverAddress)
{
    public bool IsSignedIn { get; private set; }

    private string enterEmailText = "Enter email address"; //TODO: multi-language test
    private string enterPasswordText = "Enter password"; //TODO: multi-language test
    private string signInText = "Sign in"; //TODO: multi-language test
    private string signOutText = "Sign out"; //TODO: multi-language test

    public async Task<IResponse?> GotoAsync()
    {
        var response = await page.GotoAsync(new Uri(serverAddress, Urls.SignInPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        return response;
    }

    public async Task SignInAsync(string email = "test@bitplatform.dev", string password = "123456")
    {
        await page.GetByPlaceholder(enterEmailText).FillAsync(email);
        await page.GetByPlaceholder(enterPasswordText).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = signInText }).ClickAsync();

        IsSignedIn = true;
    }

    public async Task SignOutAsync()
    {
        Assert.IsTrue(IsSignedIn, "You must sign-in first.");

        await page.GetByRole(AriaRole.Button, new() { Name = signOutText }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = signOutText }).ClickAsync();

        IsSignedIn = false;
    }
}
