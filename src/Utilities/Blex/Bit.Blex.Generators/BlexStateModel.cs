namespace Bit.Blex.Generators;

/// <param name="FieldName">Name of the [BlexState] backing field.</param>
/// <param name="PropertyName">Name of the generated reactive property.</param>
/// <param name="TypeFqn">Fully qualified type of the field.</param>
/// <param name="Persist">False when the field is marked [BlexState(Persist = false)].</param>
/// <param name="Comparer">The store member named by [BlexState(Comparer = ...)], or null for the default equality.</param>
internal sealed record BlexStateModel(string FieldName, string PropertyName, string TypeFqn, bool Persist = true, string? Comparer = null) : System.IEquatable<BlexStateModel>;
