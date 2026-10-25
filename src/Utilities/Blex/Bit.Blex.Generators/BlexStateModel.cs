namespace Bit.Blex.Generators;

/// <param name="FieldName">Name of the [BlexState] backing field.</param>
/// <param name="PropertyName">Name of the generated reactive property.</param>
/// <param name="TypeFqn">Fully qualified type of the field.</param>
/// <param name="Persist">False when the field is marked [BlexState(Persist = false)].</param>
internal sealed record BlexStateModel(string FieldName, string PropertyName, string TypeFqn, bool Persist = true) : System.IEquatable<BlexStateModel>;
