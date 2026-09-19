#!/usr/bin/env bash
# Publishes the Blazor Web App harness host the way an app ships - its WebAssembly client trimmed; pass
# -p:RunAOTCompilation=true for AOT - and fails when the trimmer or the AOT compiler reports an analysis
# warning raised inside Bit.Butil. The E2E suite then runs against the output (BUTIL_E2E_PUBLISHED_HOST),
# which is where a type trimmed out from under System.Text.Json, a constructor DI can no longer find, or a
# JavaScript module the publish-time bundle trimming dropped shows up - none of it is visible in a Debug build.
#
# Not gated on: the framework assemblies' own warnings (Microsoft.AspNetCore.Components, Microsoft.JSInterop),
# which they report once TrimmerSingleWarn is off and which are not this library's to fix, and warnings raised in
# the samples and harness code (Bit.Butil.Samples.*, ButilTests.*), which are the app's, not the library's.
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

output="$1"
shift

here="$(cd "$(dirname "$0")" && pwd)"
project="$here/../Bit.Butil.Tests.Harness.Web/Bit.Butil.Tests.Harness.Web.csproj"
log="$(mktemp)"

# ButilHarnessFramework=net10.0 on top of -f: -f alone does not reach project references, so the WebAssembly
# client would still evaluate every framework it targets - and with RunAOTCompilation each of those demands its
# own wasm-tools workload. Not -p:TargetFrameworks, which would also reach the netstandard2.0 Bit.Butil.Build.
dotnet publish "$project" -c Release -f net10.0 -p:ButilHarnessFramework=net10.0 -o "$output" \
    -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false "$@" 2>&1 | tee "$log"

# The origin member follows the warning code, so "IL2026: Bit.Butil.Clipboard.X" is raised inside the library
# while "IL2026: Bit.Butil.Samples.Core.Y" is the samples.
butil_warnings="$(grep -E "(analysis )?(warning|error) IL[0-9]{4}: Bit\.Butil\." "$log" | grep -vE "IL[0-9]{4}: Bit\.Butil\.(Samples|Tests)\." | sort -u || true)"

if [ -n "$butil_warnings" ]; then
    echo
    echo "Bit.Butil produced trim/AOT analysis warnings:"
    echo "$butil_warnings"
    exit 1
fi

echo
echo "No trim/AOT analysis warnings originate in Bit.Butil."
