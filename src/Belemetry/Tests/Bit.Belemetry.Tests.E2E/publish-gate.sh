#!/usr/bin/env bash
# Publishes the web harness host the way a WebAssembly app ships (trimmed; pass
# -p:RunAOTCompilation=true for AOT) and fails when the trimmer or the AOT compiler reports an
# analysis warning raised inside Bit.Belemetry. The package is IsTrimmable/IsAotCompatible and runs the
# whole .NET pipeline in the browser, so a warning here is a field the trimmer can remove from under it.
#
# The framework assemblies' own warnings (Microsoft.AspNetCore.Components, Microsoft.JSInterop), reported
# once TrimmerSingleWarn is off, are deliberately not gated on: they are not this library's to fix.
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

output="$1"
shift

here="$(cd "$(dirname "$0")" && pwd)"
project="$here/../Bit.Belemetry.Tests.Harness.Web/Bit.Belemetry.Tests.Harness.Web.csproj"
log="$(mktemp)"

# TargetFrameworks=net10.0 on top of -f: -f alone does not reach project references, which then still
# evaluate every framework they target - and with RunAOTCompilation each of those demands its own
# wasm-tools workload.
dotnet publish "$project" -c Release -f net10.0 -p:TargetFrameworks=net10.0 -o "$output" \
    -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false "$@" 2>&1 | tee "$log"

# The origin member follows the warning code, so "IL2026: Bit.Belemetry.X" is raised inside the library
# while "IL2026: Bit.Belemetry.Tests.Harness.Y" is the harness.
belemetry_warnings="$(grep -E "analysis (warning|error) IL[0-9]{4}: Bit\.Belemetry\." "$log" | grep -vE "analysis (warning|error) IL[0-9]{4}: Bit\.Belemetry\.Tests\." | sort -u || true)"

if [ -n "$belemetry_warnings" ]; then
    echo
    echo "Bit.Belemetry produced trim/AOT analysis warnings:"
    echo "$belemetry_warnings"
    exit 1
fi

echo
echo "No trim/AOT analysis warnings originate in Bit.Belemetry."
