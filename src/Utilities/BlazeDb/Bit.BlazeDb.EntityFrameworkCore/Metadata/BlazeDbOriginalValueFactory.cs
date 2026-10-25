using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Update;

namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

/// <summary>
/// Rebuilds an entity as it was when the change tracker last saw it. Rows are live objects, so a
/// modified entity no longer carries the values its index entries were built from - this restores
/// them into a throwaway copy the engine can use to retract those entries.
/// </summary>
internal static class BlazeDbOriginalValueFactory
{
    public static object Create(IUpdateEntry entry)
    {
        var entityType = entry.EntityType;
        // A type built through its constructor - a positional record, or one with get-only properties -
        // is created without running any constructor: every mapped property is set below, through its
        // setter or its backing field, and nothing else of the copy is ever read.
        var original = entityType.ClrType.GetConstructor(Type.EmptyTypes) is not null
            ? Activator.CreateInstance(entityType.ClrType)!
            : RuntimeHelpers.GetUninitializedObject(entityType.ClrType);

        foreach (var property in entityType.GetProperties())
        {
            var value = entry.GetOriginalValue(property);
            switch (property.PropertyInfo, property.FieldInfo)
            {
                case ({ } info, _) when info.CanWrite:
                    info.SetValue(original, value);
                    break;
                case (_, { } field):
                    field.SetValue(original, value);
                    break;
            }
        }
        return original;
    }
}
