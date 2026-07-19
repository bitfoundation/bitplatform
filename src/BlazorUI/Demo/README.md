# Bit.BlazorUI.Demo

https://blazorui.bitplatform.dev/

## Debugging Blazor Server vs. WebAssembly

Debug builds render in Blazor Server mode; the WASM client is excluded by default
(see `IncludeWasm` in the csproj). To run/debug the Blazor WebAssembly client,
build with the WASM client included, e.g.:

    dotnet run --project Bit.BlazorUI.Demo.Server -p:IncludeWasm=true