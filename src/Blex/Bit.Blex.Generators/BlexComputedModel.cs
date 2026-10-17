namespace Bit.Blex.Generators;

internal sealed record BlexComputedModel(string MethodName, string PropertyName, string TypeFqn) : System.IEquatable<BlexComputedModel>;
