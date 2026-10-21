#!/usr/bin/env bash
# Publishes the demo (Bit.Bdnd.Demo.Server) the way it ships - its WebAssembly client trimmed; pass
# -p:RunAOTCompilation=true for AOT - and fails when the trimmer or the AOT compiler reports an analysis warning
# raised inside Bit.Bdnd. The E2E suite then runs against the output (BDND_E2E_PUBLISHED_HOST), which is where an
# interop type trimmed out from under System.Text.Json shows up: a registration whose options can no longer be
# serialized throws from every draggable and drop target, and none of it is visible in a build output.
#
# Not gated on: the framework assemblies' own warnings (Microsoft.AspNetCore.Components, Microsoft.JSInterop),
# which they report once TrimmerSingleWarn is off and which are not this library's to fix, and warnings raised in
# the demo (Bit.Bdnd.Demo.*), which are the app's, not the library's.
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

output="$1"
shift

here="$(cd "$(dirname "$0")" && pwd)"
project="$here/../../Demo/Server/Bit.Bdnd.Demo.Server.csproj"
log="$(mktemp)"

# Unlike the other publish gates, no -p:TargetFrameworks=net10.0: the WebAssembly client would get it too and then
# no longer resolve its runtime pack (WASM0005). Nor is it needed: the client, a net10.0 app, is the only project
# AOT compiles, and Bit.Bdnd's other frameworks demand no wasm-tools workload of their own.
dotnet publish "$project" -c Release -o "$output" \
    -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false "$@" 2>&1 | tee "$log"

# The origin member follows the warning code, so "IL2026: Bit.Bdnd.BdndDraggable`1.X" is raised inside the library
# while "IL2026: Bit.Bdnd.Demo.Client.Y" is the demo.
bdnd_warnings="$(grep -E "(analysis )?(warning|error) IL[0-9]{4}: Bit\.Bdnd\." "$log" | grep -vE "IL[0-9]{4}: Bit\.Bdnd\.(Demo|Tests)\." | sort -u || true)"

if [ -n "$bdnd_warnings" ]; then
    echo
    echo "Bit.Bdnd produced trim/AOT analysis warnings:"
    echo "$bdnd_warnings"
    exit 1
fi

echo
echo "No trim/AOT analysis warnings originate in Bit.Bdnd."
