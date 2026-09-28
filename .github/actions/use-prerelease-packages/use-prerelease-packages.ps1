#Requires -Version 7.4
<#
.SYNOPSIS
    Makes a project restore every Bit.* package from the nupkg-files artifact of the latest successful run of
    prerelease.nuget.org.yml instead of nuget.org. See action.yml next to this file.

.DESCRIPTION
    The artifact carries the version in the branch's Bit.Build.props, which is what the projects of that branch pin,
    so nothing else has to change. nuget.org has the same id and version though, so the artifact is made the only
    source of Bit.* packages through package source mapping; NuGet does not prefer one source over another.
#>
param(
    # Folder of the nuget.config the project restores with. The file is created when it is not there.
    [Parameter(Mandatory)] [string] $ProjectFolder,

    # owner/name of the repository the prerelease runs belong to.
    [Parameter(Mandatory)] [string] $Repository,

    # Where the artifact is downloaded to. It becomes the package source.
    [Parameter(Mandatory)] [string] $PackagesFolder
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

# The successful run that finished last, so a re-run of an older one counts as the latest. Sorted here: with
# --status success the API does not keep its newest first order.
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

# Id and version as the nuspec inside each package states them; a file name alone cannot tell where the id ends.
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

# A copy of the same id and version that nuget.org already put in the global packages folder would be used as is.
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

# By name rather than by path: the Boilerplate template renames "Boilerplate" wherever it appears in a file it
# emits, paths included. NuGet expands the variable, which the steps after this one get through GITHUB_ENV.
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
    # Without a mapping every other source keeps serving everything, Bit.* included.
    $mapping = $configuration.AppendChild($config.CreateElement('packageSourceMapping'))

    foreach ($key in $sources.SelectNodes('add').key | Where-Object { $_ -ne 'bit-prerelease' }) {
        $other = $mapping.AppendChild($config.CreateElement('packageSource'))
        $other.SetAttribute('key', $key)
        $other.AppendChild($config.CreateElement('package')).SetAttribute('pattern', '*')
    }
}

# The longest matching pattern wins, so Bit.* comes from here and nowhere else.
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
