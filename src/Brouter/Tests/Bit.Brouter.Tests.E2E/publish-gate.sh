#!/usr/bin/env bash
# Publishes the web harness host the way a WebAssembly app ships (trimmed; pass
# -p:RunAOTCompilation=true for AOT) and fails when the trimmer or the AOT compiler reports an
# analysis warning raised inside Bit.Brouter.
#
# Two kinds of warning are deliberately not gated on:
#  - the framework assemblies' own (Microsoft.AspNetCore.Components, Microsoft.JSInterop), which they
#    report once TrimmerSingleWarn is off and which are not this library's to fix;
#  - IL2110/IL2111 raised in application code (the harness) where a component-typed [Parameter] is set,
#    e.g. <Broute Component="...">. Blazor's own LayoutView.Layout and DynamicComponent.Type raise the
#    identical warnings at their call sites: it is how the trimmer reports any
#    [DynamicallyAccessedMembers] component parameter, not something Brouter's API does differently.
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

output="$1"
shift

here="$(cd "$(dirname "$0")" && pwd)"
project="$here/../Bit.Brouter.Tests.Harness.Web/Bit.Brouter.Tests.Harness.Web.csproj"
log="$(mktemp)"

# TargetFrameworks=net10.0 on top of -f: -f alone does not reach project references, which then still
# evaluate every framework they target - and with RunAOTCompilation each of those demands its own
# wasm-tools workload.
dotnet publish "$project" -c Release -f net10.0 -p:TargetFrameworks=net10.0 -o "$output" \
    -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false "$@" 2>&1 | tee "$log"

# The origin member follows the warning code, so "IL2069: Bit.Brouter.X" is raised inside the library
# while "IL2111: Bit.Brouter.Tests.Harness.Y" is the harness.
brouter_warnings="$(grep -E "analysis (warning|error) IL[0-9]{4}: Bit\.Brouter\." "$log" | grep -vE "analysis (warning|error) IL[0-9]{4}: Bit\.Brouter\.Tests\." | sort -u || true)"

if [ -n "$brouter_warnings" ]; then
    echo
    echo "Bit.Brouter produced trim/AOT analysis warnings:"
    echo "$brouter_warnings"
    exit 1
fi

echo
echo "No trim/AOT analysis warnings originate in Bit.Brouter."
