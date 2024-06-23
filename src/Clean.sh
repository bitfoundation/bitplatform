#!/bin/bash

# This batch script is designed for comprehensive cleaning of your project by deleting unnecessary files.
# It's crucial to close any Integrated Development Environment (IDE), such as vs code, etc., before executing this script to prevent any conflicts or loss of unsaved data.
# Please note that the commands included in this script are specifically tailored for the Linux/macOS

# Runs dotnet clean for each csproj file
for csproj in $(find . -name '*.csproj'); do
    dotnet clean $csproj
done

# Delete specified directories
for dir in $(find . -type d \( -name "bin" -o -name "obj" -o -name "node_modules" -o -name "Packages" -o -name ".vs" -o -name "TestResults" -o -name "AppPackages" -o -name ".meteor" \)); do
    rm -rf $dir
done

# Delete specified files
for file in $(find . -type f \( -name "*.csproj.user" -o -name "Resources.designer.cs" -o -name "*.css" -o -name "*.min.css" -o -name "*.js" -o -name "*.min.js" -o -name "*.map" \)); do
    rm -f $file
done

# Delete empty directories
find . -type d -empty -delete