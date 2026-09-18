using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bit.Minifier.Tests.Library;

public class Calculator
{
    private readonly List<int> _history = [];

    public string? Label { get; set; }

    public string? Note { get; set; }

    public int Total { get; private set; }

    internal int InternalCounter;

    public int Tally;

    public event Action<int>? Added;

    internal event Action<int>? Recorded;

    public async Task<int> AddAsync(int value)
    {
        await Task.Yield();
        var before = Total;
        await Task.Delay(1);
        Record(value);
        return before + value;
    }

    public IEnumerable<int> Doubles(int count)
    {
        for (int i = 0; i < count; i++) yield return Twice(i);

        static int Twice(int value) => value * 2;
    }

    public async IAsyncEnumerable<int> CountAsync(int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    public Func<int, int> Multiplier(int factor)
    {
        var offset = Total;
        return value => (value * factor) + offset;
    }

    public int SumWhere(IEnumerable<int> values, int min) => values.Where(v => v >= min).Sum(v => Shift(v));

    public bool TryParse(string? text, [NotNullWhen(true)] out int? value)
    {
        value = int.TryParse(text, out var parsed) ? parsed : null;
        return value is not null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Fail() => ThrowFromHelper(Total);

    public int HistoryCount => _history.Count;

    // EF Core finds a read-only property's backing field by its name
    private readonly int _limit = 10;

    public int Limit => _limit;

    private int _reflectedField = 2;

    public int CallByReflection()
        => (int)typeof(Calculator).GetMethod(nameof(ReflectedByName), BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(this, null)!
           + (int)typeof(Calculator).GetField(nameof(_reflectedField), BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this)!;

    private int ReflectedByName() => 40;

    internal int InternalSquare(int value)
    {
        InternalCounter++;
        return value * value;
    }

    protected virtual int Shift(int value) => value + ShiftAmount();

    private static int ShiftAmount() => 0;

    private void Record(int value)
    {
        _history.Add(value);
        Total += value;
        Tally++;
        Added?.Invoke(value);
        Recorded?.Invoke(value);
    }

    private static void ThrowFromHelper(int total)
    {
        throw new InvalidOperationException($"total is {total}");
    }

    // a friend reaches it by name, so it keeps that name for as long as the friend is not minified
    internal static class Rounding
    {
        internal static int Half(int value) => value / 2;
    }
}

/// <summary>How EF Core's compiled model reaches backing fields: by their names, in strings.</summary>
public static class NoteAccess
{
    public static string? ReadThroughAccessor(Calculator calculator) => NoteField(calculator);

    public static object? ReadThroughReflection(Calculator calculator)
        => typeof(Calculator).GetField("<Note>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(calculator);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Note>k__BackingField")]
    private static extern ref string? NoteField(Calculator calculator);
}

public record Point(int X, int Y)
{
    public double Length => Math.Sqrt((X * X) + (Y * Y));
}

// DI compares what an open generic implementation and its service ask of their type arguments
public sealed class Box<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    public T? Value { get; set; }

    public async Task<T?> GetAsync()
    {
        await Task.Yield();
        return Select(v => v);
    }

    private T? Select(Func<T?, T?> selector) => selector(Value);
}

public enum Shade
{
    Light,
    Dark,
}

internal enum Flavor
{
    Plain,
    Spicy,
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class FlavoredAttribute : Attribute
{
    public Flavor Flavor { get; set; }

    public object? Boxed { get; set; }

    public Flavor[]? Many { get; set; }

    // a named argument names the field by string
    public int Heat;
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class TypedAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;

    public Type[]? More { get; set; }
}

internal sealed class Satchel<T>
{
}

internal sealed class Lattice
{
    // a runtime attribute keeps the nested type's name while the type around it is renamed
    [Flags]
    public enum Knot
    {
        None = 0,
        Tight = 1,
    }
}

/// <summary>Attributes whose named arguments carry an internal enum: the blob names the enum type by string.</summary>
public static class Flavors
{
    [Flavored(Flavor = Flavor.Spicy, Boxed = Flavor.Plain, Many = [Flavor.Spicy, Flavor.Plain], Heat = 3)]
    public static string Describe()
    {
        var attribute = (FlavoredAttribute)typeof(Flavors).GetMethod("Describe")!.GetCustomAttributes(false).Single(a => a is FlavoredAttribute);
        return $"{attribute.Flavor} {attribute.Boxed} {string.Join("+", attribute.Many!)} {attribute.Heat}";
    }
}

/// <summary>What the dynamic binder reads: a params array, and dynamic values.</summary>
public static class Adder
{
    public static int Sum(params int[] values) => values.Sum();

    public static dynamic Twice(dynamic value) => value * 2;
}

/// <summary>A private property reached through an expression tree: the tree holds its getter by token.</summary>
public class Shapes
{
    private int Hidden => 42;

    public int ReadThroughExpression()
    {
        System.Linq.Expressions.Expression<Func<Shapes, int>> read = s => s.Hidden;
        return read.Compile()(this);
    }

    // identifiers aren't ASCII only, and a string may name several of them
    private int Περίμετρος() => 4;

    private int Εμβαδόν() => 1;

    public int ReadThroughNames() => "Περίμετρος Εμβαδόν".Split(' ')
        .Sum(name => (int)typeof(Shapes).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(this, null)!);
}

internal interface IParsable2<TSelf> where TSelf : IParsable2<TSelf>
{
    static abstract TSelf Fabricate(int value);
}

internal readonly struct Meters : IParsable2<Meters>
{
    public Meters(int value) => Value = value;

    public int Value { get; }

    public static Meters Fabricate(int value) => new(value);
}

/// <summary>Static abstract interface members, implemented implicitly by a plain static method.</summary>
public static class Mensuration
{
    public static int MakeMeters(int value) => Create<Meters>(value).Value;

    private static T Create<T>(int value) where T : IParsable2<T> => T.Fabricate(value);
}
