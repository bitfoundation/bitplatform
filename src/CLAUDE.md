# Coding Style

Always follow the settings defined in the `.editorconfig` file located at the root of this `src` folder.

# CI Workflows

Never put E2E (browser/Playwright, hybrid, publish-gate) tests in a project's main CI workflow (`.github/workflows/bit.ci.<Project>.yml`).
The main workflow runs on pull requests and must only build, run the unit/integration tests and (where it applies) pack.
Put the E2E suites in a separate `.github/workflows/bit.ci.<Project>.e2e.yml` that is triggered by `workflow_dispatch` only,
and point the E2E project's README at that file. Follow `bit.ci.Blex.yml` / `bit.ci.Blex.e2e.yml` and `bit.ci.Bmotion.yml` / `bit.ci.Bmotion.e2e.yml`.
When the suite drives a demo, drive the demo published the way its `<project>.demo.cd.yml` publishes it, not a build output -
only a publish trims the WebAssembly client (see `bit.ci.Bsqlite.e2e.yml`).

# JS interop payloads

A library here is trimmed in a published app unless it opts out (`Bit.Build.props` sets `IsAotCompatible`; BevTools and Borm
set it back), and System.Text.Json reads what crosses JS interop by reflection the trim analyzers cannot see through
`params object?[]` - so nothing warns, and it fails only once published:

- Never pass an anonymous type: trimming strips its constructor's parameter names and System.Text.Json refuses to serialize it.
  Send a `JsonObject` or a named type.
- Give a named type `[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(T))]` at the call site (or the `[JSInvokable]`
  method), and the same for an array's element type and every type nested in its properties: `InvokeAsync<T>` preserves `T`
  alone. Without it the getters only System.Text.Json reads, and the constructors and setters only it calls, are trimmed away -
  the payload arrives as `{}` or the result fails to deserialize.
