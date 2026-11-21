namespace Bit.Bsqlite;

/// <summary>
/// Makes an AOT-compiled Blazor WebAssembly app carry the code that boxes and unboxes a nullable property of every type
/// EF Core's SQLite provider maps - without it the app stops for good, silently, on the first save of such an entity.
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
/// <see cref="RoundTrip"/> is never called. <see cref="BsqliteDbContextOptionsExtensions"/> keeps it through trimming,
/// and the AOT compiler compiles every method the trimmer kept: the casts below are what make it emit
/// <c>Nullable&lt;T&gt;.Box</c> and <c>Nullable&lt;T&gt;.Unbox</c> for each type. An app whose entities have nullable
/// properties of its own enums or structs does the same for those (docs/getting-started.md).
/// </para>
/// </remarks>
internal static class BsqliteAotNullableValues
{
    internal static object?[] RoundTrip(object? value) =>
    [
        (bool?)value,
        (byte?)value,
        (sbyte?)value,
        (short?)value,
        (ushort?)value,
        (int?)value,
        (uint?)value,
        (long?)value,
        (ulong?)value,
        (char?)value,
        (float?)value,
        (double?)value,
        (decimal?)value,
        (DateTime?)value,
        (DateTimeOffset?)value,
        (DateOnly?)value,
        (TimeOnly?)value,
        (TimeSpan?)value,
        (Guid?)value,
    ];
}
