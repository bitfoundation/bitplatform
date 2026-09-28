#Requires -Version 7.4
# Makes a project restore every Bit.* package from the nupkg-files artifact of the latest successful
# prerelease.nuget.org.yml run. See action.yml.
param(
    [Parameter(Mandatory)] [string] $ProjectFolder, # Of the nuget.config to use; created when missing.
    [Parameter(Mandatory)] [string] $Repository,
    [Parameter(Mandatory)] [string] $PackagesFolder
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

# The last one to finish, so a re-run counts; with --status success the API is not newest first.
$run = gh run list --repo $Repository --workflow prerelease.nuget.org.yml --limit 20 --json databaseId,headBranch,headSha,url,conclusion,updatedAt |
    ConvertFrom-Json |
    Where-Object conclusion -eq 'success' |
    Sort-Object { [datetimeoffset]$_.updatedAt } -Descending |
    Select-Object -First 1

if (-not $run) {
    throw "$Repository has no successful run of prerelease.nuget.org.yml to take the packages from."
}

New-Item -ItemType Directory -Force $PackagesFolder | Out-Null

try {
    gh run download $run.databaseId --repo $Repository --name nupkg-files --dir $PackagesFolder
}
catch {
    throw "The nupkg-files artifact of $($run.url) could not be downloaded, it may have expired. Run the Prerelease nuget packages workflow again. $_"
}

$PackagesFolder = (Resolve-Path $PackagesFolder).Path

# From each nuspec: a file name alone cannot tell where the id ends.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$packages = foreach ($nupkg in Get-ChildItem $PackagesFolder -Filter *.nupkg) {
    $zip = [IO.Compression.ZipFile]::OpenRead($nupkg.FullName)

    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -notlike '*/*' -and $_.Name -like '*.nuspec' } | Select-Object -First 1
        $reader = [IO.StreamReader]::new($entry.Open())

        try {
            $metadata = ([xml]$reader.ReadToEnd()).package.metadata
            [pscustomobject]@{ Id = $metadata.id; Version = $metadata.version }
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $zip.Dispose()
    }
}

# A nuget.org copy of the same version in the global packages folder would be used as is.
$globalPackagesFolder = $env:NUGET_PACKAGES ? $env:NUGET_PACKAGES : (Join-Path $HOME '.nuget' 'packages')

foreach ($package in $packages) {
    Remove-Item (Join-Path $globalPackagesFolder $package.Id.ToLowerInvariant() $package.Version.ToLowerInvariant()) -Recurse -Force -ErrorAction SilentlyContinue
}

$configFile = Get-ChildItem $ProjectFolder -File | Where-Object Name -ieq 'nuget.config' | Select-Object -First 1

if (-not $configFile) {
    Set-Content (Join-Path $ProjectFolder 'nuget.config') @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
</configuration>
'@

    $configFile = Get-Item (Join-Path $ProjectFolder 'nuget.config')
}

$config = [xml](Get-Content $configFile.FullName -Raw)
$configuration = $config.DocumentElement

# A variable, not a path: the template renames "Boilerplate" in what it emits, paths included.
$env:BIT_PRERELEASE_PACKAGES = $PackagesFolder

if ($env:GITHUB_ENV) {
    Add-Content $env:GITHUB_ENV "BIT_PRERELEASE_PACKAGES=$PackagesFolder"
}

$sources = $configuration.SelectSingleNode('packageSources') ?? $configuration.AppendChild($config.CreateElement('packageSources'))
$source = $sources.AppendChild($config.CreateElement('add'))
$source.SetAttribute('key', 'bit-prerelease')
$source.SetAttribute('value', '%BIT_PRERELEASE_PACKAGES%')

$mapping = $configuration.SelectSingleNode('packageSourceMapping')

if (-not $mapping) {
    # Without a mapping any source may serve Bit.* too.
    $mapping = $configuration.AppendChild($config.CreateElement('packageSourceMapping'))

    foreach ($key in $sources.SelectNodes('add').key | Where-Object { $_ -ne 'bit-prerelease' }) {
        $other = $mapping.AppendChild($config.CreateElement('packageSource'))
        $other.SetAttribute('key', $key)
        $other.AppendChild($config.CreateElement('package')).SetAttribute('pattern', '*')
    }
}

# The longest pattern wins: Bit.* comes only from here.
$bit = $mapping.AppendChild($config.CreateElement('packageSource'))
$bit.SetAttribute('key', 'bit-prerelease')
$bit.AppendChild($config.CreateElement('package')).SetAttribute('pattern', 'Bit.*')

$config.Save($configFile.FullName)

$versions = ($packages | Where-Object Id -like 'Bit.*' | ForEach-Object Version | Sort-Object -Unique) -join ', '
$message = "Bit.* $versions from prerelease run $($run.databaseId), $($run.headBranch) at $($run.headSha.Substring(0, 9))"

Write-Host "::notice title=Bit packages::$message"

if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content $env:GITHUB_STEP_SUMMARY "### Bit packages`n`n$message ($($run.url))`n"
}
