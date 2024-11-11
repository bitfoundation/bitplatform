using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil11HistoryPage
{
    private string? historyLength;

    private bool isScrollRestorationManual;

    private double delta;

    private string pushStateUrl = "butil/history#push-state";

    private string replaceStateUrl = "butil/history#replace-state";


    private async Task GetLength()
    {
        var result = await history.GetLength();
        historyLength = result.ToString();
    }

    private async Task SetScrollRestoration()
    {
        await history.SetScrollRestoration(isScrollRestorationManual ? ScrollRestoration.Manual : ScrollRestoration.Auto);
    }

    private async Task Go()
    {
        await history.Go((int)delta);
    }

    private async Task PushState()
    {
        await history.PushState(url: pushStateUrl);
    }

    private async Task ReplaceState()
    {
        await history.ReplaceState(url: replaceStateUrl);
    }


    private string getLengthExampleCode =
@"@inject Bit.Butil.History history

<BitButton OnClick=""@GetLength"">GetLength</BitButton>

<div>History length is: @historyLength</div>

@code {
    private string? historyLength;

    private async Task GetLength()
    {
        var result = await history.GetLength();
        historyLength = result.ToString();
    }
}";
    private string setScrollRestorationExampleCode =
@"@inject Bit.Butil.History history

<BitCheckbox @bind-Value=""isScrollRestorationManual"" Label=""@(isScrollRestorationManual ? ""Manual"" : ""Auto"")"" />

<BitButton OnClick=""@SetScrollRestoration"">SetScrollRestoration</BitButton>

@code {
    private bool isScrollRestorationManual;

    private async Task SetScrollRestoration()
    {
        await history.SetScrollRestoration(isScrollRestorationManual ? ScrollRestoration.Manual : ScrollRestoration.Auto);
    }
}";
    private string goBackExampleCode =
@"@inject Bit.Butil.History history

<BitButton OnClick=""@(() => history.GoBack())"">GoBack</BitButton>";
    private string goForwardExampleCode =
@"@inject Bit.Butil.History history

<BitButton OnClick=""@(() => history.GoForward())"">GoForward</BitButton>";
    private string goExampleCode =
@"@inject Bit.Butil.History history

<BitSpinButton @bind-Value=""delta"" Mode=""BitSpinButtonMode.Inline"" />

<BitButton OnClick=""@Go"">Go</BitButton>

@code {
    private double delta;

    private async Task Go()
    {
        await history.Go((int)delta);
    }
}";
    private string pushStateExampleCode =
@"@inject Bit.Butil.History history

<BitTextField @bind-Value=""pushStateUrl"" Style=""max-width: 18.75rem;"" />

<BitButton OnClick=""@PushState"">PushState</BitButton>

@code {
    private string pushStateUrl = ""butil/history#push-state"";

    private async Task PushState()
    {
        await history.PushState(url: pushStateUrl);
    }
}";
    private string replaceStateExampleCode =
@"@inject Bit.Butil.History history

<BitTextField @bind-Value=""replaceStateUrl"" Style=""max-width: 18.75rem;"" />

<BitButton OnClick=""@ReplaceState"">ReplaceState</BitButton>

@code {
    private string replaceStateUrl = ""butil/history#replace-state"";

    private async Task ReplaceState()
    {
        await history.ReplaceState(url: replaceStateUrl);
    }
}";
}
