using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Extensions;
public static partial class PlaywrightHydrationExtensions
{
    public static async Task WaitForHydrationToComplete(this IPage page)
    {
        await page.WaitForFunctionAsync("document.title != 'Before-Hydration'");
        //await Assertions.Expect(page.Locator("title")).Not.ToHaveTextAsync("Before-Hydration");
    }

    public static Task EnableHydrationCheck(this IBrowserContext context) => context.RouteAsync("**/*", ChangeTitleHandler);

    public static Task EnableHydrationCheck(this IPage page) => page.RouteAsync("**/*", ChangeTitleHandler);

    public static Task DisableHydrationCheck(this IBrowserContext context) => context.UnrouteAsync("**/*", ChangeTitleHandler);

    public static Task DisableHydrationCheck(this IPage page) => page.UnrouteAsync("**/*", ChangeTitleHandler);

    private static async Task ChangeTitleHandler(IRoute route)
    {
        var request = route.Request;
        if (request.ResourceType == "document" && request.Method == HttpMethods.Get)
        {
            var response = await route.FetchAsync();

            // Change page title to Before-Hydration to check if hydration is completed
            // It will be set to original title once after hydration is completed
            var body = await response.TextAsync();
            body = TitleRegex().Replace(body, "<title>Before-Hydration</title>");

            await route.FulfillAsync(new()
            {
                Response = response,
                Body = body,
            });
        }
        else
        {
            await route.ContinueAsync();
        }
    }

    [GeneratedRegex(@"<title>(.*?)<\/title>")]
    private static partial Regex TitleRegex();
}

