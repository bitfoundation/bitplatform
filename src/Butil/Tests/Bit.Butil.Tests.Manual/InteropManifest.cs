using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Bit.Butil;
using Microsoft.JSInterop;

namespace ButilTests.Manual;

/// <summary>
/// What an untrimmed run records for a trimmed run to check itself against: the interop contract, plus the
/// roster of <c>[ButilService]</c> names so the trimmed run can tell a genuinely trimmed-away service from a
/// name that no longer refers to anything.
/// </summary>
internal sealed record InteropManifest(string[] ServiceNames, TypeContract[] Types);
