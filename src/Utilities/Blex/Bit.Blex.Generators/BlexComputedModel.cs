namespace Bit.Blex.Generators;

/// <summary>A <c>[BlexComputed]</c> method and what its body reads.</summary>
/// <param name="MethodName">Name of the computed method.</param>
/// <param name="PropertyName">Name of the generated memoized property.</param>
/// <param name="TypeFqn">Fully qualified type of the value.</param>
/// <param name="Reads">The state properties it reads, directly or through other computeds.</param>
/// <param name="ReadsEverything">
/// Whether it may read more than that -- through a helper method, a service field, <c>this</c> passed along -- so any
/// write invalidates it, as every write did before dependencies were tracked.
/// </param>
internal sealed record BlexComputedModel(
    string MethodName,
    string PropertyName,
    string TypeFqn,
    BlexEquatableArray<string> Reads = default,
    bool ReadsEverything = true) : System.IEquatable<BlexComputedModel>;
