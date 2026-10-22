#!/usr/bin/env bash
# Publishes the demo (Bit.Bdnd.Demo.Server) as bdnd.demo.cd.yml deploys it - its WebAssembly client trimmed, and with
# the wasm-tools workload installed, as the deployment has it, its runtime relinked - and fails on any trim/AOT
# analysis warning raised inside Bit.Bdnd. Pass -p:RunAOTCompilation=true for AOT: the deployed demo is not, but an
# app using the library may be. The E2E suite then runs against the output (BDND_E2E_PUBLISHED_HOST), which is where
# an interop type trimmed out from under System.Text.Json shows up: a registration whose options can no longer be
# serialized throws from every draggable and drop target, and none of it is visible in a build output.
#
# src/publish-gate.sh, shared by every library's gate, does the publishing and the matching. Not gated on: the
# framework assemblies' own warnings (Microsoft.AspNetCore.Components, Microsoft.JSInterop), which they report once
# TrimmerSingleWarn is off and which are not this library's to fix, and warnings raised in the demo (Bit.Bdnd.Demo.*),
# which are the app's, not the library's.
#
# Run it from inside src (src/Utilities/Bdnd), where src/global.json picks the SDK.
#
# Usage: publish-gate.sh <output-folder> [extra dotnet publish arguments...]
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"

# Unlike the other publish gates, no -p:TargetFrameworks=net10.0: the WebAssembly client would get it too and then
# no longer resolve its runtime pack (WASM0005). Nor is it needed: the client, a net10.0 app, is the only project
# AOT compiles, and Bit.Bdnd's other frameworks demand no wasm-tools workload of their own.
exec bash "$here/../../../../publish-gate.sh" Bit.Bdnd 'Demo|Tests' "$here/../../Demo/Server/Bit.Bdnd.Demo.Server.csproj" "$@"
