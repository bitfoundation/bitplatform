using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil10CookiePage
{
    private string? newCookieName;
    private string? newCookieValue;
    private string? getCookieName;
    private string? getCookieValue;

    private string? getAllCookieValues;

    private string? getValueCookieName;
    private string? getValueCookieValue;

    private string? removeCookieName;


    private async Task SetCookie()
    {
        await cookie.Set(new ButilCookie { Name = newCookieName, Value = newCookieValue });
    }

    private async Task GetCookie()
    {
        var result = await cookie.Get(getCookieName!);
        getCookieValue = result?.Value;
    }

    private async Task GetAllCookies()
    {
        getAllCookieValues = string.Join<ButilCookie>(", ", await cookie.GetAll());
    }

    private async Task GetValue()
    {
        getValueCookieValue = await cookie.GetValue(getValueCookieName!);
    }

    private async Task RemoveCookie()
    {
        await cookie.Remove(removeCookieName!);
    }


    private string getSetExampleCode =
@"@inject Bit.Butil.Cookie cookie

<BitTextField @bind-Value=""newCookieName"" Label=""Cookie name"" />

<BitTextField @bind-Value=""newCookieValue"" Label=""Cookie value"" />

<BitButton OnClick=""@SetCookie"">Set</BitButton>

<BitTextField @bind-Value=""getCookieName"" Label=""Cookie name"" />

<BitButton OnClick=""@GetCookie"">Get</BitButton>

<div>Cookie value: @getCookieValue</div>

@code {
    private string? newCookieName;
    private string? newCookieValue;
    private string? getCookieName;
    private string? getCookieValue;

    private async Task SetCookie()
    {
        await cookie.Set(new ButilCookie { Name = newCookieName, Value = newCookieValue });
    }

    private async Task GetCookie()
    {
        var result = await cookie.Get(getCookieName!);
        currentCookieValue = result?.Value;
    }
}";

    private string getAllExampleCode =
@"@inject Bit.Butil.Cookie cookie

<BitButton OnClick=""@GetAllCookies"">GetAll</BitButton>

<div>Cookies: @getAllCookieValues</div>

@code {
    private string? getAllCookieValues;

    private async Task GetAllCookies()
    {
        getAllCookieValues = string.Join<ButilCookie>("", "", await cookie.GetAll());
    }
}";

    private string getValueExampleCode =
@"@inject Bit.Butil.Cookie cookie

<BitTextField @bind-Value=""getValueCookieName"" Label=""Cookie name"" Style=""max-width: 18.75rem;"" />

<BitButton OnClick=""@GetValue"">GetValue</BitButton>
                        
<div>Cookie value: @getValueCookieValue</div>

@code {
    private string? getValueCookieName;
    private string? getValueCookieValue;

    private async Task GetValue()
    {
        getValueCookieValue = await cookie.GetValue(getValueCookieName!);
    }
}";


    private string removeExampleCode =
@"@inject Bit.Butil.Cookie cookie

<BitTextField @bind-Value=""removeCookieName"" Label=""Cookie name"" />

<BitButton OnClick=""@RemoveCookie"">Remove</BitButton>

@code {
    private string? removeCookieName;

    private async Task RemoveCookie()
    {
        await cookie.Remove(removeCookieName!);
    }
}";
}
