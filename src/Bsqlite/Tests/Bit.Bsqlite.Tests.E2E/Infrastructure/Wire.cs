using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>What the worker reports about itself: the SQLite version and the engine build it loaded.</summary>
public sealed record HostVersion(string Version, string Build);

/// <summary>
/// The options of the host module's <c>open</c>, as the .NET transport sends them. Unset members are left
/// out of what the page receives rather than sent as null, the same as a JavaScript caller leaving them out.
/// </summary>
public sealed record HostOptions
{
    public string DatabaseName { get; init; } = "test.db";

    /// <summary><c>synchronous</c> or <c>asyncCapable</c>.</summary>
    public string RequiredBuild { get; init; } = "synchronous";

    /// <summary>The storage provider's VFS module, e.g. <see cref="Storages.OpfsVfs"/>.</summary>
    public JsModule? Vfs { get; init; }

    public StorageLimits? Limits { get; init; }

    public bool? ForeignKeys { get; init; }

    public string[]? OnOpenSql { get; init; }

    public JsModule[]? Extensions { get; init; }

    internal Dictionary<string, object?> ToArgument()
    {
        var argument = new Dictionary<string, object?>
        {
            ["databaseName"] = DatabaseName,
            ["requiredBuild"] = RequiredBuild,
        };

        if (Vfs is not null) argument["vfs"] = Vfs.ToArgument();
        if (Limits is not null) argument["limits"] = Limits.ToArgument();
        if (ForeignKeys is not null) argument["foreignKeys"] = ForeignKeys;
        if (OnOpenSql is not null) argument["onOpenSql"] = OnOpenSql;
        if (Extensions is not null) argument["extensions"] = Extensions.Select(e => e.ToArgument()).ToArray();
        return argument;
    }
}

/// <summary>A module the worker imports: a storage provider's VFS, or an extension registering functions.</summary>
public sealed record JsModule(string ModuleUrl, string RegisterExport = "register")
{
    internal Dictionary<string, object?> ToArgument() => new() { ["moduleUrl"] = ModuleUrl, ["registerExport"] = RegisterExport };
}

/// <summary>What a storage provider tells the worker it cannot do, so the worker can refuse it in SQL.</summary>
public sealed record StorageLimits(bool SupportsMultiDatabaseTransactions, bool CanChangePageSize)
{
    internal Dictionary<string, object?> ToArgument() => new()
    {
        ["supportsMultiDatabaseTransactions"] = SupportsMultiDatabaseTransactions,
        ["canChangePageSize"] = CanChangePageSize,
    };
}

/// <summary>The first-party storages, as each .NET provider describes itself to the worker.</summary>
public static class Storages
{
    public static readonly JsModule OpfsVfs = new("/_content/Bit.Bsqlite.Storage.Opfs/bit-bsqlite-opfs-vfs.js");
    public static readonly StorageLimits OpfsLimits = new(SupportsMultiDatabaseTransactions: false, CanChangePageSize: true);

    public static readonly JsModule IndexedDbVfs = new("/_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-vfs.js");
    public static readonly StorageLimits IndexedDbLimits = new(SupportsMultiDatabaseTransactions: true, CanChangePageSize: false);

    public static readonly JsModule CacheStorageVfs = new("/_content/Bit.Bsqlite.Storage.CacheStorage/bit-bsqlite-cache-register.js");
    public static readonly StorageLimits CacheStorageLimits = new(SupportsMultiDatabaseTransactions: true, CanChangePageSize: false);

    public static HostOptions Opfs(string databaseName) => new()
    {
        DatabaseName = databaseName,
        RequiredBuild = "synchronous",
        Vfs = OpfsVfs,
        Limits = OpfsLimits,
    };

    public static HostOptions IndexedDb(string databaseName) => new()
    {
        DatabaseName = databaseName,
        RequiredBuild = "asyncCapable",
        Vfs = IndexedDbVfs,
        Limits = IndexedDbLimits,
    };

    public static HostOptions CacheStorage(string databaseName) => new()
    {
        DatabaseName = databaseName,
        RequiredBuild = "asyncCapable",
        Vfs = CacheStorageVfs,
        Limits = CacheStorageLimits,
    };
}

