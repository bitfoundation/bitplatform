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

    public void Fail() => ThrowFromHelper(Total);

    public int HistoryCount => _history.Count;

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
    }

    private static void ThrowFromHelper(int total)
    {
        throw new InvalidOperationException($"total is {total}");
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

public sealed class Box<T>
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
