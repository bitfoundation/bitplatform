@echo off
setlocal
cd /d "%~dp0"

set EXIT_CODE=0

echo ================================ Web tests on chromium ================================
dotnet test --filter "TestCategory=Web"
if errorlevel 1 set EXIT_CODE=1

echo ================================ Web tests on firefox =================================
set BROWSER=firefox
dotnet test --filter "TestCategory=Web" --no-build
if errorlevel 1 set EXIT_CODE=1
set BROWSER=

echo ==================== Web tests on webkit (Safari) on the remote mac ===================
set BROWSER=webkit
set PLAYWRIGHT_SERVER_ENDPOINT=ws://192.168.178.24:4444/
dotnet test --filter "TestCategory=Web" --no-build
if errorlevel 1 set EXIT_CODE=1
set BROWSER=
set PLAYWRIGHT_SERVER_ENDPOINT=

echo ================================== Windows app tests ==================================
dotnet test --filter "TestCategory=Windows" --no-build
if errorlevel 1 set EXIT_CODE=1

echo ================================== Android app tests ==================================
dotnet test --filter "TestCategory=Android" --no-build
if errorlevel 1 set EXIT_CODE=1

exit /b %EXIT_CODE%
