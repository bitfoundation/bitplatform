using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Borm.Benchmarks;

/// <summary>
/// The real components on Blazor's own renderer, with nothing between them and the numbers: the first
/// render of a large form, and what one committed keystroke costs once it is on screen — the write the
/// input makes, the validation it triggers, the whole-form re-render that follows, and every field
/// deciding through its render signature whether it has anything new to draw.
/// </summary>
/// <remarks>
/// <see cref="HtmlRenderer"/> is Blazor's public renderer for components outside a browser. It diffs every
/// render exactly as a browser's renderer does and produces HTML only when asked, which these never do.
/// </remarks>
[MemoryDiagnoser]
public class RenderBenchmarks
{
    [Params(50, 500)]
    public int Fields { get; set; }

    private ServiceProvider _services = null!;
    private HtmlRenderer _renderer = null!;
    private BormDefinition _form = null!;
    private BormState _state = null!;
    private BormFieldContext _lastField = null!;
    private int _tick;

    [GlobalSetup]
    public void Setup()
    {
        _services = new ServiceCollection().AddBorm().BuildServiceProvider();
        _renderer = new HtmlRenderer(_services, NullLoggerFactory.Instance);
        _form = Workloads.FlatForm(Fields);
        _state = new BormState(_form, new BormDictionaryDataAccessor(Workloads.FlatData(Fields)), _services);
        Mount(_renderer, _state);

        var path = $"f{Fields - 1}";
        _lastField = new BormFieldContext(_state, _form.FindField(path)!, path);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _renderer.Dispose();
        _state.Dispose();
        _services.Dispose();
    }

    [Benchmark(Description = "First render (flat form)")]
    public void FirstRender()
    {
        using var renderer = new HtmlRenderer(_services, NullLoggerFactory.Instance);
        using var state = new BormState(_form, new BormDictionaryDataAccessor(Workloads.FlatData(Fields)), _services);
        Mount(renderer, state);
    }

    /// <summary>What an input's <c>onchange</c> does: parse, write, validate the field, re-render.</summary>
    [Benchmark(Description = "Committed keystroke (change on the last field)")]
    public void Keystroke()
        => _renderer.Dispatcher.InvokeAsync(() => _lastField.SetFromStringAsync((++_tick & 1) == 0 ? "a" : "b"))
            .GetAwaiter().GetResult();

    private static void Mount(HtmlRenderer renderer, BormState state)
        => renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<BormView>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(BormView.State)] = state })))
            .GetAwaiter().GetResult();
}
