#Requires -Version 7.4
param(
    [Parameter(Mandatory)] [string] $Id,
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $Installer,
    [Parameter(Mandatory)] [string] $InstallerUrl,
    [Parameter(Mandatory)] [ValidateSet('x64', 'x86')] [string] $Architecture,
    [Parameter(Mandatory)] [string] $ProductCode,
    [Parameter(Mandatory)] [string] $Name,
    [Parameter(Mandatory)] [string] $ShortDescription,
    [Parameter(Mandatory)] [string] $PackageUrl,
    [string] $PrivacyUrl,
    [Parameter(Mandatory)] [string] $WorkFolder
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function ConvertTo-YamlString([string] $value) {
    "'" + $value.Replace("'", "''") + "'"
}

function New-Manifests([string] $folder) {
    New-Item -ItemType Directory -Force $folder | Out-Null
    $sha256 = (Get-FileHash $Installer -Algorithm SHA256).Hash
    $privacy = $PrivacyUrl ? "PrivacyUrl: $(ConvertTo-YamlString $PrivacyUrl)`n" : ''

    Set-Content (Join-Path $folder "$Id.yaml") -Encoding utf8NoBOM -NoNewline -Value @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.10.0.schema.json
PackageIdentifier: $Id
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.10.0

"@

    Set-Content (Join-Path $folder "$Id.installer.yaml") -Encoding utf8NoBOM -NoNewline -Value @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.10.0.schema.json
PackageIdentifier: $Id
PackageVersion: $Version
InstallerType: exe
Scope: user
InstallModes:
- interactive
- silent
- silentWithProgress
InstallerSwitches:
  Silent: --silent
  SilentWithProgress: --silent
UpgradeBehavior: install
RequireExplicitUpgrade: true
ProductCode: $(ConvertTo-YamlString $ProductCode)
Installers:
- Architecture: $Architecture
  InstallerUrl: $(ConvertTo-YamlString $InstallerUrl)
  InstallerSha256: $sha256
ManifestType: installer
ManifestVersion: 1.10.0

"@

    Set-Content (Join-Path $folder "$Id.locale.en-US.yaml") -Encoding utf8NoBOM -NoNewline -Value @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.10.0.schema.json
PackageIdentifier: $Id
PackageVersion: $Version
PackageLocale: en-US
Publisher: BitPlatform
PublisherUrl: https://bitplatform.dev
PublisherSupportUrl: https://github.com/bitfoundation/bitplatform/issues
$($privacy)PackageName: $(ConvertTo-YamlString $Name)
PackageUrl: $(ConvertTo-YamlString $PackageUrl)
License: MIT
LicenseUrl: https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE
ShortDescription: $(ConvertTo-YamlString $ShortDescription)
ManifestType: defaultLocale
ManifestVersion: 1.10.0

"@
}

$publisher, $package = $Id -split '\.', 2
$manifests = Join-Path $WorkFolder "manifests/$($publisher.Substring(0, 1).ToLowerInvariant())/$publisher/$package/$Version"

$wingetcreate = Join-Path $WorkFolder 'wingetcreate.exe'
New-Item -ItemType Directory -Force $WorkFolder | Out-Null
Invoke-WebRequest https://github.com/microsoft/winget-create/releases/download/v1.12.13.0/wingetcreate.exe -OutFile $wingetcreate

if ((Get-FileHash $wingetcreate -Algorithm SHA256).Hash -ne '24042BD37915805615E6CF969AC57C6439124C3FE85823327F5F3FB24BD9FFEA') {
    throw 'wingetcreate.exe is not the build this script pins.'
}

$listed = $true
$headers = @{ Authorization = "Bearer $env:WINGET_CREATE_GITHUB_TOKEN" }

try {
    Invoke-RestMethod "https://api.github.com/repos/microsoft/winget-pkgs/contents/manifests/$($publisher.Substring(0, 1).ToLowerInvariant())/$publisher/$package" -Headers $headers | Out-Null
}
catch {
    if ($_.Exception.Response.StatusCode -ne [Net.HttpStatusCode]::NotFound) {
        throw
    }

    $listed = $false
}

if ($listed) {
    & $wingetcreate update $Id --version $Version --urls "$InstallerUrl|$Architecture" --submit --no-open
}
else {
    $owner = (Invoke-RestMethod https://api.github.com/user -Headers $headers).login
    $query = [uri]::EscapeDataString("repo:microsoft/winget-pkgs is:pr is:open author:$owner in:title `"New package: $Id version`"")
    $waiting = (Invoke-RestMethod "https://api.github.com/search/issues?q=$query" -Headers $headers).items |
        Where-Object { $_.title -like "New package: $Id version *" } |
        Select-Object -First 1

    if ($waiting) {
        Write-Output "::notice::$Id is still waiting for its first review in $($waiting.html_url), so no new pull request is opened. Update that one to $Version, installer $InstallerUrl."
        return
    }

    New-Manifests $manifests
    & $wingetcreate submit $manifests --prtitle "New package: $Id version $Version" --no-open
}
