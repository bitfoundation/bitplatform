using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Bit.Butil;
using Microsoft.JSInterop;

namespace ButilTests.Manual;

/// <summary>
/// Captures the members Bit.Butil reaches by reflection at runtime, so a trimmed publish can be checked
/// against an untrimmed capture of the same thing.
/// </summary>
/// <remarks>
/// The reflection-based registration only settles who gets <b>registered</b>. Two other things inside the
/// library are resolved by name at runtime and would fail silently, in the browser, if the trimmer removed
/// them - and neither shows up as a missing service:
/// <list type="bullet">
/// <item><b>[JSInvokable] callbacks.</b> JS dispatches these by method name through a
/// <c>DotNetObjectReference</c>, including ones on internal types the consumer never names -
/// <c>DomEventsInterop</c>, the observer interops, <c>IndexedDbHandle</c>. Nothing in a consumer's code
/// references them.</item>
/// <item><b>JSON payload types.</b> The DTOs and option objects crossing the interop boundary are
/// (de)serialized by <c>System.Text.Json</c> reflecting over their constructors and properties, so a
/// trimmed-away property turns into a silently null field rather than an error.</item>
/// </list>
/// Both are meant to be covered by annotations already in the library - <c>DotNetObjectReference.Create</c>
/// preserves public methods, and the <c>Invoke&lt;T&gt;</c> overloads annotate <c>T</c> with
/// <c>LinkerFlags.JsonSerialized</c>. This class is what verifies that claim on real output rather than
/// taking it on trust.
/// </remarks>
internal sealed record TypeContract(string TypeName, bool IsCallbackTarget, bool IsPayload, int PublicConstructors, string[] JSInvokableIdentifiers, string[] PublicProperties)
{
    public string Serialize()
        => string.Join('|',
            TypeName,
            IsCallbackTarget ? "J" : "-",
            IsPayload ? "P" : "-",
            PublicConstructors.ToString(),
            string.Join(',', JSInvokableIdentifiers),
            string.Join(',', PublicProperties));

    public static TypeContract? Deserialize(string line)
    {
        var parts = line.Split('|');
        if (parts.Length != 6) return null;

        return new TypeContract(
            parts[0],
            parts[1] == "J",
            parts[2] == "P",
            int.TryParse(parts[3], out var constructors) ? constructors : 0,
            SplitList(parts[4]),
            SplitList(parts[5]));
    }

    private static string[] SplitList(string value)
        => value.Length == 0 ? [] : value.Split(',');
}
