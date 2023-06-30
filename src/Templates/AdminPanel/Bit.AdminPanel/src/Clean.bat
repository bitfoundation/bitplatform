:: This batch script is designed for comprehensive cleaning of your project by deleting unnecessary files.
:: It's crucial to close any Integrated Development Environment (IDE), such as Visual Studio, etc., before executing this script to prevent any conflicts or loss of unsaved data.
:: Please note that the commands included in this script are specifically tailored for the Windows

:: Delete css,js and source maps files if not tracked in git
powershell -Command "[string]$trackedFiles = git ls-files; Get-ChildItem -Include *.css,*.min.css,*.js,*.min.js,*.map -Recurse | ForEach-Object { if ($trackedFiles -NotMatch $_.Name) { Remove-Item -Recurse -Path $_ -Confirm:$false -Force }}"

@REM :: Runs dotnet clean for each csproj file
@REM powershell -Command "Get-ChildItem -Include *.csproj -Recurse | ForEach-Object { dotnet clean $_.FullName }"

@REM :: Delete specified directories
@REM powershell -Command "Get-ChildItem -Include bin,obj,node_modules,Packages,.vs,TestResults,AppPackages -Recurse -Directory | Remove-Item -Recurse -Confirm:$false -Force"

@REM :: Delete specified files
@REM powershell -Command "Get-ChildItem -Include *.csproj.user,Resources.designer.cs -Recurse | Remove-Item -Confirm:$false -Force"

@REM :: Delete empty directories
@REM powershell -Command "Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer -and @(Get-ChildItem -Lit $_.FullName).Count -eq 0 } | Remove-Item -Confirm:$false -Force"