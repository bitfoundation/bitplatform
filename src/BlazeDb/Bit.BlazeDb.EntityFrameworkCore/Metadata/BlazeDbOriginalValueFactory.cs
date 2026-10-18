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
        var original = Activator.CreateInstance(entityType.ClrType)
            ?? throw new InvalidOperationException(
                $"Entity type '{entityType.ClrType.Name}' could not be instantiated. BlazeDb needs a " +
                "parameterless constructor to reconstruct an entity's original values.");

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
