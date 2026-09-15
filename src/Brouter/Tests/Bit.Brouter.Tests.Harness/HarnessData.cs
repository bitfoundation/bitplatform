using System.Text.Json.Serialization;

namespace Bit.Brouter.Tests.Harness;

/// <summary>
/// What the harness loaders return: where they ran and in which scope. A page rendering data whose
/// <see cref="ScopeId"/> is not its own scope's is looking at a result carried across the
/// prerender -&gt; interactive boundary rather than one its own loader produced.
/// </summary>
public sealed record HarnessData(string ScopeId, string Platform, int Run);

/// <summary>
/// Source-generated serialization for <see cref="HarnessData"/>, plugged into
/// <c>BrouterOptions.LoaderStateTypeInfoResolver</c> - the trimming/AOT-safe configuration the
/// publish gate exercises.
/// </summary>
[JsonSerializable(typeof(HarnessData))]
internal partial class HarnessJsonContext : JsonSerializerContext
{
}
