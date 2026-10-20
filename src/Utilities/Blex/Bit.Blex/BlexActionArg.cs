using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// A named action argument captured at dispatch time. Generated action/effect wrappers attach
/// their parameters so middleware, subscribers and DevTools can inspect the payload.
/// </summary>
/// <param name="Name">The parameter name (or property name for a standalone <c>Set X</c>).</param>
/// <param name="Value">The argument value (may be <c>null</c>).</param>
public readonly record struct BlexActionArg(string Name, object? Value);
