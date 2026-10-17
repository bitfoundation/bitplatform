#!/usr/bin/env bash
# Publishes the web harness host the way a WebAssembly app ships (trimmed; pass
# -p:RunAOTCompilation=true for AOT) and fails when the trimmer or the AOT compiler reports an analysis
# warning raised inside one of the Bit.Blex libraries.
#
# Two kinds of warning are deliberately not gated on:
#  - the framework assemblies' own (Microsoft.AspNetCore.Components, Microsoft.JSInterop, System.Text.Json),
#    which they report once TrimmerSingleWarn is off and which are not this library's to fix;
#  - warnings raised in application code (the harness), above all the IL2026/IL3050 the generated store
#    snapshot code raises for the reflection-based JSON serializer: it runs against the app's own state
#    types, which only the app can root or give a source-generated resolver (BlexJson.Configure).
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

output="$1"
shift

here="$(cd "$(dirname "$0")" && pwd)"
project="$here/../Bit.Blex.Tests.Harness.Web/Bit.Blex.Tests.Harness.Web.csproj"
log="$(mktemp)"

# TargetFrameworks=net10.0 on top of -f: -f alone does not reach project references, which then still
# evaluate every framework they target - and with RunAOTCompilation each of those demands its own
# wasm-tools workload.
dotnet publish "$project" -c Release -f net10.0 -p:TargetFrameworks=net10.0 -o "$output" \
    -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false "$@" 2>&1 | tee "$log"

# The origin member follows the warning code, so "IL2026: Bit.Blex.BlexManager..." is raised inside the
# library while "IL2026: Bit.Blex.Tests.Harness.Stores..." is the harness.
blex_warnings="$(grep -E "analysis (warning|error) IL[0-9]{4}: Bit\.Blex\." "$log" | grep -vE "analysis (warning|error) IL[0-9]{4}: Bit\.Blex\.Tests\." | sort -u || true)"

if [ -n "$blex_warnings" ]; then
    echo
    echo "Bit.Blex produced trim/AOT analysis warnings:"
    echo "$blex_warnings"
    exit 1
fi

echo
echo "No trim/AOT analysis warnings originate in Bit.Blex."
