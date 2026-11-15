using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Bit.BlazeDb.Generators;

/// <summary>
/// What the generator produces for one table type - the source text and the diagnostics - in a form the incremental
/// pipeline can compare by value.
/// <para>
/// The models the generator builds (<see cref="BlazeDbTableModel"/>) are classes that compare by reference and carry
/// symbols and type information. Handed to the pipeline as they were, no step ever compared equal, so every edit
/// re-emitted every table. This is what is handed on instead: the emitted text and diagnostics, compared by what they
/// say and where, so an edit that leaves a table's output unchanged stops here. A diagnostic keeps its source
/// location - the one thing it refers to in a tree - so the IDE can still underline the declaration at fault; a
/// location compares equal while its tree is unchanged.
/// </para>
/// </summary>
internal sealed class BlazeDbGeneratedOutput : IEquatable<BlazeDbGeneratedOutput>
{
    private BlazeDbGeneratedOutput(string? hintName, string? source, ImmutableArray<Diagnostic> diagnostics)
    {
        HintName = hintName;
        Source = source;
        Diagnostics = diagnostics;
    }

    public string? HintName { get; }

    public string? Source { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>Emits <paramref name="result"/>'s source and creates its diagnostics.</summary>
    public static BlazeDbGeneratedOutput From(BlazeDbGenerationResult result)
    {
        var diagnostics = result.Diagnostics
            .Select(d => Diagnostic.Create(d.Descriptor, d.Location, d.Args))
            .ToImmutableArray();
        return result.Model is null
            ? new BlazeDbGeneratedOutput(null, null, diagnostics)
            : new BlazeDbGeneratedOutput(result.Model.HintName, BlazeDbSourceEmitter.Emit(result.Model), diagnostics);
    }

    public bool Equals(BlazeDbGeneratedOutput? other) =>
        other is not null &&
        HintName == other.HintName &&
        Source == other.Source &&
        Diagnostics.Length == other.Diagnostics.Length &&
        Diagnostics.Zip(other.Diagnostics, SameDiagnostic).All(same => same);

    // A diagnostic is the same when it says the same thing at the same place.
    private static bool SameDiagnostic(Diagnostic a, Diagnostic b) =>
        a.Id == b.Id &&
        a.GetMessage() == b.GetMessage() &&
        a.Location.Equals(b.Location);

    public override bool Equals(object? obj) => Equals(obj as BlazeDbGeneratedOutput);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = HintName is null ? 0 : StringComparer.Ordinal.GetHashCode(HintName);
            hash = hash * 31 + (Source is null ? 0 : StringComparer.Ordinal.GetHashCode(Source));
            return hash * 31 + Diagnostics.Length;
        }
    }
}
