:: This batch script cleans your project by deleting unnecessary files.
:: It is important to close any IDEs, such as Visual Studio, before running this script to prevent conflicts or data loss.
:: The commands in this script are specifically designed for Windows.

:: Deletes CSS, JS, and source map files that are not tracked in Git (e.g. wwwroot/service-worker.js is git-tracked source code and must survive).
powershell -NoProfile -Command "$trackedFiles = @(git ls-files 2>$null); if ($trackedFiles.Count -eq 0) { Write-Warning 'Not a git repository - skipping cleanup of css/js/map files, because git-tracked source files (e.g. service-worker.js) cannot be told apart from build outputs.' } else { $tracked = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase); foreach ($t in $trackedFiles) { [void]$tracked.Add([System.IO.Path]::GetFullPath((Join-Path $PWD.Path $t.Replace('/', '\')))) }; Get-ChildItem -Force -File -Include *.css,*.min.css,*.js,*.min.js,*.map,*.proj.Backup.tmp -Recurse | ForEach-Object { if ($tracked.Contains($_.FullName) -eq $false) { Remove-Item -LiteralPath $_.FullName -Force -Confirm:$false } } }"

:: Runs the dotnet clean command for each .csproj file.
powershell -Command "Get-ChildItem -Force -Include *.csproj -Recurse | ForEach-Object { dotnet clean $_.FullName }"

:: Deletes the specified files and folders.
powershell -Command "Get-ChildItem -Force -Include *.csproj.user,Resources.designer.cs,bin,obj,node_modules,Packages,TestResults,AppPackages,.meteor,.playwright-mcp -Recurse | ForEach-Object { Remove-Item -Recurse -Path $_ -Confirm:$false -Force }"
FOR /d /r . %%d IN (.vs) DO @IF EXIST "%%d" rd /s /q "%%d"

:: Deletes empty directories.
powershell -Command "Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer -and @(Get-ChildItem -Lit $_.FullName).Count -eq 0 } | Remove-Item -Confirm:$false -Force"
