#!/bin/bash

# This batch script cleans your project by deleting unnecessary files.
# It is important to close any IDEs, such as vs for mac, before running this script to prevent conflicts or data loss.
# The commands in this script are specifically designed for macOS/Linux.

# Runs the dotnet clean command for each .csproj file.
find . -name '*.csproj' -print0 | while IFS= read -r -d '' csproj; do
    dotnet clean "$csproj"
done

# Deletes specified directories
find . -type d \( -name "bin" -o -name "obj" -o -name "node_modules" -o -name "Packages" -o -name ".vs" -o -name "TestResults" -o -name "AppPackages" -o -name ".meteor" -o -name ".playwright-mcp" \) -prune -print0 | while IFS= read -r -d '' dir; do
    rm -rf "$dir"
done

# Deletes CSS, JS, and source map files that are not tracked in Git (e.g. wwwroot/service-worker.js is git-tracked source code and must survive).
if git rev-parse --is-inside-work-tree > /dev/null 2>&1; then
    find . -type f \( -name "*.csproj.user" -o -name "Resources.designer.cs" -o -name "*.css" -o -name "*.min.css" -o -name "*.js" -o -name "*.min.js" -o -name "*.map" -o -name "*.proj.Backup.tmp" \) -print0 | while IFS= read -r -d '' file; do
        git ls-files --error-unmatch "$file" > /dev/null 2>&1 || rm -f "$file"
    done
else
    echo "Warning: not a git repository - skipping cleanup of css/js/map files, because git-tracked source files (e.g. service-worker.js) cannot be told apart from build outputs."
fi

# Deletes empty directories.
find . -type d -empty -delete
