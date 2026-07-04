using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil22DevicePage
{
    private string? batterySupported;
    private string? batteryStatus;
    private string? networkStatus;
    private string? wakeLockResult;
    private string? idlePermission;
    private string? idleState;

    private int idleThreshold = 60;
    private ButilSubscription? idleSub;
    private IAsyncDisposable? persistentWakeLock;


    private async Task BatterySupported()
    {
        batterySupported = (await battery.IsSupported()).ToString();
    }

    private async Task BatteryStatus()
    {
        var status = await battery.GetStatus();
        batteryStatus = $"level={status.Level:P0}, charging={status.Charging}";
    }

    private async Task NetworkStatus()
    {
        var status = await networkInformation.GetStatus();
        networkStatus = $"online={status.Online}, type={status.EffectiveType ?? status.Type ?? "n/a"}, downlink={status.Downlink}, rtt={status.Rtt}";
    }

    private async Task AcquireWakeLock()
    {
        if (await wakeLock.IsSupported() is false)
        {
            wakeLockResult = "Wake lock is not supported.";
            return;
        }

        var ok = await wakeLock.Request();
        wakeLockResult = ok ? "Wake lock acquired." : "Wake lock request failed.";
    }

    private async Task ReleaseWakeLock()
    {
        await wakeLock.Release();
        wakeLockResult = "Wake lock released.";
    }

    private async Task AcquirePersistentWakeLock()
    {
        if (await wakeLock.IsSupported() is false)
        {
            wakeLockResult = "Wake lock is not supported.";
            return;
        }

        if (persistentWakeLock is not null)
            await persistentWakeLock.DisposeAsync();

        persistentWakeLock = await wakeLock.RequestPersistent();
        wakeLockResult = "Persistent wake lock started (auto re-acquire on visibility).";
    }

    private async Task RequestIdlePermission()
    {
        if (await idleDetector.IsSupported() is false)
        {
            idlePermission = "IdleDetector is not supported.";
            return;
        }

        idlePermission = (await idleDetector.RequestPermission()).ToString();
    }

    private async Task StartIdleWatch()
    {
        await StopIdleWatch();

        if (await idleDetector.IsSupported() is false)
        {
            idleState = "IdleDetector is not supported.";
            return;
        }

        idleSub = await idleDetector.Start(idleThreshold, state =>
        {
            idleState = $"user={state.UserState}, screen={state.ScreenState}";
            InvokeAsync(StateHasChanged);
        });

        idleState = "Idle watch started.";
    }

    private async Task StopIdleWatch()
    {
        if (idleSub is null) return;
        await idleSub.DisposeAsync();
        idleSub = null;
        idleState = "Idle watch stopped.";
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (idleSub is not null)
            await idleSub.DisposeAsync();
        if (persistentWakeLock is not null)
            await persistentWakeLock.DisposeAsync();
        await wakeLock.DisposeAsync();

        await base.DisposeAsync(disposing);
    }


    private readonly string batterySupportedExampleCode =
@"@inject Bit.Butil.Battery battery

<BitButton OnClick=""BatterySupported"">IsSupported</BitButton>

<div>Is supported: @batterySupported</div>

@code {
    private string? batterySupported;

    private async Task BatterySupported()
    {
        batterySupported = (await battery.IsSupported()).ToString();
    }
}";

    private readonly string batteryStatusExampleCode =
@"@inject Bit.Butil.Battery battery

<BitButton OnClick=""BatteryStatus"">GetStatus</BitButton>

<div>Battery: @batteryStatus</div>

@code {
    private string? batteryStatus;

    private async Task BatteryStatus()
    {
        var status = await battery.GetStatus();
        batteryStatus = $""level={status.Level:P0}, charging={status.Charging}"";
    }
}";

    private readonly string networkStatusExampleCode =
@"@inject Bit.Butil.NetworkInformation networkInformation

<BitButton OnClick=""NetworkStatus"">GetStatus</BitButton>

<div>Network: @networkStatus</div>

@code {
    private string? networkStatus;

    private async Task NetworkStatus()
    {
        var status = await networkInformation.GetStatus();
        networkStatus = $""online={status.Online}, type={status.EffectiveType ?? status.Type ?? ""n/a""}, downlink={status.Downlink}, rtt={status.Rtt}"";
    }
}";

    private readonly string wakeLockExampleCode =
@"@inject Bit.Butil.WakeLock wakeLock

<BitButton OnClick=""AcquireWakeLock"">Request</BitButton>
<BitButton OnClick=""ReleaseWakeLock"">Release</BitButton>
<BitButton OnClick=""AcquirePersistentWakeLock"">RequestPersistent</BitButton>

@code {
    private IAsyncDisposable? persistentWakeLock;

    private async Task AcquireWakeLock()
    {
        if (await wakeLock.IsSupported() is false) return;
        await wakeLock.Request();
    }

    private async Task ReleaseWakeLock()
    {
        await wakeLock.Release();
    }

    private async Task AcquirePersistentWakeLock()
    {
        persistentWakeLock = await wakeLock.RequestPersistent();
    }
}";

    private readonly string idleDetectorExampleCode =
@"@inject Bit.Butil.IdleDetector idleDetector

<BitButton OnClick=""RequestIdlePermission"">RequestPermission</BitButton>
<BitButton OnClick=""StartIdleWatch"">Start</BitButton>

@code {
    private ButilSubscription? idleSub;

    private async Task RequestIdlePermission()
    {
        if (await idleDetector.IsSupported() is false) return;
        var permission = await idleDetector.RequestPermission();
    }

    private async Task StartIdleWatch()
    {
        idleSub = await idleDetector.Start(60, state =>
        {
            var current = $""user={state.UserState}, screen={state.ScreenState}"";
            InvokeAsync(StateHasChanged);
        });
    }
}";
}
