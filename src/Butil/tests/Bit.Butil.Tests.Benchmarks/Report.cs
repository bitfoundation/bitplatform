using System.Globalization;

namespace ButilTests.Benchmarks;

/// <summary>
/// The report the suite prints and the verdict it exits on.
/// </summary>
/// <remarks>
/// Measurements are always printed, whether or not they pass: the point of a benchmark run is the
/// numbers, and a run that printed only its failures would be useless for the thing this is mostly
/// used for - watching a figure move over time.
/// </remarks>
internal sealed class Report
{
    private readonly List<string> _failures = [];

    internal bool Failed => _failures.Count > 0;

    internal static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(new string('-', title.Length));
    }

    internal static void Line(string text) => Console.WriteLine(text);

    /// <summary>Prints a measurement and fails the run when it is over its budget.</summary>
    internal void AtMost(string name, double measured, double budget, string unit)
    {
        var ok = measured <= budget;
        Console.WriteLine($"  {(ok ? "ok  " : "FAIL")} {name,-42} {Format(measured),12} {unit} (budget {Format(budget)})");
        if (ok is false) _failures.Add($"{name}: {Format(measured)} {unit} exceeds the budget of {Format(budget)} {unit}");
    }

    /// <summary>Prints a measurement and fails the run when it is under its floor.</summary>
    internal void AtLeast(string name, double measured, double floor, string unit)
    {
        var ok = measured >= floor;
        Console.WriteLine($"  {(ok ? "ok  " : "FAIL")} {name,-42} {Format(measured),12} {unit} (at least {Format(floor)})");
        if (ok is false) _failures.Add($"{name}: {Format(measured)} {unit} is below the floor of {Format(floor)} {unit}");
    }

    /// <summary>A number worth printing that nothing is asserted about.</summary>
    internal static void Info(string name, double measured, string unit)
        => Console.WriteLine($"       {name,-42} {Format(measured),12} {unit}");

    internal void Fail(string message)
    {
        Console.WriteLine($"  FAIL {message}");
        _failures.Add(message);
    }

    internal int Conclude()
    {
        Console.WriteLine();
        if (Failed is false)
        {
            Console.WriteLine("All measurements are inside their budgets.");
            return 0;
        }

        Console.WriteLine($"{_failures.Count} measurement(s) outside budget:");
        foreach (var failure in _failures) Console.WriteLine($"  - {failure}");
        Console.WriteLine();
        Console.WriteLine("A timing failure on a loaded machine is usually the machine; re-run before believing it.");
        Console.WriteLine("A weight failure is not - the same sources minify to the same bytes everywhere.");
        return 1;
    }

    private static string Format(double value)
        => value >= 1000
            ? value.ToString("N0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
}
