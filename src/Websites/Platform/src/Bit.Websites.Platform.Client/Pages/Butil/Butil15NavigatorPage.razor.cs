using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil15NavigatorPage
{
    private string? deviceMemory;

    private string? hardwareConcurrency;

    private string? language;

    private string? languages;

    private string? maxTouchPoints;

    private string? isOnLine;

    private string? isPdfViewerEnabled;

    private string? userAgent;

    private string? canShare;

    private string? textValue;
    private string? titleValue;
    private string? urlValue;

    private int[] sosPattern = [100, 30, 100, 30, 100, 30, 200, 30, 200, 30, 200, 30, 100, 30, 100, 30, 100];


    private async Task GetDeviceMemory()
    {
        var result = await navigator.GetDeviceMemory();
        deviceMemory = result.ToString();
    }

    private async Task GetHardwareConcurrency()
    {
        var result = await navigator.GetHardwareConcurrency();
        hardwareConcurrency = result.ToString();
    }

    private async Task GetLanguage()
    {
        var result = await navigator.GetLanguage();
        language = result.ToString();
    }

    private async Task GetLanguages()
    {
        var result = await navigator.GetLanguages();
        languages = string.Join(",", result);
    }

    private async Task GetMaxTouchPoints()
    {
        var result = await navigator.GetMaxTouchPoints();
        maxTouchPoints = result.ToString();
    }

    private async Task GetIsOnLine()
    {
        var result = await navigator.IsOnLine();
        isOnLine = result.ToString();
    }

    private async Task GetIsPdfViewerEnabled()
    {
        var result = await navigator.IsPdfViewerEnabled();
        isPdfViewerEnabled = result.ToString();
    }

    private async Task GetUserAgent()
    {
        var result = await navigator.GetUserAgent();
        userAgent = result.ToString();
    }

    private async Task GetCanShare()
    {
        var result = await navigator.CanShare();
        canShare = result.ToString();
    }

    private async Task Share()
    {
        var shareData = new ShareData()
        {
            Text = textValue,
            title = titleValue,
            url = urlValue
        };

        await navigator.Share(shareData);
    }


    private string getDeviceMemoryExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetDeviceMemory"">GetDeviceMemory</BitButton>

<div>Device memory is: @deviceMemory</div>

@code {
    private string? deviceMemory;

    private async Task GetDeviceMemory()
    {
        var result = await navigator.GetDeviceMemory();
        deviceMemory = result.ToString();
    }
}";
    private string getHardwareConcurrencyExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetHardwareConcurrency"">GetHardwareConcurrency</BitButton>

<div>Hardware concurrency is: @hardwareConcurrency</div>

@code {
    private string? hardwareConcurrency;

    private async Task GetHardwareConcurrency()
    {
        var result = await navigator.GetHardwareConcurrency();
        hardwareConcurrency = result.ToString();
    }
}";
    private string getLanguageExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetLanguage"">GetLanguage</BitButton>

<div>Language: @language</div>

@code {
    private string? language;

    private async Task GetLanguage()
    {
        var result = await navigator.GetLanguage();
        language = result.ToString();
    }
}";
    private string getLanguagesExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetLanguages"">GetLanguages</BitButton>

<div>Languages: @languages</div>

@code {
    private string? languages;

    private async Task GetLanguages()
    {
        var result = await navigator.GetLanguages();
        languages = string.Join("","", result);
    }
}";
    private string getMaxTouchPointsExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetMaxTouchPoints"">GetMaxTouchPoints</BitButton>

<div>Max touch points: @maxTouchPoints</div>

@code {
    private string? maxTouchPoints;

    private async Task GetMaxTouchPoints()
    {
        var result = await navigator.GetMaxTouchPoints();
        maxTouchPoints = result.ToString();
    }
}";
    private string isOnLineExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetIsOnLine"">GetIsOnLine</BitButton>

<div>Is OnLine: @isOnLine</div>

@code {
    private string? isOnLine;

    private async Task GetIsOnLine()
    {
        var result = await navigator.IsOnLine();
        isOnLine = result.ToString();
    }
}";
    private string isPdfViewerEnabledExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetIsPdfViewerEnabled"">GetIsPdfViewerEnabled</BitButton>

<div>Is Pdf viewer enabled: @isPdfViewerEnabled</div>

@code {
    private string? isPdfViewerEnabled;

    private async Task GetIsPdfViewerEnabled()
    {
        var result = await navigator.IsPdfViewerEnabled();
        isPdfViewerEnabled = result.ToString();
    }
}";
    private string getUserAgentExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetUserAgent"">GetUserAgent</BitButton>

<div>User agent: @userAgent</div>

@code {
    private string? userAgent;

    private async Task GetUserAgent()
    {
        var result = await navigator.GetUserAgent();
        userAgent = result.ToString();
    }
}";
    private string canShareExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@GetCanShare"">GetCanShare</BitButton>

<div>Can share: @canShare</div>

@code {
    private string? canShare;

    private async Task CanShare()
    {
        var result = await navigator.CanShare();
        canShare = result.ToString();
    }
}";
    private string clearAppBadgeExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@(() => navigator.ClearAppBadge())"">ClearAppBadge</BitButton>";
    private string sendBeaconExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@(() => navigator.SendBeacon())"">SendBeacon</BitButton>";
    private string setAppBadgeExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@(() => navigator.SetAppBadge())"">SetAppBadge</BitButton>";
    private string shareExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitTextField @bind-Value=""textValue"" Label=""Text"" />

<BitTextField @bind-Value=""titleValue"" Label=""Title"" />

<BitTextField @bind-Value=""urlValue"" Label=""Url"" />

<BitButton OnClick=""Share"">Share</BitButton>

@code {
    private string? textValue;
    private string? titleValue;
    private string? urlValue;

    private async Task Share()
    {
        var shareData = new ShareData()
        {
            Text = textValue,
            title = titleValue,
            url = urlValue
        };

        await navigator.Share(shareData);
    }
}";
    private string vibrateExampleCode =
@"@inject Bit.Butil.Navigator navigator

<BitButton OnClick=""@(() => navigator.Vibrate(sosPattern))"">Vibrate</BitButton>

@code {
    private int[] sosPattern = [100, 30, 100, 30, 100, 30, 200, 30, 200, 30, 200, 30, 100, 30, 100, 30, 100];
}";
}
