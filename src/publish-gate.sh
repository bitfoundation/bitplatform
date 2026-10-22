#!/usr/bin/env bash
# The publish gate of the libraries' E2E suites: publishes an app (a library's harness or demo) the way a WebAssembly
# app ships - its client trimmed; pass -p:RunAOTCompilation=true for AOT - and fails on any trim/AOT analysis warning
# raised inside the library. Each library's Tests/<...>.E2E/publish-gate.sh calls it with its app, the framework
# arguments that app needs, and what it deliberately does not gate on.
#
# The warnings come from two places, each with its own format:
#  - the trimmer (and NativeAOT's compiler), which names the member a warning is raised in right after the code:
#    "Trim analysis warning IL2026: Bit.X.Y.Z(): ..." is raised inside Bit.X, while "...: Bit.X.Tests.Y" is the app;
#  - the trim and AOT analyzers while a project compiles, which name the project at the end of the line:
#    "File.cs(1,2): warning IL3050: Using member ... [/.../Bit.X.csproj]". The library's own compile is the only place
#    an AOT warning (IL3050) shows up for a WebAssembly app: its AOT compiler runs no analysis.
# Either reports only while it runs, and a publish skips what is up to date: CI publishes from a clean checkout, and
# locally, after a Release build or an earlier run, clean the app first.
# Not gated on: the framework assemblies' own warnings, which they report once TrimmerSingleWarn is off and which are
# not the library's to fix, and the app's (the namespaces and projects under the library's name that <app> names).
#
# src/global.json picks the SDK (and so the workload band wasm-tools is installed for) only when run from inside src.
#
# Usage: publish-gate.sh <library> <app> <project> <output-folder> [extra dotnet publish arguments...]
#   <library>  the library's root namespace and project name, e.g. Bit.Bdnd
#   <app>      what under that name is the app's, as an alternation: 'Demo|Tests' for Bit.Bdnd.Demo.* and Bit.Bdnd.Tests.*
#   <project>  the app to publish
set -euo pipefail

library="$1"
app="$2"
project="$3"
output="$4"
shift 4

log="$(mktemp)"

# -tl:off: the classic console logger writes the one-line warnings matched below.
dotnet publish "$project" -c Release -o "$output" -tl:off \
    -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false "$@" 2>&1 | tee "$log"

name="${library//./\\.}"
# A project file of the library ([/.../Bit.X.csproj], [/.../Bit.X.Y.csproj::TargetFramework=net10.0]) or of the app.
library_project="[/\\]$name(\.[^]/\\]*)?\.csproj"
app_project="[/\\]$name\.($app)(\.[^]/\\]*)?\.csproj"

# The trimmer's (with or without a source location, it says "analysis" before the code) and the analyzers' (right
# after the source location).
warnings="$( (
    grep -E "(warning|error) IL[0-9]{4}: $name\." "$log" | grep -vE "IL[0-9]{4}: $name\.($app)\." || true
    grep -E "\): (warning|error) IL[0-9]{4}: .*$library_project" "$log" | grep -vE "$app_project" || true
) | sort -u)"

if [ -n "$warnings" ]; then
    echo
    echo "$library produced trim/AOT analysis warnings:"
    echo "$warnings"
    exit 1
fi

echo
echo "No trim/AOT analysis warnings originate in $library."
