namespace Bit.Bsqlite;

/// <summary>The host this scope's application runs as - worked out once per scope, from its services.</summary>
internal sealed record BsqliteRuntimeHost(BsqliteHostKind Kind);
