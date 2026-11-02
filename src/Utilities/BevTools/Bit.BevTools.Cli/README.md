# bit BevTools CLI

Checks [bit BevTools](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/BevTools) session exports
in CI. It reads export files and writes Markdown; it never connects to a running app.

```sh
dotnet tool install --global Bit.BevTools.Cli

# Fail the build on the findings of BevTools' rules in an export (warnings and errors by default).
bevtools check checkout.session.json --report "$GITHUB_STEP_SUMMARY"

# Fail the build when a component type renders more often, or clearly longer, than in the baseline recording.
bevtools compare checkout.baseline.json checkout.session.json
```

Exit codes: `0` passed, `1` failed, `2` an input could not be read (not a BevTools export, a missing section, a bad
option).

| `check` option | |
| --- | --- |
| `--fail-on warning\|error\|info\|none` | The least severe finding that fails (`warning`); `none` only lists them |
| `--ignore <rule-id>` | Lists that rule's findings without failing on them; repeatable (`--ignore render.storm`) |
| `--fail-on-errors` | Any recorded error fails, whether or not a rule reported it |
| `--report <file.md>` | Also appends the Markdown to a file |

| `compare` option | |
| --- | --- |
| `--min-render-change <n>` | Renders a type must gain to count (`1`: counts are compared exactly) |
| `--render-change-ratio <r>` | ... and by more than this share of its baseline renders (`0`) |
| `--min-time-change-ms <ms>` | Render time a type must gain to count (`5`) |
| `--time-change-ratio <r>` | ... and by more than this share of its baseline time (`0.5`), as times vary between runs |
| `--report <file.md>` | Also appends the Markdown to a file |

Most of BevTools' rules look back over a window of 10 to 60 seconds when they run; set
`o.Diagnostics.EvaluateWhileClosed = true` in the app for the run you check, so they run as it is used and not only
when the session is exported (the report's caveats say when it was not set).

The same checks run in a test without the tool: `BevSessionCheck.Check(json)` and `BevProfileComparison.Compare(baseline,
current)` in `Bit.BevTools`. For a reliable comparison, record both exports the same way: reset the profiler baseline,
perform the interaction, export.
