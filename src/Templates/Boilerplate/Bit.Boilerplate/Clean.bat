:: This batch script cleans your project by deleting unnecessary files.
:: It is important to close any IDEs, such as Visual Studio, before running this script to prevent conflicts or data loss.
:: The commands in this script are specifically designed for Windows.

:: Deletes CSS, JS, and source map files that are not tracked in Git.
powershell -Command "[string]$trackedFiles = git ls-files; Get-ChildItem -Force -Include *.css,*.min.css,*.js,*.min.js,*.map -Recurse | ForEach-Object { if ($trackedFiles -NotMatch $_.Name) { Remove-Item -Recurse -Path $_ -Confirm:$false -Force }}"

:: Runs the dotnet clean command for each .csproj file.
powershell -Command "Get-ChildItem -Force -Include *.csproj -Recurse | ForEach-Object { dotnet clean $_.FullName }"

:: Deletes the specified files and folders.
powershell -Command "Get-ChildItem -Force -Include *.csproj.user,Resources.designer.cs,bin,obj,node_modules,Packages,TestResults,AppPackages,.meteor -Recurse | ForEach-Object { Remove-Item -Recurse -Path $_ -Confirm:$false -Force }"
FOR /d /r . %%d IN (.vs) DO @IF EXIST "%%d" rd /s /q "%%d"

:: Deletes empty directories.
powershell -Command "Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer -and @(Get-ChildItem -Lit $_.FullName).Count -eq 0 } | Remove-Item -Confirm:$false -Force"