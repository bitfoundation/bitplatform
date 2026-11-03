# Bit.BevTools.Blex

[bit Blex](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Blex) in
[bit BevTools](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/BevTools)' State panel: the
global state of every store, a change per action - its qualified name (`cart/AddItem`) and payload, values under
sensitive names and credential-shaped text redacted - and restoring the state after any change through Blex's own time
travel (`BlexManager.RestoreGlobalState`), the way the Redux DevTools jump to a state.

```csharp
using Bit.BevTools;

builder.Services.AddBitBlexServices();
builder.Services.AddBitBlexStore<CartStore>();
builder.Services.AddBitBevToolsServices();
builder.Services.AddBevToolsBlex();
```

State → *Blex* lists the actions newest first, each with the members it changed; *⟲ restore* on one puts every store
back as it was right after it. BevTools offers restoring only in Development or in a process that serves one user
(WebAssembly, Hybrid), as it does for any provider that implements `IBevStateRestorer`. A restore Blex makes for another
reason - the Redux DevTools, persistence, another tab - is recorded as a change too. The last hundred states are kept
for restoring.
