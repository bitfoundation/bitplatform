#!/usr/bin/env bash
# usage: e2e.sh <feed folder> <old> <mid> <new>
set -uo pipefail

FEED="$1"; OLD="$2"; MID="$3"; NEW="$4"
LAB="$(mktemp -d)"
EXE=""
case "$(uname -s)" in MINGW*|MSYS*) EXE=".exe"; LAB="$(cygpath -m "$LAB")"; FEED="$(cygpath -m "$FEED")";; esac
export DOTNET_CLI_HOME="$LAB/home" BIT_CLI_HOME="$LAB/home" NUGET_PACKAGES="$LAB/packages" DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
unset CI GITHUB_ACTIONS TF_BUILD BIT_CLI_NO_UPDATE BIT_CLI_HANDED_OFF_FROM
BIT="$LAB/home/.dotnet/tools/bit$EXE"
PROJECT="$LAB/work/Contoso"
COPIES="$LAB/home/.bitplatform/cli"
FAILED=0

mkdir -p "$PROJECT/src/Server" "$LAB/home"
printf '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /><add key="lab" value="%s" /></packageSources></configuration>\n' "$FEED" > "$LAB/work/NuGet.config"
echo '<Solution />' > "$PROJECT/Contoso.slnx"

bitjson() { printf '{ "template": "bit-bp", "version": "%s", "cli": { %s }, "name": "Contoso" }\n' "$OLD" "$1" > "$PROJECT/bit.json"; }
check() { if eval "$2"; then echo "  ok    $1"; else echo "  FAIL  $1"; FAILED=1; fi; }
flat() { tr -d '\r' | tr '\n' ' ' | tr -s ' '; }
run() { OUT="$( (cd "$1" && shift && "$BIT" "$@") 2>&1 )"; CODE=$?; FLAT="$(printf '%s' "$OUT" | flat)"; }

echo "== $(uname -sm), dotnet $(dotnet --version)"
(cd "$LAB/work" && dotnet tool install -g Bit.Cli --version "$OLD" >/dev/null 2>&1)
check "the oldest bit is installed" '[ -x "$BIT" ]'

echo "== 1. a bit too old for the project"
bitjson "\"minVersion\": \"$MID\""
run "$PROJECT/src/Server" clean ../..
HANDED_CODE=$CODE
check "it hands the command to the newest bit" '[[ "$FLAT" == *"This project needs bit $MID or newer, so bit $NEW runs this, and your installed bit updates to it when the run ends."* ]]'
check "the copy is there, marked as a project's" '[ -x "$COPIES/$NEW/bit$EXE" ] && [ -f "$COPIES/$NEW/used-by-a-project" ]'
for _ in $(seq 1 60); do dotnet tool list -g 2>/dev/null | grep -q "$NEW" && break; sleep 2; done
check "the installed bit updated itself afterwards" 'dotnet tool list -g | grep -q "$NEW"'

echo "== 2. the installed bit fits now"
run "$PROJECT" clean
check "it runs the command itself" '[[ "$FLAT" != *"This project"* ]]'
check "the handed off run ended with the same exit code ($HANDED_CODE = $CODE)" '[ "$HANDED_CODE" = "$CODE" ] && [ "$CODE" != 0 ]'

echo "== 3. a bit too new for the project"
bitjson "\"minVersion\": \"$OLD\", \"maxVersion\": \"$MID\""
run "$PROJECT" clean
check "it hands the command to the newest bit the project works with" '[[ "$FLAT" == *"This project works with bit up to $MID, so bit $MID runs this."* ]] && [ "$CODE" = "$HANDED_CODE" ]'
run "$PROJECT" clean
check "and again, with the copy it kept" '[[ "$FLAT" == *"so bit $MID runs this."* ]] && [ -f "$COPIES/$MID/used-by-a-project" ]'
check "the installed bit stays" 'dotnet tool list -g | grep -q "$NEW"'

