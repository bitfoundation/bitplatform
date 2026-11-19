namespace Bit.Blex;

/// <summary>The equatable output of the transform stage: an optional model plus diagnostics.</summary>
internal sealed record BlexTransformOutput(
    BlexStoreModel? Model,
    BlexEquatableArray<BlexDiagnosticInfo> Diagnostics) : System.IEquatable<BlexTransformOutput>;
