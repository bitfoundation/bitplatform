#Requires -Version 7.4
# Makes a project restore every Bit.* package from the newest nupkg-files artifact of the
# prerelease.nuget.org.yml runs. See action.yml.
param(
    [Parameter(Mandatory)] [string] $ProjectFolder, # Of the nuget.config to use; created when missing.
    [Parameter(Mandatory)] [string] $Repository,
    [Parameter(Mandatory)] [string] $PackagesFolder
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$runs = gh run list --repo $Repository --workflow prerelease.nuget.org.yml --limit 100 --json databaseId,headBranch,headSha,url |
    ConvertFrom-Json

$artifact = (gh api "repos/$Repository/actions/artifacts?name=nupkg-files&per_page=100" | ConvertFrom-Json).artifacts |
    Where-Object { -not $_.expired -and $_.workflow_run.id -in $runs.databaseId } |
    Sort-Object { [datetimeoffset]$_.created_at } -Descending |
    Select-Object -First 1

$run = $runs | Where-Object databaseId -eq $artifact.workflow_run.id

if (-not $run) {
    throw "$Repository has no run of prerelease.nuget.org.yml with an unexpired nupkg-files artifact to take the packages from."
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

$versions = @($packages | Where-Object Id -like 'Bit.*' | ForEach-Object Version | Sort-Object -Unique)
$checkoutVersion = ([xml](Get-Content (Join-Path $PSScriptRoot '..' '..' '..' 'src' 'Bit.Build.props') -Raw)).Project.PropertyGroup.ReleaseVersion | Where-Object { $_ } | Select-Object -First 1

# Checked before anything changes: the restore would fail anyway, with far less to go on.
if ($versions -notcontains $checkoutVersion) {
    throw "$($run.url) ($($run.headBranch)) built Bit.* $($versions -join ', '), but this checkout is on $checkoutVersion (src/Bit.Build.props). Run Prerelease nuget packages on a branch at $checkoutVersion."
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

# A Bit pattern of another source would win (a longer one) or share (the same one).
foreach ($pattern in @($mapping.SelectNodes('packageSource/package')) | Where-Object { $_.GetAttribute('pattern') -like 'Bit.*' }) {
    $owner = $pattern.ParentNode
    $owner.RemoveChild($pattern) | Out-Null

    if ($owner.SelectNodes('package').Count -eq 0) {
        $mapping.RemoveChild($owner) | Out-Null
    }
}

# The longest pattern wins: Bit.* comes only from here.
$bit = $mapping.AppendChild($config.CreateElement('packageSource'))
$bit.SetAttribute('key', 'bit-prerelease')
$bit.AppendChild($config.CreateElement('package')).SetAttribute('pattern', 'Bit.*')

$config.Save($configFile.FullName)

$message = "Bit.* $($versions -join ', ') from prerelease run $($run.databaseId), $($run.headBranch) at $($run.headSha.Substring(0, 9))"

Write-Host "::notice title=Bit packages::$message"

if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content $env:GITHUB_STEP_SUMMARY "### Bit packages`n`n$message ($($run.url))`n"
}
