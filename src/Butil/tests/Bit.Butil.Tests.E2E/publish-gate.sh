#!/usr/bin/env bash
# Publishes the Blazor Web App harness host the way an app ships - its WebAssembly client trimmed; pass
# -p:RunAOTCompilation=true for AOT - and fails when the trimmer or the AOT compiler reports an analysis
# warning raised inside Bit.Butil. The E2E suite then runs against the output (BUTIL_E2E_PUBLISHED_HOST),
# which is where a type trimmed out from under System.Text.Json, a constructor DI can no longer find, or a
# JavaScript module the publish-time bundle trimming dropped shows up - none of it is visible in a Debug build.
#
# src/publish-gate.sh, shared by every library's gate, does the publishing and the matching. Not gated on: the
# framework assemblies' own warnings (Microsoft.AspNetCore.Components, Microsoft.JSInterop), which they report once
# TrimmerSingleWarn is off and which are not this library's to fix, and warnings raised in the samples and harness
# code (Bit.Butil.Samples.*, ButilTests.*), which are the app's, not the library's.
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"

# ButilHarnessFramework=net11.0 on top of -f: -f alone does not reach project references, so the WebAssembly
# client would still evaluate every framework it targets - and with RunAOTCompilation each of those demands its
# own wasm-tools workload. Not -p:TargetFrameworks, which would also reach the netstandard2.0 Bit.Butil.Build.
exec bash "$here/../../../publish-gate.sh" Bit.Butil 'Samples|Tests' "$here/../Bit.Butil.Tests.Harness.Web/Bit.Butil.Tests.Harness.Web.csproj" \
    "$1" -f net11.0 -p:ButilHarnessFramework=net11.0 "${@:2}"
