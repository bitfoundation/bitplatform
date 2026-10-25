#!/usr/bin/env bash
# Publishes the web harness host the way a WebAssembly app ships (trimmed; pass
# -p:RunAOTCompilation=true for AOT) and fails when the trimmer or the AOT compiler reports an
# analysis warning raised inside Bit.Belemetry. The package is IsTrimmable/IsAotCompatible and runs the
# whole .NET pipeline in the browser, so a warning here is a field the trimmer can remove from under it.
# src/publish-gate.sh, shared by every library's gate, does the publishing and the matching.
#
# The framework assemblies' own warnings (Microsoft.AspNetCore.Components, Microsoft.JSInterop), reported
# once TrimmerSingleWarn is off, are deliberately not gated on: they are not this library's to fix.
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"

# TargetFrameworks=net10.0 on top of -f: -f alone does not reach project references, which then still
# evaluate every framework they target - and with RunAOTCompilation each of those demands its own
# wasm-tools workload.
exec bash "$here/../../../../publish-gate.sh" Bit.Belemetry Tests "$here/../Bit.Belemetry.Tests.Harness.Web/Bit.Belemetry.Tests.Harness.Web.csproj" \
    "$1" -f net10.0 -p:TargetFrameworks=net10.0 "${@:2}"
