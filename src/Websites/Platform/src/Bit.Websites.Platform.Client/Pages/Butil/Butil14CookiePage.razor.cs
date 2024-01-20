using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil14CookiePage
{
    private string? newCookieName;
    private string? newCookieValue;
    private string? currentCookieName;
    private string? currentCookieValue;

    private string? currentCookies;

    private string? cookieName;


    private async Task SetCookie()
    {
        await cookie.Set(new ButilCookie { Name = newCookieName, Value = newCookieValue });
    }

    private async Task GetCookie()
    {
        var result = await cookie.Get(currentCookieName!);
        currentCookieValue = result?.Value;
    }

    private async Task GetAllCookies()
    {
        currentCookies = string.Join<ButilCookie>(", ", await cookie.GetAll());
    }

    private async Task RemoveCookie()
    {
        await cookie.Remove(cookieName!);
    }


    private string cookieExampleCode =
@"@inject Bit.Butil.Cookie cookie

<BitTextField @bind-Value=""newCookieName"" Label=""Cookie name"" />

<BitTextField @bind-Value=""newCookieValue"" Label=""Cookie value"" />

<BitButton OnClick=""@SetCookie"">SetCookie</BitButton>

<BitTextField @bind-Value=""currentCookieName"" Label=""Cookie name"" />

<BitButton OnClick=""@GetCookie"">GetCookie</BitButton>

<div>Cookie value: @currentCookieValue</div>

@code {
    private string? newCookieName;
    private string? newCookieValue;
    private string? currentCookieName;
    private string? currentCookieValue;

    private async Task SetCookie()
    {
        await cookie.Set(new ButilCookie { Name = newCookieName, Value = newCookieValue });
    }

    private async Task GetCookie()
    {
        var result = await cookie.Get(currentCookieName!);
        currentCookieValue = result?.Value;
    }
}";
    private string cookiesExampleCode =
@"@inject Bit.Butil.Cookie cookie

<BitButton OnClick=""@GetAllCookies"">GetAllCookies</BitButton>

<div>Cookies: @currentCookies</div>

@code {
    private string? currentCookies;

    private async Task GetAllCookies()
    {
        currentCookies = string.Join<ButilCookie>("", "", await cookie.GetAll());
    }
}";
    private string removeExampleCode =
@"@inject Bit.Butil.Cookie cookie

<BitTextField @bind-Value=""cookieName"" Label=""Cookie name"" />

<BitButton OnClick=""@RemoveCookie"">RemoveCookie</BitButton>

@code {
    private string? cookieName;

    private async Task RemoveCookie()
    {
        await cookie.Remove(cookieName!);
    }
}";
}
