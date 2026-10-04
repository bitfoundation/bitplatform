#!/usr/bin/env bash
# Publishes the web harness host the way a WebAssembly app ships (trimmed; pass
# -p:RunAOTCompilation=true for AOT) and fails when the trimmer or the AOT compiler reports an
# analysis warning raised inside Bit.Brouter. src/publish-gate.sh, shared by every library's gate,
# does the publishing and the matching.
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

here="$(cd "$(dirname "$0")" && pwd)"

# TargetFrameworks=net11.0 on top of -f: -f alone does not reach project references, which then still
# evaluate every framework they target - and with RunAOTCompilation each of those demands its own
# wasm-tools workload.
exec bash "$here/../../../publish-gate.sh" Bit.Brouter Tests "$here/../Bit.Brouter.Tests.Harness.Web/Bit.Brouter.Tests.Harness.Web.csproj" \
    "$1" -f net11.0 -p:TargetFrameworks=net11.0 "${@:2}"
