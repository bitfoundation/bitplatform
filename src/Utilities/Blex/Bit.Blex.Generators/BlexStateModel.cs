namespace Bit.Blex.Generators;

internal sealed record BlexStateModel(string FieldName, string PropertyName, string TypeFqn) : System.IEquatable<BlexStateModel>;
