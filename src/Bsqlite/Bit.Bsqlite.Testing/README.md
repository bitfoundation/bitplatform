# Bit.Bsqlite.Testing

Test support for [bit Bsqlite](https://www.nuget.org/packages/Bit.Bsqlite). Two things live here.

```
dotnet add package Bit.Bsqlite.Testing
```

## 1. Run the stack without a browser

`BsqliteInProcessTransport` is an `IBsqliteTransport` backed by Microsoft.Data.Sqlite that
stands in for the web-worker transport, so EF Core mappings, migrations, generated SQL, and live
queries can be tested on desktop .NET.

```csharp
await using var transport = new BsqliteInProcessTransport();
await using var connection = new BsqliteConnection(transport, "test.db");
await connection.OpenAsync();
options.UseBitBsqlite(connection);

// after the act
Assert.Contains("CREATE TABLE", transport.ExecutedCommands[0]);
```

It mirrors the worker's responsibilities rather than approximating them. It installs the EF function
set, which matters more than it sounds: EF's SQLite provider registers its scalar functions,
aggregates, and collation only when the connection is literally a `SqliteConnection`; against any
other `DbConnection` it logs a warning and moves on. Bsqlite supplies its own connection, so it
inherits that obligation, and `BsqliteFunctions` is the reference implementation the
worker-side UDF host matches. Pass `registerEfFunctions: false` to see what breaks without it. And
it reports writes the way the worker does - from SQLite's update hook, cascades and triggers
included, once the transaction that made them commits - so live queries behave on desktop
exactly as they do in the browser.

## 2. The storage conformance kit

If you are writing a storage backend, this is how you prove it works. Inherit the suites in your own
test project and hand them your provider:

```csharp
[TestClass]
public sealed class MyBackendConformanceTests : BsqliteStorageProviderConformanceTests
{
    protected override IBsqliteStorageProvider CreateProvider() => new MyStorageProvider();
}
```

The core trusts `IBsqliteStorageProvider.Capabilities` without verifying it — selection, pragma
guarding, and durability options are all driven by what a backend *claims*. The kit is what makes
that trust reasonable.

- `BsqliteStorageProviderConformanceTests` — everything checkable without a running engine: are
  the declared capabilities internally coherent, does the admin surface behave the way
  cross-provider migration assumes.
- `BsqliteStorageEngineConformanceTests` — the claims only a real database can settle: write
  atomicity, crash safety, concurrency levels. Needs the worker host.

Providers that cannot run in the current environment report the test inconclusive with an explanation
rather than failing, so the same suite is meaningful on desktop and in a browser.

Built on MSTest: the derived classes need `[TestClass]`, and the kit's test methods come with the base
class. MIT. Part of [bit Bsqlite](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite).
