using Bit.Minifier.Tests.Library;

namespace Bit.Minifier.Tests.Friend;

/// <summary>
/// Everything the tests check, called the way an app calls it: once public names are minified too, the tests
/// can't reach the fixtures by name, only this class, which <see cref="Entries"/> names the way an app names
/// what it reaches by reflection.
/// </summary>
public static class Driver
{
    public static IReadOnlyList<string> Entries { get; } = ["Driver", "RunAsync", "FailureStackTrace"];

    public static async Task<List<string>> RunAsync()
    {
        var results = new List<string>();
        var calculator = new FriendCalculator();
        calculator.Added += value => results.Add($"added {value}");
        results.Add($"add {await calculator.AddAsync(5)} {await calculator.AddAsync(2)}");
        results.Add($"history {calculator.HistoryCount} total {calculator.Total} tally {calculator.Tally}");
        results.Add($"doubles {string.Join(",", calculator.Doubles(3))}");
        var counted = new List<int>();
        await foreach (var value in calculator.CountAsync(3)) counted.Add(value);
        results.Add($"count {string.Join(",", counted)}");
        results.Add($"multiplier {calculator.Multiplier(3)(2)}");
        results.Add($"sum {calculator.SumWhere([1, 2, 3], 2)}");
        results.Add($"parse {calculator.TryParse("7", out var parsed)} {parsed}");
        calculator.Label = "L";
        results.Add($"label {calculator.Label}");
        results.Add($"reflection {calculator.CallByReflection()}");
        calculator.Note = "noted";
        results.Add($"note {NoteAccess.ReadThroughAccessor(calculator)} {NoteAccess.ReadThroughReflection(calculator)}");

        var point = new Point(3, 4);
        results.Add($"point {point} {point.Length} {point.Equals(new Point(3, 4))}");
        // what a reflection-based serializer sees
        results.Add($"properties {string.Join(",", point.GetType().GetProperties().Select(p => $"{p.Name}={p.GetValue(point)}"))}");

        results.Add($"box {await new Box<string> { Value = "boxed" }.GetAsync()}");
        results.Add($"shade {Enum.Parse<Shade>("Dark")}");
        results.Add($"flavors {Flavors.Describe()} {DescribeFlavor()} {DescribeTypes()}");
        results.Add($"expression {new Shapes().ReadThroughExpression()}");
        results.Add($"generic math {Generic.MakeMeters(7)}");
        results.Add($"friend square {calculator.SquareThroughInternals(3)}");
        results.Add($"friend add {await calculator.AddTwiceAsync(2)}");

        var plugin = Type.GetType("Bit.Minifier.Tests.Library.Plugin, Bit.Minifier.Tests.Library", throwOnError: true)!;
        results.Add($"plugin {plugin.GetMethod("Hello")!.Invoke(Activator.CreateInstance(plugin), ["hi"])}");
        results.Add($"widget {new Widget { Colors = new Palette { Name = "dark" } }.Colors!.Name}");
        results.Add($"crate {new Bit.Minifier.Tests.Shelf.Crate().Count(2, 3)}");
        results.Add($"startup {await Startup.Main(["a"])}");
        return results;
    }

    // an attribute naming an enum of another assembly: the blob spells out the enum's full name
    [Flavored(Flavor = Flavor.Spicy, Boxed = Flavor.Plain, Many = [Flavor.Plain])]
    private static string DescribeFlavor()
    {
        var attribute = (FlavoredAttribute)typeof(Driver).GetMethod(nameof(DescribeFlavor), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetCustomAttributes(false).Single(a => a is FlavoredAttribute);
        return $"{attribute.Flavor} {attribute.Boxed} {string.Join("+", attribute.Many!)}";
    }

    // Type values of another assembly's types, inside generic arguments and arrays, and a nested enum
    [Typed(typeof(Satchel<Flavor>), More = [typeof(Satchel<Lattice.Knot>[]), typeof(Lattice.Knot)])]
    [Flavored(Boxed = Lattice.Knot.Tight)]
    private static string DescribeTypes()
    {
        var method = typeof(Driver).GetMethod(nameof(DescribeTypes), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var typed = (TypedAttribute)method.GetCustomAttributes(false).Single(a => a is TypedAttribute);
        var flavored = (FlavoredAttribute)method.GetCustomAttributes(false).Single(a => a is FlavoredAttribute);
        return string.Join(" ",
            typed.Type == typeof(Satchel<Flavor>),
            typed.More![0] == typeof(Satchel<Lattice.Knot>[]),
            typed.More[1] == typeof(Lattice.Knot),
            flavored.Boxed is Lattice.Knot.Tight);
    }

    public static string FailureStackTrace()
    {
        try
        {
            new Calculator().Fail();
            return "";
        }
        catch (InvalidOperationException e)
        {
            return e.StackTrace!;
        }
    }
}
