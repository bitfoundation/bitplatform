namespace Bit.Bsqlite;

/// <summary>
/// Makes an AOT-compiled app carry the code that boxes and unboxes a nullable property of every type EF Core's SQLite
/// provider maps - without it a Blazor WebAssembly app stops for good, silently, on the first save of such an entity.
/// </summary>
/// <remarks>
/// <para>
/// EF Core keeps an entity's original values in a <c>Snapshot&lt;T0, ..., Tn&gt;</c> it instantiates at run time, over
/// the entity's property types, and reads them back boxed. AOT cannot compile that instantiation ahead of time, so it
/// runs through Mono's shared code for value types, and boxing a <c>DateOnly?</c> there calls
/// <c>Nullable&lt;DateOnly&gt;.Box</c> - which the AOT compiler emits only for the nullable types some compiled code
/// boxes itself. For any other, the shared code calls a null function pointer: the WebAssembly module traps, no .NET
/// exception is raised, nothing is logged, and no .NET code runs again in the tab.
/// </para>
/// <para>
/// The library never calls <see cref="RoundTrip"/>. <see cref="BsqliteDbContextOptionsExtensions"/> keeps it through
/// trimming, and the AOT compiler compiles every method the trimmer kept: its casts are what make it emit
/// <c>Nullable&lt;T&gt;.Box</c> and <c>Nullable&lt;T&gt;.Unbox</c> for each type. An app whose entities have nullable
/// properties of its own enums or structs does the same for those (docs/getting-started.md, "AOT and nullable
/// properties").
/// </para>
/// </remarks>
internal static class BsqliteAotNullableValues
{
    /// <summary>
    /// Unboxes <paramref name="value"/> to <paramref name="type"/>'s nullable form and boxes it back. One arm per type,
    /// so a test can tell which types are covered.
    /// </summary>
    internal static object? RoundTrip(Type type, object? value)
    {
        if (type == typeof(bool)) return (bool?)value;
        if (type == typeof(byte)) return (byte?)value;
        if (type == typeof(sbyte)) return (sbyte?)value;
        if (type == typeof(short)) return (short?)value;
        if (type == typeof(ushort)) return (ushort?)value;
        if (type == typeof(int)) return (int?)value;
        if (type == typeof(uint)) return (uint?)value;
        if (type == typeof(long)) return (long?)value;
        if (type == typeof(ulong)) return (ulong?)value;
        if (type == typeof(char)) return (char?)value;
        if (type == typeof(float)) return (float?)value;
        if (type == typeof(double)) return (double?)value;
        if (type == typeof(decimal)) return (decimal?)value;
        if (type == typeof(DateTime)) return (DateTime?)value;
        if (type == typeof(DateTimeOffset)) return (DateTimeOffset?)value;
        if (type == typeof(DateOnly)) return (DateOnly?)value;
        if (type == typeof(TimeOnly)) return (TimeOnly?)value;
        if (type == typeof(TimeSpan)) return (TimeSpan?)value;
        if (type == typeof(Guid)) return (Guid?)value;

        throw new ArgumentOutOfRangeException(nameof(type), type, "Not a type the SQLite provider maps.");
    }
}
