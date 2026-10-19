using Microsoft.CodeAnalysis;

namespace Bit.BlazeDb.Generators;

internal static class BlazeDbDiagnostics
{
    private const string Category = "Bit.BlazeDb.Generators";

    public static readonly DiagnosticDescriptor NotPartial = new(
        "BLZ001",
        "Table type must be partial",
        "Type '{0}' is marked [BlazeDbTable] and must be declared partial",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingOrMultipleKey = new(
        "BLZ002",
        "Table type must have exactly one [BlazeDbKey] property",
        "Type '{0}' must have exactly one serializable property marked [BlazeDbKey]",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        "BLZ003",
        "Unsupported property type",
        "Property '{0}.{1}' has a type BlazeDb cannot serialize ({2}); mark it [BlazeDbIgnore] or change the type",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
        "BLZ004",
        "Table type needs a parameterless constructor",
        "Type '{0}' needs a parameterless constructor so rows can be deserialized (positional-only records are not supported)",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateFieldNumber = new(
        "BLZ005",
        "Duplicate field number",
        "Type '{0}' assigns field number {1} to more than one property",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedKeyType = new(
        "BLZ006",
        "Unsupported key type",
        "Property '{0}.{1}' is marked [BlazeDbKey] but its type is not a supported non-nullable scalar",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CompoundIndexPropertyNotFound = new(
        "BLZ008",
        "Compound index references an unknown property",
        "Compound index '{0}' on type '{1}' references '{2}', which is not a serializable property of that type",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CompoundIndexNeedsTwoProperties = new(
        "BLZ009",
        "Compound index needs at least two properties",
        "Compound index '{0}' on type '{1}' must list at least two properties; use [BlazeDbIndex] or [BlazeDbOrderedIndex] for a single one",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateIndexName = new(
        "BLZ010",
        "Duplicate index name",
        "Type '{0}' declares more than one index named '{1}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InheritedProperties = new(
        "BLZ012",
        "Table type inherits properties",
        "Type '{0}' inherits property '{2}' from '{1}'; BlazeDb serializes only a table type's own properties, so an inherited one would silently never be persisted. Declare it on '{0}' or mark it [BlazeDbIgnore].",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidIndexName = new(
        "BLZ011",
        "Index name must be a valid identifier",
        "Index name '{1}' on type '{0}' cannot be a member of the generated Indexes class; it must be a valid C# identifier other than 'Indexes'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MixedFieldNumbering = new(
        "BLZ013",
        "Table type mixes explicit and automatic field numbers",
        "Type '{0}' gives some properties a [BlazeDbField] number and leaves '{1}' (and {2} more) to be numbered by declaration order. An automatic number moves whenever a property is added, removed or given an explicit number that it was using, and rows already on disk are then read into the wrong properties without an error. Number every property once the type persists data.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidFieldNumber = new(
        "BLZ014",
        "Field number out of range",
        "Property '{0}.{1}' has field number {2}; [BlazeDbField] numbers must be between 1 and {3}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnindexableProperty = new(
        "BLZ015",
        "Property cannot be indexed",
        "Property '{0}.{1}' cannot be indexed: its type ({2}) has no value equality or ordering to key an index on. Collections and byte[] can be stored but not indexed.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CompoundIndexDuplicateProperty = new(
        "BLZ016",
        "Compound index lists a property twice",
        "Compound index '{0}' on type '{1}' lists property '{2}' more than once",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedTypeShape = new(
        "BLZ007",
        "Unsupported table type shape",
        "Type '{0}' cannot be a table: generic or nested types are not supported",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
