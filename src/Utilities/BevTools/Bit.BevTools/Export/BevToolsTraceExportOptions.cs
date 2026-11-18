namespace Bit.BevTools;

/// <summary>What <see cref="BevToolsTraceExporter.ToChromeTrace"/> includes.</summary>
public sealed class BevToolsTraceExportOptions
{
    /// <summary>Only activity that started at or after this moment, such as the profiler baseline. Null: everything buffered.</summary>
    public DateTimeOffset? Since { get; set; }

    /// <summary>Arrows from each UI event or navigation to the render batches and requests it caused. Default true.</summary>
    public bool IncludeFlows { get; set; } = true;
}
