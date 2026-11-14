# Coding Style

Always follow the settings defined in the `.editorconfig` file located at the root of this `src` folder.

# CI Workflows

Never put E2E (browser/Playwright, hybrid, publish-gate) tests in a project's main CI workflow (`.github/workflows/ci.<project>.yml`).
The main workflow runs on pull requests and must only build, run the unit/integration tests and (where it applies) pack.
Put the E2E suites in a separate `.github/workflows/ci.<project>.e2e.yml` that is triggered by `workflow_dispatch` and `workflow_call` only,
and point the E2E project's README at that file. Follow `ci.blex.yml` / `ci.blex.e2e.yml` and `ci.bmotion.yml` / `ci.bmotion.e2e.yml`.
A workflow's name starts with CI or CD, and its file is that name in lower case with dots for spaces ("CI Blex E2E" is `ci.blex.e2e.yml`).
Every test project runs in one of these workflows, and each of them is called from the All workflow of the project's folder, where a new
one is added too: All Utilities CI / All Utilities E2E CI for `src/Utilities`, All Toolings CI for `src/Toolings`, All Other CI /
All Other E2E CI for the rest (`all.utilities.ci.yml`, `all.utilities.e2e.ci.yml`, `all.toolings.ci.yml`, `all.other.ci.yml`, `all.other.e2e.ci.yml`).
A new folder solution also goes into All Release Build's matrix (`all.release.build.yml`), which builds each one in Release in a job of its own.
When the suite drives a demo, drive the demo published the way its `cd.<project>.demo.yml` publishes it, not a build output -
only a publish trims the WebAssembly client (see `ci.bsqlite.e2e.yml`).

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
