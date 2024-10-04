using Microsoft.Playwright;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.PageTests.PageModels;

public class SingUpPage(IPage page, Uri serverAddress)
{
    public async Task<IResponse> GotoAsync()
    {
        var response = await page.GotoAsync(new Uri(serverAddress, Urls.SignUpPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        //Override behavior of the javascript recaptcha function on the browser
        await page.WaitForFunctionAsync("window.grecaptcha !== undefined");
        await page.EvaluateAsync("window.grecaptcha.getResponse = () => 'not-empty';");

        return response;
    }

    public async Task SingUpAsync(bool usingMagicLink = true)
    {
        var email = $"{Guid.NewGuid()}@gmail.com";

        await page.GetByPlaceholder("Enter email address").FillAsync(email);
        await page.GetByPlaceholder("Enter password").FillAsync("123456");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign up", Exact = true }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Confirm Email Address" })).ToBeVisibleAsync();

        var html = ReadEmlAndGetHtmlBody(email);
        var emailPage = await page.Context.NewPageAsync();
        await emailPage.SetContentAsync(html);


        if (usingMagicLink)
        {
            var optLink = emailPage.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ConfirmPage).ToString() });
            await Assertions.Expect(optLink).ToBeVisibleAsync();
            await optLink.ClickAsync();
        }
        else
        {
            var optCode = await emailPage.GetByText(new Regex("^\\d{6}$")).TextContentAsync();
            await page.GetByPlaceholder("Enter code").FillAsync(optCode);
            await page.GetByRole(AriaRole.Button, new() { Name = "Confirm Email Address" }).ClickAsync();
        }

        var signInText = "Sign in"; //TODO: multi-language test
        var signOutText = "Sign out"; //TODO: multi-language test

        await Assertions.Expect(emailPage).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(emailPage.Locator(".persona")).ToBeVisibleAsync();
        await Assertions.Expect(emailPage.Locator(".persona")).ToContainTextAsync(email);
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Button, new() { Name = signOutText })).ToBeVisibleAsync();
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Button, new() { Name = signInText })).ToBeVisibleAsync(new() { Visible = false });

        await emailPage.CloseAsync();
    }

    private static string ReadEmlAndGetHtmlBody(string toMailAddress)
    {
        var emailsDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "sent-emails");
        var emlFileInfo = new DirectoryInfo(emailsDirectory).GetFiles().MaxBy(f => f.CreationTime);
        var eml = MsgReader.Mime.Message.Load(emlFileInfo);

        Assert.AreEqual(toMailAddress, eml.Headers.To[0].Address);
        Assert.AreEqual("info@Boilerplate.com", eml.Headers.From.Address);
        //TODO: multi-language test
        Assert.AreEqual("Boilerplate app", eml.Headers.From.DisplayName);
        Assert.IsTrue(Regex.IsMatch(eml.Headers.Subject, "^Boilerplate \\d{6} - Confirm your email address$"), "Email subject does not match.");

        //TODO: multi-language test for HtmlBody
        return eml.HtmlBody.GetBodyAsText();
    }
}
