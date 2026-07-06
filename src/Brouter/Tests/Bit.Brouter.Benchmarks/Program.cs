using System.Globalization;
using Bit.Brouter.Benchmarks;

// Route counts to sweep. Override on the command line, e.g.:
//   dotnet run -c Release -- 100 250 500 1000
// The review specifically calls out benchmarking "at 200-500 routes", which the default sweep covers.
int[] routeCounts = args.Length > 0
    ? args.Select(a => int.Parse(a, CultureInfo.InvariantCulture)).ToArray()
    : [50, 100, 200, 500, 1000];

const int warmup = 2;
const int trials = 7;

Console.WriteLine();
Console.WriteLine("Bit.Brouter route-scalability benchmark");
Console.WriteLine("=======================================");
Console.WriteLine("Scenario A (Brouter)    : every route is a live <Broute> component instance.");
Console.WriteLine("Scenario B (RouteTable) : routes as data; only the matched component is instantiated");
Console.WriteLine("                          (models the built-in Blazor Router).");
Console.WriteLine($"warmup={warmup}  trials={trials} (median reported)  |  build MUST be Release for meaningful numbers");
#if DEBUG
Console.WriteLine();
Console.WriteLine("  !! WARNING: running a DEBUG build. Re-run with `dotnet run -c Release` for real numbers. !!");
#endif
Console.WriteLine();

// Header
Console.WriteLine(
    "{0,7} | {1,-28} | {2,-28} | {3}",
    "routes", "Brouter (A)", "RouteTable (B)", "retained delta");
Console.WriteLine(
    "{0,7} | {1,-28} | {2,-28} | {3}",
    "", "render / alloc / retained", "render / alloc / retained", "A - B  (~per route)");
Console.WriteLine(new string('-', 108));

foreach (var n in routeCounts)
{
    var a = Harness.MeasureBrouter(n, warmup, trials);
    var b = Harness.MeasureRouteTable(n, warmup, trials);

    var retainedDelta = a.RetainedKB - b.RetainedKB;
    var perRouteBytes = n > 0 ? retainedDelta * 1024.0 / n : 0;

    Console.WriteLine(
        "{0,7} | {1,-28} | {2,-28} | {3,8:0.0} KB (~{4:0} B/route)",
        n,
        Fmt(a),
        Fmt(b),
        retainedDelta,
        perRouteBytes);
}

Console.WriteLine();
Console.WriteLine("Reading the results:");
Console.WriteLine("  - 'render'   : median wall-clock to instantiate + do the first match (ms).");
Console.WriteLine("  - 'alloc'    : bytes allocated during that render (MB).");
Console.WriteLine("  - 'retained' : managed heap still held after render, tree kept alive (KB).");
Console.WriteLine("  - The A-B retained delta is the steady-state cost of keeping every route as a live");
Console.WriteLine("    component. Divided by route count it estimates the per-route memory overhead.");
Console.WriteLine("  - Absolute values include a fixed bUnit renderer/host overhead present in both");
Console.WriteLine("    columns; compare the two columns and how the delta scales with route count.");
Console.WriteLine();

static string Fmt(Sample s) => $"{s.RenderMs,6:0.0}ms {s.AllocMB,5:0.0}MB {s.RetainedKB,7:0}KB";
