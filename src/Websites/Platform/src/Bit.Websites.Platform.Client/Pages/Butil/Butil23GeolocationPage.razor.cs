using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil23GeolocationPage
{
    private string? isSupported;
    private string? currentPosition;
    private string? watchResult;

    private ButilSubscription? watchSub;


    private async Task IsSupported()
    {
        isSupported = (await geolocation.IsSupported()).ToString();
    }

    private async Task GetCurrentPosition()
    {
        try
        {
            var position = await geolocation.GetCurrentPosition();
            var coords = position.Coords;
            currentPosition = $"lat={coords.Latitude:F6}, lng={coords.Longitude:F6}, accuracy={coords.Accuracy:F1}m";
        }
        catch (GeolocationException ex)
        {
            currentPosition = $"{ex.Code} → {ex.Message}";
        }
    }

    private async Task StartWatch()
    {
        await StopWatch();

        watchSub = await geolocation.SubscribeWatch(
            onPosition: position =>
            {
                var coords = position.Coords;
                watchResult = $"lat={coords.Latitude:F6}, lng={coords.Longitude:F6}, accuracy={coords.Accuracy:F1}m";
                InvokeAsync(StateHasChanged);
            },
            onError: ex =>
            {
                watchResult = $"{ex.Code} → {ex.Message}";
                InvokeAsync(StateHasChanged);
            });

        watchResult = "Watch started.";
    }

    private async Task StopWatch()
    {
        if (watchSub is null) return;
        await watchSub.DisposeAsync();
        watchSub = null;
        watchResult = "Watch stopped.";
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (watchSub is not null)
            await watchSub.DisposeAsync();

        await base.DisposeAsync(disposing);
    }


    private readonly string isSupportedExampleCode =
@"@inject Bit.Butil.Geolocation geolocation

<BitButton OnClick=""IsSupported"">IsSupported</BitButton>

<div>Is supported: @isSupported</div>

@code {
    private string? isSupported;

    private async Task IsSupported()
    {
        isSupported = (await geolocation.IsSupported()).ToString();
    }
}";

    private readonly string getCurrentPositionExampleCode =
@"@inject Bit.Butil.Geolocation geolocation

<BitButton OnClick=""GetCurrentPosition"">GetCurrentPosition</BitButton>

<div>Current position: @currentPosition</div>

@code {
    private string? currentPosition;

    private async Task GetCurrentPosition()
    {
        try
        {
            var position = await geolocation.GetCurrentPosition();
            var coords = position.Coords;
            currentPosition = $""lat={coords.Latitude:F6}, lng={coords.Longitude:F6}, accuracy={coords.Accuracy:F1}m"";
        }
        catch (GeolocationException ex)
        {
            currentPosition = $""{ex.Code} → {ex.Message}"";
        }
    }
}";

    private readonly string subscribeWatchExampleCode =
@"@inject Bit.Butil.Geolocation geolocation

<BitButton OnClick=""StartWatch"">Start watch</BitButton>
<BitButton OnClick=""StopWatch"">Stop watch</BitButton>

<div>Watch: @watchResult</div>

@code {
    private ButilSubscription? watchSub;

    private async Task StartWatch()
    {
        await StopWatch();

        watchSub = await geolocation.SubscribeWatch(
            onPosition: position =>
            {
                var coords = position.Coords;
                watchResult = $""lat={coords.Latitude:F6}, lng={coords.Longitude:F6}"";
                InvokeAsync(StateHasChanged);
            });
    }

    private async Task StopWatch()
    {
        if (watchSub is null) return;
        await watchSub.DisposeAsync();
        watchSub = null;
    }
}";
}
