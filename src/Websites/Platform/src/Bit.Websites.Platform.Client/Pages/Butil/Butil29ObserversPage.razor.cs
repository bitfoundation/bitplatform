using Bit.Butil;
using Microsoft.AspNetCore.Components;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil29ObserversPage
{
    private ElementReference target;
    private int targetWidth = 120;

    private ButilSubscription? intersectionSub;
    private ButilSubscription? resizeSub;
    private ButilSubscription? mutationSub;
    private ButilSubscription? perfObserverSub;
    private ButilSubscription? broadcastSub;

    private string? intersectionResult;
    private string? resizeResult;
    private string? mutationResult;
    private string? perfMeasureResult;
    private string? perfObserverResult;
    private string? storageResult;
    private string? networkResult;
    private string? broadcastResult;
    private string? indexedDbResult;


    private async Task StartIntersection()
    {
        await StopIntersection();
        intersectionSub = await target.ObserveIntersection(JSRuntime, entries =>
        {
            if (entries.Length > 0)
            {
                intersectionResult = $"ratio={entries[0].IntersectionRatio:F2}, visible={entries[0].IsIntersecting}";
                InvokeAsync(StateHasChanged);
            }
        });
        intersectionResult = "Intersection observer started.";
    }

    private async Task StopIntersection()
    {
        if (intersectionSub is null) return;
        await intersectionSub.DisposeAsync();
        intersectionSub = null;
    }

    private async Task StartResize()
    {
        await StopResize();
        resizeSub = await target.ObserveResize(JSRuntime, entries =>
        {
            if (entries.Length > 0)
            {
                var rect = entries[0].ContentRect;
                resizeResult = $"{rect?.Width:F0}x{rect?.Height:F0}";
                InvokeAsync(StateHasChanged);
            }
        });
        resizeResult = "Resize observer started.";
    }

    private async Task GrowTarget()
    {
        targetWidth += 40;
        await target.SetAttribute("style",
            $"width:{targetWidth}px;height:48px;border:2px solid var(--bit-clr-pri);border-radius:6px;display:flex;align-items:center;justify-content:center");
    }

    private async Task StopResize()
    {
        if (resizeSub is null) return;
        await resizeSub.DisposeAsync();
        resizeSub = null;
    }

    private async Task StartMutation()
    {
        await StopMutation();
        mutationSub = await target.ObserveMutations(JSRuntime, records =>
        {
            if (records.Length > 0)
            {
                mutationResult = $"{records[0].Type} on {records[0].TargetId ?? "target"}";
                InvokeAsync(StateHasChanged);
            }
        }, new MutationObserverOptions { Attributes = true });
        mutationResult = "Mutation observer started.";
    }

    private async Task MutateTarget()
    {
        await target.SetAttribute("data-butil", Guid.NewGuid().ToString("N")[..8]);
    }

    private async Task StopMutation()
    {
        if (mutationSub is null) return;
        await mutationSub.DisposeAsync();
        mutationSub = null;
    }

    private async Task PerfMarkMeasure()
    {
        await performance.Mark("butil-demo-a");
        await Task.Delay(50);
        await performance.Mark("butil-demo-b");
        await performance.Measure("butil-demo-measure", "butil-demo-a", "butil-demo-b");
        var entries = await performance.GetEntries("butil-demo-measure", "measure");
        perfMeasureResult = entries.Length > 0 ? entries[0].ToString() : "(no entry)";
    }

    private async Task PerfObserver()
    {
        await StopPerfObserver();
        perfObserverSub = await performance.SubscribeObserver(["mark"], entries =>
        {
            if (entries.Length > 0)
            {
                perfObserverResult = entries[0].ToString();
                InvokeAsync(StateHasChanged);
            }
        }, buffered: false);

        await performance.Mark("butil-demo-observed");
        perfObserverResult = "PerformanceObserver subscribed - mark created.";
    }

    private async Task StopPerfObserver()
    {
        if (perfObserverSub is null) return;
        await perfObserverSub.DisposeAsync();
        perfObserverSub = null;
    }

    private async Task StorageEstimate()
    {
        var est = await storageManager.Estimate();
        storageResult = $"quota={est.Quota}, usage={est.Usage}";
    }

    private async Task NetworkStatus()
    {
        var status = await networkInformation.GetStatus();
        networkResult = $"online={status.Online}, type={status.EffectiveType ?? status.Type ?? "n/a"}, downlink={status.Downlink}, rtt={status.Rtt}";
    }

    private async Task BroadcastSubscribe()
    {
        await StopBroadcast();
        broadcastSub = await broadcastChannel.Subscribe("butil-demo-channel", msg =>
        {
            broadcastResult = $"received -> {msg}";
            InvokeAsync(StateHasChanged);
        });
        broadcastResult = "Subscribed to butil-demo-channel.";
    }

    private async Task BroadcastPost()
    {
        await broadcastChannel.Post("butil-demo-channel", new { text = "hello from this tab", at = DateTimeOffset.Now });
        broadcastResult = "Posted (same tab won't receive its own message; open a second tab).";
    }

    private async Task StopBroadcast()
    {
        if (broadcastSub is null) return;
        await broadcastSub.DisposeAsync();
        broadcastSub = null;
    }

    private async Task IndexedDbRoundTrip()
    {
        await using var db = await indexedDb.Open("butil-demo-db", 1,
        [
            new IndexedDbStoreSchema { Name = "items", KeyPath = "id" }
        ]);

        var item = new DemoIdbItem { Id = "demo-1", Value = "stored at " + DateTimeOffset.Now.ToString("HH:mm:ss") };
        await db.Put("items", item);
        var read = await db.Get<DemoIdbItem>("items", "demo-1");
        indexedDbResult = read?.Value ?? "(null)";
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (intersectionSub is not null)
            await intersectionSub.DisposeAsync();
        if (resizeSub is not null)
            await resizeSub.DisposeAsync();
        if (mutationSub is not null)
            await mutationSub.DisposeAsync();
        if (perfObserverSub is not null)
            await perfObserverSub.DisposeAsync();
        if (broadcastSub is not null)
            await broadcastSub.DisposeAsync();

        await base.DisposeAsync(disposing);
    }


    // Public so System.Text.Json can materialize it via its public parameterless
    // constructor in trimmed/published builds when db.Get<DemoIdbItem> deserializes.
    public class DemoIdbItem
    {
        public string Id { get; set; } = "";
        public string Value { get; set; } = "";
    }


    private readonly string intersectionExampleCode =
@"@inject IJSRuntime js

<div @ref=""target"">target</div>

<BitButton OnClick=""StartIntersection"">Observe</BitButton>
<BitButton OnClick=""StopIntersection"">Stop</BitButton>

<div>@intersectionResult</div>

@code {
    private ElementReference target;
    private ButilSubscription? intersectionSub;
    private string? intersectionResult;

    private async Task StartIntersection()
    {
        await StopIntersection();
        intersectionSub = await target.ObserveIntersection(js, entries =>
        {
            if (entries.Length > 0)
            {
                intersectionResult = $""ratio={entries[0].IntersectionRatio:F2}, visible={entries[0].IsIntersecting}"";
                InvokeAsync(StateHasChanged);
            }
        });
    }

    private async Task StopIntersection()
    {
        if (intersectionSub is null) return;
        await intersectionSub.DisposeAsync();
        intersectionSub = null;
    }
}";

    private readonly string resizeExampleCode =
@"@inject IJSRuntime js

<div @ref=""target"">target</div>

<BitButton OnClick=""StartResize"">Observe</BitButton>
<BitButton OnClick=""GrowTarget"">Grow target</BitButton>
<BitButton OnClick=""StopResize"">Stop</BitButton>

<div>@resizeResult</div>

@code {
    private int targetWidth = 120;
    private ElementReference target;
    private ButilSubscription? resizeSub;
    private string? resizeResult;

    private async Task StartResize()
    {
        await StopResize();
        resizeSub = await target.ObserveResize(js, entries =>
        {
            if (entries.Length > 0)
            {
                var rect = entries[0].ContentRect;
                resizeResult = $""{rect?.Width:F0}x{rect?.Height:F0}"";
                InvokeAsync(StateHasChanged);
            }
        });
    }

    private async Task GrowTarget()
    {
        targetWidth += 40;
        await target.SetAttribute(""style"", $""width:{targetWidth}px;height:48px"");
    }

    private async Task StopResize()
    {
        if (resizeSub is null) return;
        await resizeSub.DisposeAsync();
        resizeSub = null;
    }
}";

    private readonly string mutationExampleCode =
@"@inject IJSRuntime js

<div @ref=""target"">target</div>

<BitButton OnClick=""StartMutation"">Observe</BitButton>
<BitButton OnClick=""MutateTarget"">Mutate attribute</BitButton>

<div>@mutationResult</div>

@code {
    private ElementReference target;
    private ButilSubscription? mutationSub;
    private string? mutationResult;

    private async Task StartMutation()
    {
        await StopMutation();
        mutationSub = await target.ObserveMutations(js, records =>
        {
            if (records.Length > 0)
            {
                mutationResult = $""{records[0].Type} on {records[0].TargetId ?? ""target""}"";
                InvokeAsync(StateHasChanged);
            }
        }, new MutationObserverOptions { Attributes = true });
    }

    private async Task MutateTarget()
    {
        await target.SetAttribute(""data-butil"", Guid.NewGuid().ToString(""N"")[..8]);
    }

    private async Task StopMutation()
    {
        if (mutationSub is null) return;
        await mutationSub.DisposeAsync();
        mutationSub = null;
    }
}";

    private readonly string perfMeasureExampleCode =
@"@inject Bit.Butil.Performance performance

<BitButton OnClick=""PerfMarkMeasure"">Mark + Measure</BitButton>

<div>@perfMeasureResult</div>

@code {
    private string? perfMeasureResult;

    private async Task PerfMarkMeasure()
    {
        await performance.Mark(""butil-demo-a"");
        await Task.Delay(50);
        await performance.Mark(""butil-demo-b"");
        await performance.Measure(""butil-demo-measure"", ""butil-demo-a"", ""butil-demo-b"");
        var entries = await performance.GetEntries(""butil-demo-measure"", ""measure"");
        perfMeasureResult = entries.Length > 0 ? entries[0].ToString() : ""(no entry)"";
    }
}";

    private readonly string perfObserverExampleCode =
@"@inject Bit.Butil.Performance performance

<BitButton OnClick=""PerfObserver"">SubscribeObserver</BitButton>

<div>@perfObserverResult</div>

@code {
    private ButilSubscription? perfObserverSub;
    private string? perfObserverResult;

    private async Task PerfObserver()
    {
        await StopPerfObserver();
        perfObserverSub = await performance.SubscribeObserver([""mark""], entries =>
        {
            if (entries.Length > 0)
            {
                perfObserverResult = entries[0].ToString();
                InvokeAsync(StateHasChanged);
            }
        }, buffered: false);

        await performance.Mark(""butil-demo-observed"");
    }

    private async Task StopPerfObserver()
    {
        if (perfObserverSub is null) return;
        await perfObserverSub.DisposeAsync();
        perfObserverSub = null;
    }
}";

    private readonly string storageExampleCode =
@"@inject Bit.Butil.StorageManager storageManager

<BitButton OnClick=""StorageEstimate"">Estimate</BitButton>

<div>@storageResult</div>

@code {
    private string? storageResult;

    private async Task StorageEstimate()
    {
        var est = await storageManager.Estimate();
        storageResult = $""quota={est.Quota}, usage={est.Usage}"";
    }
}";

    private readonly string networkExampleCode =
@"@inject Bit.Butil.NetworkInformation networkInformation

<BitButton OnClick=""NetworkStatus"">GetStatus</BitButton>

<div>@networkResult</div>

@code {
    private string? networkResult;

    private async Task NetworkStatus()
    {
        var status = await networkInformation.GetStatus();
        networkResult = $""online={status.Online}, type={status.EffectiveType}, downlink={status.Downlink}, rtt={status.Rtt}"";
    }
}";

    private readonly string broadcastExampleCode =
@"@inject Bit.Butil.BroadcastChannel broadcastChannel

<BitButton OnClick=""BroadcastSubscribe"">Subscribe</BitButton>
<BitButton OnClick=""BroadcastPost"">Post</BitButton>

<div>@broadcastResult</div>

@code {
    private ButilSubscription? broadcastSub;
    private string? broadcastResult;

    private async Task BroadcastSubscribe()
    {
        await StopBroadcast();
        broadcastSub = await broadcastChannel.Subscribe(""butil-demo-channel"", msg =>
        {
            broadcastResult = $""received -> {msg}"";
            InvokeAsync(StateHasChanged);
        });
    }

    private async Task BroadcastPost()
    {
        await broadcastChannel.Post(""butil-demo-channel"", new { text = ""hello from this tab"", at = DateTime.Now });
    }

    private async Task StopBroadcast()
    {
        if (broadcastSub is null) return;
        await broadcastSub.DisposeAsync();
        broadcastSub = null;
    }
}";

    private readonly string indexedDbExampleCode =
@"@inject Bit.Butil.IndexedDb indexedDb

<BitButton OnClick=""IndexedDbRoundTrip"">Round-trip</BitButton>

<div>@indexedDbResult</div>

@code {
    private string? indexedDbResult;

    private class DemoIdbItem
    {
        public string Id { get; set; } = """";
        public string Value { get; set; } = """";
    }

    private async Task IndexedDbRoundTrip()
    {
        await using var db = await indexedDb.Open(""butil-demo-db"", 1,
        [
            new IndexedDbStoreSchema { Name = ""items"", KeyPath = ""id"" }
        ]);

        var item = new DemoIdbItem { Id = ""demo-1"", Value = ""stored at "" + DateTime.Now.ToString(""HH:mm:ss"") };
        await db.Put(""items"", item);
        var read = await db.Get<DemoIdbItem>(""items"", ""demo-1"");
        indexedDbResult = read?.Value ?? ""(null)"";
    }
}";
}
