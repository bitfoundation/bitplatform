@echo off
setlocal
cd /d "%~dp0"

set EXIT_CODE=0

set PLAYWRIGHT_SERVER_ENDPOINT=ws://192.168.178.24:4444/

echo ======================= Web tests on chromium on the remote mac =======================
dotnet test --filter "TestCategory=Web"
if errorlevel 1 set EXIT_CODE=1

echo ==================== Web tests on webkit (Safari) on the remote mac ===================
set BROWSER=webkit
dotnet test --filter "TestCategory=Web" --no-build
if errorlevel 1 set EXIT_CODE=1
set PLAYWRIGHT_SERVER_ENDPOINT=

echo ================================ Web tests on firefox =================================
set BROWSER=firefox
dotnet test --filter "TestCategory=Web" --no-build
if errorlevel 1 set EXIT_CODE=1
set BROWSER=

echo ================================== Android app tests ==================================
dotnet test --filter "TestCategory=Android" --no-build
if errorlevel 1 set EXIT_CODE=1

echo ================================== Windows app tests ==================================
dotnet test --filter "TestCategory=Windows" --no-build
if errorlevel 1 set EXIT_CODE=1

echo =============================== API and database tests ===============================
dotnet test --filter "TestCategory=Api" --no-build
if errorlevel 1 set EXIT_CODE=1

exit /b %EXIT_CODE%
