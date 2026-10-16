# Bit.BevTools.Abstractions

The public integration contract of [bit BevTools](https://github.com/bitfoundation/bitplatform/tree/develop/src/BevTools),
the in-app developer tools for Blazor. It has no Blazor dependency, so a library can describe itself to BevTools without
referencing the tool:

- `IBevToolsExtension` / `IBevToolsExtensionBuilder` - contribute panels, state providers, diagnostic rules, commands
  and value formatters, or hide internal components.
- `IBevStateProvider` - expose a state container to the State panel, with snapshots, change history and diffs.
- `IBevDiagnosticRule` - turn symptoms into findings with evidence.
- `IBevToolsCommand` - add commands to the command palette.
- `IBevToolsTimeline` / `BevToolsEvent` - write your own events into the activity timeline.
- `[BevToolsSensitive]` / `[BevToolsIgnore]` - redact members, or hide members and whole components.

```csharp
public sealed class MyLibraryBevTools : IBevToolsExtension
{
    public string Name => "MyLibrary";
    public void Configure(IBevToolsExtensionBuilder b) => b
        .AddDiagnosticRule(new MyRule())
        .HideComponentsInNamespace("MyLibrary.Internal");
}
```

The application registers it with `services.AddBitBevToolsServices(o => o.Extensions.Add(new MyLibraryBevTools()))`.