/// <summary>One request of a batch. <see cref="ResultKind"/> is <c>nonQuery</c>, <c>scalar</c> or <c>reader</c>.</summary>
public sealed record WireRequest(string CommandText, IReadOnlyList<WireParameter>? Parameters = null, string ResultKind = "nonQuery", int? MaxRows = null)
{
    internal Dictionary<string, object?> ToArgument()
    {
        var argument = new Dictionary<string, object?>
        {
            ["commandText"] = CommandText,
            ["resultKind"] = ResultKind,
            ["parameters"] = (Parameters ?? []).Select(p => p.ToArgument()).ToArray(),
        };

        if (MaxRows is not null) argument["maxRows"] = MaxRows;
        return argument;
    }
}

/// <summary>
/// A parameter with a natural value. Two kinds cannot cross into the page as themselves and are written as
/// <see cref="Blob"/> for bytes and <see cref="BigInt"/> for a 64-bit integer.
/// </summary>
public sealed record WireParameter(string Name, object? Value = null)
{
    public byte[]? Bytes { get; init; }

    public string? Big { get; init; }

    public static WireParameter Blob(string name, params byte[] bytes) => new(name) { Bytes = bytes };

    public static WireParameter BigInt(string name, long value) => new(name) { Big = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };

    public static WireParameter BigInt(string name, string value) => new(name) { Big = value };

    internal Dictionary<string, object?> ToArgument()
    {
        var argument = new Dictionary<string, object?> { ["name"] = Name, ["value"] = Value };
        if (Bytes is not null) argument["bytes"] = Bytes.Select(b => (int)b).ToArray();
        if (Big is not null) argument["big"] = Big;
        return argument;
    }
}

/// <summary>One result set of a reply, with the rows kept as JSON for <see cref="AssertJson"/>.</summary>
public sealed record WireResult(string[] ColumnNames, string?[] ColumnTypes, int RecordsAffected, long LastInsertRowId, JsonElement Rows)
{
    internal static WireResult From(JsonElement result) => new(
        [.. result.GetProperty("columnNames").EnumerateArray().Select(n => n.GetString()!)],
        [.. result.GetProperty("columnTypes").EnumerateArray().Select(t => t.ValueKind == JsonValueKind.Null ? null : t.GetString())],
        result.GetProperty("recordsAffected").GetInt32(),
        result.GetProperty("lastInsertRowId").GetInt64(),
        result.GetProperty("rows").Clone());
}

/// <summary>A failed batch, as the reply carried it.</summary>
public sealed record WireFailure(string Message, int? SqliteCode, string Name)
{
    internal static WireFailure? From(JsonElement failure) => failure.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
        ? null
        : new WireFailure(
            failure.GetProperty("message").GetString()!,
            failure.GetProperty("sqliteCode").ValueKind == JsonValueKind.Number ? failure.GetProperty("sqliteCode").GetInt32() : null,
            failure.GetProperty("name").GetString()!);
}

/// <summary>
/// Structural equality of a value that came out of the page and the JSON a test expects - the counterpart
/// of Playwright Test's <c>toEqual</c>. Numbers compare by value, so <c>42</c> and <c>42.0</c> are equal.
/// </summary>
public static class AssertJson
{
    public static void AreEqual(string expectedJson, JsonElement actual, string? message = null)
    {
        var expected = JsonNode.Parse(expectedJson);
        var actualNode = WithoutObjectIds(JsonNode.Parse(actual.GetRawText()));

        if (JsonNode.DeepEquals(expected, actualNode) is false)
        {
            Assert.Fail($"{(message is null ? "" : message + " ")}Expected {expected?.ToJsonString() ?? "null"} but was {actualNode?.ToJsonString() ?? "null"}.");
        }
    }

    /// <summary>
    /// Removes the <c>$id</c> member Playwright gives every object it returns as a <see cref="JsonElement"/>. Its
    /// protocol numbers the objects of a result so that shared references and cycles survive the trip; that
    /// numbering is transport metadata rather than part of the value, and <c>toEqual</c> never sees it.
    /// </summary>
    private static JsonNode? WithoutObjectIds(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                obj.Remove("$id");
                foreach (var (_, child) in obj) WithoutObjectIds(child);
                break;
            case JsonArray array:
                foreach (var child in array) WithoutObjectIds(child);
                break;
        }

        return node;
    }
}
