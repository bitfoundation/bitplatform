// Measures the cost BevTools adds to a real Blazor renderer: same component, same workload, with and without it.
// Numbers are meaningless unless the two runs are identical in everything else, so both use the same renderer type.
using System.Diagnostics;
using Bit.BevTools;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const int Warmup = 200;
const int Iterations = 20_000;
const int Children = 20;

Console.WriteLine($"components per render: {Children + 1}, iterations: {Iterations}\n");

// Every configuration is measured twice and the second result kept: the first pass of any of them pays for JIT
// tiering and would otherwise make whichever configuration ran first look slow.
Run(bevTools: false); Run(bevTools: true); Run(bevTools: true, disabled: true); Run(bevTools: true, noRenderEvents: true);
var baseline = Run(bevTools: false);
var idle = Run(bevTools: true, disabled: true);
var lean = Run(bevTools: true, noRenderEvents: true);
var instrumented = Run(bevTools: true);

void Report(string name, (double TotalMs, long Bytes) r, (double TotalMs, long Bytes) b)
{
    var perRender = r.TotalMs * 1000 / Iterations;
    var overhead = b.TotalMs == 0 ? 0 : (r.TotalMs - b.TotalMs) / b.TotalMs * 100;
    Console.WriteLine($"{name,-34} {r.TotalMs,9:0.0} ms  {perRender,8:0.00} µs/render  {overhead,7:+0.0;-0.0;0.0} %  alloc {r.Bytes / 1024.0 / 1024.0,7:0.0} MB");
}

Report("without BevTools", baseline, baseline);
Report("with BevTools registered, disabled", idle, baseline);
Report("recording, no per-render events", lean, baseline);
Report("recording everything", instrumented, baseline);

(double TotalMs, long Bytes) Run(bool bevTools, bool disabled = false, bool noRenderEvents = false)
{
    var services = new ServiceCollection();
    services.AddLogging();
    if (bevTools)
    {
        services.AddBitBevToolsServices(o =>
        {
            o.Enabled = !disabled;
            o.MaxEvents = 5000;
            o.RecordRenderEvents = !noRenderEvents;
        });
    }

    var provider = services.BuildServiceProvider();
    var renderer = new BenchRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
    var root = new Host();
    var id = renderer.Attach(root);
    renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(id)).GetAwaiter().GetResult();

    for (var i = 0; i < Warmup; i++)
    {
        renderer.Dispatcher.InvokeAsync(root.Bump).GetAwaiter().GetResult();
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var before = GC.GetAllocatedBytesForCurrentThread();
    var start = Stopwatch.GetTimestamp();
    for (var i = 0; i < Iterations; i++)
    {
        renderer.Dispatcher.InvokeAsync(root.Bump).GetAwaiter().GetResult();
    }

    var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
    provider.Dispose();
    return (elapsed, bytes);
}

sealed class BenchRenderer(IServiceProvider sp, ILoggerFactory lf) : Renderer(sp, lf)
{
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    protected override void HandleException(Exception e) => Console.WriteLine("EX " + e);

    protected override Task UpdateDisplayAsync(in RenderBatch batch) => Task.CompletedTask;

    public int Attach(IComponent c) => AssignRootComponentId(c);

    public Task RenderRootAsync(int i) => RenderRootComponentAsync(i);
}

sealed class Host : ComponentBase
{
    private int _counter;

    private readonly List<string> _items = Enumerable.Range(0, 20).Select(i => "item" + i).ToList();

    public void Bump()
    {
        _counter++;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        for (var i = 0; i < 20; i++)
        {
            builder.OpenComponent<Row>(1 + i);
            builder.AddComponentParameter(200 + i, nameof(Row.Text), _items[i]);
            builder.AddComponentParameter(400 + i, nameof(Row.Version), _counter);
            builder.CloseComponent();
        }

        builder.CloseElement();
    }
}

sealed class Row : ComponentBase
{
    [Parameter] public string Text { get; set; }

    [Parameter] public int Version { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddContent(1, Text);
        builder.AddContent(2, Version);
        builder.CloseElement();
    }
}