echo "== 4. no bit fits"
bitjson '"minVersion": "99.0.0"'
run "$PROJECT" clean
check "it says so and goes on" '[[ "$FLAT" == *"This project needs bit 99.0.0 or newer, and bit couldn'"'"'t find one, so bit $NEW goes on."* ]] && [ "$CODE" = "$HANDED_CODE" ]'
OUT="$( (cd "$PROJECT" && BIT_CLI_NO_UPDATE=1 "$BIT" clean) 2>&1 )"; FLAT="$(printf '%s' "$OUT" | flat)"
check "with BIT_CLI_NO_UPDATE it only says so" '[[ "$FLAT" == *"This project needs bit 99.0.0 or newer, and this is bit $NEW."* ]]'
OUT="$( (cd "$PROJECT" && GITHUB_ACTIONS=true "$BIT" clean) 2>&1 )"; FLAT="$(printf '%s' "$OUT" | flat)"
check "in CI it only says so" '[[ "$FLAT" == *"and this is bit $NEW."* ]]'
run "$PROJECT" telemetry
check "bit telemetry doesn't read the project" '[[ "$FLAT" != *"This project"* ]]'

echo "== 5. five commands at once that need the same missing copy"
bitjson "\"maxVersion\": \"$MID\""
rm -rf "$COPIES/$MID"
for i in 1 2 3 4 5; do ( (cd "$PROJECT" && "$BIT" clean) > "$LAB/par-$i.txt" 2>&1; echo "exit=$?" >> "$LAB/par-$i.txt" ) & done
wait
OK=0; for i in 1 2 3 4 5; do grep -q "exit=$HANDED_CODE" "$LAB/par-$i.txt" && [[ "$(flat < "$LAB/par-$i.txt")" == *"so bit $MID runs this."* ]] && OK=$((OK + 1)); done
check "all five ran with the copy ($OK of 5)" '[ "$OK" = 5 ]'
INSTALLS="$(cat "$LAB"/home/.bitplatform/logs/clean-*.log | grep -c "> dotnet tool install Bit.Cli --version $MID ")"
check "the copy was downloaded once more only ($INSTALLS downloads of it in all)" '[ "$INSTALLS" = 2 ]'
check "the copy is whole" '[ -x "$COPIES/$MID/bit$EXE" ] && (cd "$PROJECT" && BIT_CLI_HANDED_OFF_FROM=x "$COPIES/$MID/bit$EXE" --version >/dev/null)'

if [ -z "$EXE" ]; then
echo "== 6. Ctrl+C and SIGTERM"
bitjson "\"maxVersion\": \"$MID\""
mv "$COPIES/$MID/bit" "$COPIES/$MID/bit.real"
cat > "$COPIES/$MID/bit" <<'EOF'
#!/bin/sh
trap 'echo other-bit-got-int; exit 42' INT
trap 'echo other-bit-got-term; exit 43' TERM
sleep 120 &
echo $! > "$SLEEP_PID_FILE"
echo other-bit-started
wait $!
EOF
chmod +x "$COPIES/$MID/bit"
export SLEEP_PID_FILE="$LAB/sleep.pid"
started() { for _ in $(seq 1 100); do grep -q other-bit-started "$1" 2>/dev/null && return 0; sleep 0.2; done; return 1; }
set -m

(cd "$PROJECT" && exec "$BIT" clean) > "$LAB/int.txt" 2>&1 &
PARENT=$!
check "the other bit started" 'started "$LAB/int.txt"'
kill -s INT -- "-$PARENT"
wait "$PARENT"; CODE=$?
check "Ctrl+C: the other bit handles it, and its exit code is the run's ($CODE)" 'grep -q other-bit-got-int "$LAB/int.txt" && [ "$CODE" = 42 ]'

(cd "$PROJECT" && exec "$BIT" clean) > "$LAB/term.txt" 2>&1 &
PARENT=$!
check "the other bit started again" 'started "$LAB/term.txt"'
SLEEPER="$(cat "$SLEEP_PID_FILE")"
BEGAN=$(date +%s)
kill -s TERM "$PARENT"
wait "$PARENT"; CODE=$?
TOOK=$(( $(date +%s) - BEGAN ))
sleep 1
check "SIGTERM to bit alone: it ends as canceled ($CODE) within its grace (${TOOK}s)" '[ "$CODE" = 130 ] && [ "$TOOK" -ge 6 ] && [ "$TOOK" -le 14 ]'
check "and leaves nothing of the other bit running" '! kill -0 "$SLEEPER" 2>/dev/null'
set +m
mv "$COPIES/$MID/bit.real" "$COPIES/$MID/bit"
fi

rm -rf "$LAB"
if [ "$FAILED" = 0 ]; then echo "ALL PASSED"; else echo "SOME FAILED"; exit 1; fi
