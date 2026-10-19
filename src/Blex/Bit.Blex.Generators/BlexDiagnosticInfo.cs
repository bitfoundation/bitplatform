namespace Bit.Blex.Generators;

/// <summary>An equatable, cache-friendly stand-in for a <see cref="Microsoft.CodeAnalysis.Diagnostic"/>.</summary>
internal sealed record BlexDiagnosticInfo(
    Microsoft.CodeAnalysis.DiagnosticDescriptor Descriptor,
    BlexLocationInfo? Location,
    BlexEquatableArray<string> Args) : System.IEquatable<BlexDiagnosticInfo>
{
    public Microsoft.CodeAnalysis.Diagnostic ToDiagnostic()
        => Microsoft.CodeAnalysis.Diagnostic.Create(
            Descriptor,
            Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None,
            // ReSharper disable once CoVariantArrayConversion
            System.Linq.Enumerable.ToArray(Args));
}
