#!/usr/bin/env bash
# Publishes the web harness host the way a WebAssembly app ships (trimmed; pass
# -p:RunAOTCompilation=true for AOT) and fails when the trimmer or the AOT compiler reports an analysis
# warning raised inside one of the Bit.Blex libraries. src/publish-gate.sh, shared by every library's
# gate, does the publishing and the matching.
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

here="$(cd "$(dirname "$0")" && pwd)"

# TargetFrameworks=net10.0 on top of -f: -f alone does not reach project references, which then still
# evaluate every framework they target - and with RunAOTCompilation each of those demands its own
# wasm-tools workload.
exec bash "$here/../../../../publish-gate.sh" Bit.Blex Tests "$here/../Bit.Blex.Tests.Harness.Web/Bit.Blex.Tests.Harness.Web.csproj" \
    "$1" -f net10.0 -p:TargetFrameworks=net10.0 "${@:2}"
