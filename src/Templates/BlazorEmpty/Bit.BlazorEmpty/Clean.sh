#!/bin/bash

# This batch script cleans your project by deleting unnecessary files.
# It is important to close any IDEs, such as vs for mac, before running this script to prevent conflicts or data loss.
# The commands in this script are specifically designed for macOS/Linux.

# Runs the dotnet clean command for each .csproj file.
for csproj in $(find . -name '*.csproj'); do
    dotnet clean $csproj
done

# Deletes specified directories
for dir in $(find . -type d \( -name "bin" -o -name "obj" -o -name "node_modules" -o -name "Packages" -o -name ".vs" -o -name "TestResults" -o -name "AppPackages" -o -name ".meteor" \)); do
    rm -rf $dir
done

# Deletes specified files
for file in $(find . -type f \( -name "*.csproj.user" -o -name "Resources.designer.cs" -o -name "*.css" -o -name "*.min.css" -o -name "*.js" -o -name "*.min.js" -o -name "*.map" \)); do
    rm -f $file
done

# Deletes empty directories.
find . -type d -empty -delete