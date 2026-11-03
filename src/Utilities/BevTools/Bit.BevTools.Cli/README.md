# bit BevTools CLI

Checks [bit BevTools](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/BevTools) session exports
in CI, and serves them to AI coding agents. It reads export files and writes reports; it never connects to a running
app.

```sh
dotnet tool install --global Bit.BevTools.Cli

# Fail the build on the findings of BevTools' rules in an export (warnings and errors by default).
bevtools check checkout.session.json --report "$GITHUB_STEP_SUMMARY"

# Fail the build when a component type renders more often, or clearly longer, than in the baseline recording.
bevtools compare checkout.baseline.json checkout.session.json

# Let a coding agent investigate exports, read-only, as a Model Context Protocol server over stdio.
bevtools mcp checkout.session.json checkout.baseline.json
```

Exit codes: `0` passed, `1` failed, `2` an input could not be read (not a BevTools export, a missing section, a bad
option). The output format never changes them.

| `check` option | |
| --- | --- |
| `--fail-on warning\|error\|info\|none` | The least severe finding that fails (`warning`); `none` only lists them |
| `--ignore <rule-id>` | Lists that rule's findings without failing on them; repeatable (`--ignore render.storm`) |
| `--fail-on-errors` | Any recorded error fails, whether or not a rule reported it |
| `--format markdown\|json\|sarif\|github` | What it prints (`markdown`); see [Formats](#formats) |
| `--output <file>` | Writes the formatted result to a file (replacing it) instead of the console |
| `--report <file.md>` | Also appends the Markdown to a file, whatever `--format` is; not the `--output` file |

| `compare` option | |
| --- | --- |
| `--min-render-change <n>` | Renders a type must gain to count (`1`: counts are compared exactly) |
| `--render-change-ratio <r>` | ... and by more than this share of its baseline renders (`0`) |
| `--min-time-change-ms <ms>` | Render time a type must gain to count (`5`) |
| `--time-change-ratio <r>` | ... and by more than this share of its baseline time (`0.5`), as times vary between runs |
| `--min-allocation-change-bytes <n>` | Bytes per render a type must allocate more (or less) to count (`1024`); only when both exports measured allocations, in the same render mode |
| `--allocation-change-ratio <r>` | ... and by more than this share of its baseline bytes per render (`0.25`) |
| `--format markdown\|json` | What it prints (`markdown`) |
| `--output <file>` | Writes the formatted result to a file (replacing it) instead of the console |
| `--report <file.md>` | Also appends the Markdown to a file |

Most of BevTools' rules look back over a window of 10 to 60 seconds when they run; set
`o.Diagnostics.EvaluateWhileClosed = true` in the app for the run you check, so they run as it is used and not only
when the session is exported (the report's caveats say when it was not set).

The same checks run in a test without the tool: `BevSessionCheck.Check(json)` and `BevProfileComparison.Compare(baseline,
current)` in `Bit.BevTools`. For a reliable comparison, record both exports the same way: reset the profiler baseline,
perform the interaction, export.

## Formats

Every format is written from the same result as the Markdown, so they cannot disagree with it or with the exit code.

- **`markdown`** (default): the summary for a CI log or a pull request comment.
- **`json`**: `check` writes `"format": "bevtools-check/1"`: `passed`, the threshold (`failOn`, `null` for `none`),
  `ignoredRules`, `failOnErrors` and `failedOnErrors`, `failingFindings`, the export's `platform`, `runtime` and
  `exportedAt`, `errors`, `errorGroups`, `requests` and `failedRequests` (`null` when the export has no such section,
  which is not the same as none), `dismissedFindings`, `findings` (each with `ruleId`, `severity`, `title`, `evidence`,
  `suggestion`, `fingerprint`, `componentInstanceId` when the rule named a component, `fails` and `dismissed`;
  dismissed ones last) and `caveats`. `compare` writes `"format": "bevtools-compare/1"`: `passed`, `regressions`, the
  totals of both recordings, the thresholds, `changes` (each type with `kind`, `reason`, instances, renders, and total,
  max and p95 render times in both) and `warnings`.
- **`sarif`** (`check` only): a [SARIF 2.1.0](https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html) log with
  one run of the `bevtools` driver, a rule per rule id, and a result per finding: `level` from its severity (`error`,
  `warning`, `note`), `message.text` its title and evidence, its fingerprint in
  `partialFingerprints["bevToolsFingerprint/v1"]`, and a `suppressions` entry of kind `external` when it was dismissed
  in the session. The caveats are the invocation's `toolExecutionNotifications`, with an `error` one when recorded
  errors failed the check (`--fail-on-errors`), which no result stands for. A finding is about a recorded session,
  not a line of source, so results have **no `locations`**: SARIF allows that, and none are made up.
- **`github`** (`check` only): GitHub Actions workflow commands, one annotation per finding (`::error`, `::warning` or
  `::notice` by severity; dismissed findings are left out), the caveats as notices, then one summary line. Text from the
  export is escaped as the Actions toolkit escapes it (`%`, `\r`, `\n`, and `:` and `,` in properties), so nothing in
  an export can end an annotation early or start a command of its own.

## GitHub Actions

```yaml
- name: Check the BevTools session
  run: |
    dotnet tool install --global Bit.BevTools.Cli
    bevtools check artifacts/checkout.session.json --format github --report "$GITHUB_STEP_SUMMARY"

- name: Keep the result as SARIF
  if: always()
  run: bevtools check artifacts/checkout.session.json --format sarif --output bevtools.sarif --fail-on none

- uses: actions/upload-artifact@v4
  if: always()
  with:
    name: bevtools
    path: bevtools.sarif
```

The first step fails the job and annotates the run; the Markdown lands in the job summary. GitHub code scanning
(`github/codeql-action/upload-sarif`) is not an option for this SARIF: code scanning needs a source location for every
result and rejects a file whose results have none ("expected at least one location"), and BevTools' findings have no
source location to give. Keep the SARIF as an artifact for SARIF viewers and tools that accept results without
locations.

## GitLab CI

```yaml
bevtools:
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - dotnet tool install --global Bit.BevTools.Cli
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - bevtools check checkout.session.json --format json --output bevtools-check.json --report bevtools-check.md
  after_script:
    - cat bevtools-check.md
  artifacts:
    when: always
    paths:
      - bevtools-check.json
      - bevtools-check.md
```

The job fails with the check and keeps both results. GitLab's code quality report (`artifacts:reports:codequality`)
has a format of its own, not SARIF, and the tool does not write it.

## For AI coding agents: `bevtools mcp`

`bevtools mcp <export.json> [<export.json>...]` is a [Model Context Protocol](https://modelcontextprotocol.io) server
over stdio that lets an agent investigate exports the way a person reads the panels. Register it with your agent as a
command, for example in Claude Code:

```sh
claude mcp add bevtools -- bevtools mcp artifacts/checkout.session.json artifacts/checkout.baseline.json
```

What it may do is narrow by construction:

- It serves the files named on its command line and nothing else: it never connects to a running app, never reads
  another file and never writes one. Every file is read when it starts: a missing one, or one that is not a BevTools
  export, ends it with exit code `2`.
- Every tool is read-only, idempotent and closed-world, and says so in its annotations, so a client can run it without
  asking first.
- Answers come from the exports as they are. BevTools redacted their values when it captured them (credential-shaped
  values, sensitive names, `[BevToolsSensitive]` members); the server adds nothing a file does not hold. Read an export
  before you hand it to an agent you do not run yourself, as you would before attaching it to an issue.
- Every answer is bounded: a limit on items (lists say `total`, `returned` and `truncated`), strings cut at 300
  characters (1000 for messages and evidence, 2000 for stack traces) and named in `truncatedFields`, and about 64 KB
  per answer (`truncatedBy` says when that stopped a list).

| Tool | Returns |
| --- | --- |
| `list_sessions` | Each loaded export: index, file, format, exportedAt, platform, render mode, runtime, value access, capture, and the entries per section |
| `get_findings` | The rules' findings, most severe first, with their evidence; by minimum severity or rule id, dismissed ones on request |
| `get_errors` | Recorded errors, most recently seen first, with component, source location and stack trace; by text |
| `get_error_groups` | Errors grouped into distinct problems |
| `get_slowest_components` | Component instances by total render time, render count or slowest render |
| `get_component` | One instance's entry by id, with its children and the findings and errors that name it |
| `get_http` | Requests, newest first; failed or slow only, by URL text |
| `get_http_endpoints` | Requests per endpoint template, with p50/p95/max |
| `get_timeline` | Timeline events in order, by kind and after an event id (100 by default, at most 500 per call) |
| `get_logs` | Log entries from an export's `logs` section, by level, category prefix and text; says so when there is none |
| `check` | `bevtools check` on an export, as JSON |
| `compare` | `bevtools compare` on two loaded exports, as JSON |

With several exports loaded, tools take `session`: the index from `list_sessions` or the file name.
