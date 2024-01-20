:: This batch script is designed for comprehensive cleaning of your project by deleting unnecessary files.
:: It's crucial to close any Integrated Development Environment (IDE), such as Visual Studio, etc., before executing this script to prevent any conflicts or loss of unsaved data.
:: Please note that the commands included in this script are specifically tailored for the Windows

:: Delete css,js and source maps files if not tracked in git
powershell -Command "[string]$trackedFiles = git ls-files; Get-ChildItem -Force -Include *.css,*.min.css,*.js,*.min.js,*.map -Recurse | ForEach-Object { if ($trackedFiles -NotMatch $_.Name) { Remove-Item -Recurse -Path $_ -Confirm:$false -Force }}"

:: Runs dotnet clean for each csproj file
powershell -Command "Get-ChildItem -Force -Include *.csproj -Recurse | ForEach-Object { dotnet clean $_.FullName }"

:: Delete specified files & folders
powershell -Command "Get-ChildItem -Force -Include *.csproj.user,Resources.designer.cs,bin,obj,node_modules,Packages,TestResults,AppPackages,.meteor -Recurse | ForEach-Object { Remove-Item -Recurse -Path $_ -Confirm:$false -Force }"
FOR /d /r . %%d IN (.vs) DO @IF EXIST "%%d" rd /s /q "%%d"

:: Delete empty directories
powershell -Command "Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer -and @(Get-ChildItem -Lit $_.FullName).Count -eq 0 } | Remove-Item -Confirm:$false -Force"