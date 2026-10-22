# AOT profiles of the demos

todo-aot.bitplatform.cc (Todo Sample CD) and bmage.bitplatform.dev (Bmage Demo CD) are built with profile-guided AOT: `WasmAotProfilePath` points the AOT compiler at a profile here, and only the methods listed in it are compiled to WebAssembly. Everything else runs on the interpreter. Compared with compiling every method, todo-aot downloads 41% less and becomes interactive sooner.

A method missing from a profile only runs slower, so a profile can lag behind the code. Collect it again when a demo changes a lot or .NET is updated.

## Collect a profile

1. Publish the app the way its CD does, but on the interpreter and with Mono's AOT profiler linked in:
   `-p:RunAOTCompilation=false -p:WasmProfilers=aot -p:WasmBuildNative=true`. Keep the CD's own switches (`InvariantGlobalization`, `WasmEnableSIMD`) and `-p:BitMinify=false`: Bit.Minifier's short names change from one build to the next, so a profile could never match them.
2. Use the app, then run a method that nothing has run before: the profiler writes the profile the first time that method runs. Mono matches the method by its name only, so it must be a name that nothing calls during startup.
   `collect.js` does this with Playwright (`npm i playwright-core`, `CHROMIUM_PATH` pointing at a Chromium):
   - todo-aot, against the live site with the local `_framework`:
     `node collect.js https://todo-aot.bitplatform.cc/ todo.collected.aotprofile "AppMenu::ToggleTheme" "click:.menu-chevron,/about,/sign-in,/sign-up,/terms,/privacy-policy,/not-found,/,click:.menu-chevron,role:switch|theme" <TodoSample.Client.Web>/bin/Release/net10.0/publish/wwwroot/_framework`
   - Bmage, against the published demo running locally:
     `node collect.js http://127.0.0.1:5000/ bmage.collected.aotprofile "CommandPalette::OpenFromShortcut" "/editor,/annotate,/avatar,/batch,/optimize,/social,/product,/pipelines,/formats,/metadata,/headless,/performance,/capabilities,/getting-started,/hosting,/mcp-server,/,key:/"`
3. Add what the profiler cannot see, from the trimmed assemblies in the client's `obj/Release/net10.0/linked`:
   `dotnet run genprofile.cs -- todo.aotprofile --base todo.collected.aotprofile uco:<each .dll>`
   - `uco:` adds the `[UnmanagedCallersOnly]` methods (`TimerQueue.TimerHandler` and others). Their AOT compiled native-to-managed wrappers call them directly, so without them the app dies with `RuntimeError: null function` at its first timer.
   - `all:` adds every method of an assembly. Bmage uses `all:Bit.Bmage.dll`, so every image operation stays compiled.

Delete the client's `obj/Release/net10.0/wasm` before publishing with a new profile: the AOT compiler's cache does not notice a changed profile.
