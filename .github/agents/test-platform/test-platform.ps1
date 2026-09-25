#Requires -Version 7.4
<#
.SYNOPSIS
    The mechanical half of the test-platform skill (.github/agents/test-platform.md): which version every deployed app is
    at, rolling an All CD run's Android and Windows apps out onto this machine, running the E2E suite's stages, and
    calling https://bitplatform.dev/mcp.

.DESCRIPTION
    versions  Compares everything the CD workflows deploy with APP_VERSION: the IIS sites, the Windows apps, the Android
              apps, the version each Boilerplate web app shows in its nav panel, and the MCP endpoint's own version.
              Exits 1 when anything is behind.
    android   Installs the APKs an All CD run built onto the one connected device, booting the AVD when none is up.
    windows   Opens each Windows app that is behind and taps its version in the nav panel, which makes it update itself
              from its feed and restart.
    e2e       Runs Boilerplate.Tests.E2E stage by stage, as RunTests.bat does, keeping each stage's log and TRX.
    mcp       One call to https://bitplatform.dev/mcp: tools/list, or tools/call when -Tool is given.

    What is deployed where is read from .github/workflows/*.cd.yml and from the E2E suite's DeployedApps.cs and
    RunTests.bat rather than repeated here, so this script follows them.

.EXAMPLE
    pwsh .github/agents/test-platform/test-platform.ps1 versions -Sha 465ce237c
.EXAMPLE
    pwsh .github/agents/test-platform/test-platform.ps1 android -CdRun https://github.com/bitfoundation/bitplatform/actions/runs/36078311727
.EXAMPLE
    pwsh .github/agents/test-platform/test-platform.ps1 e2e -Stage web-firefox -Filter 'FullyQualifiedName~WebSmokeTests'
.EXAMPLE
    pwsh .github/agents/test-platform/test-platform.ps1 mcp -McpVersion 10.6.1 -Tool GetBitBlazorUIComponent -Arguments '{"name":"BitAccordion"}'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('versions', 'android', 'windows', 'e2e', 'mcp')]
    [string] $Command,

    # What everything must be at. The repository's APP_VERSION variable when omitted.
    [string] $Expected,

    # The CD run's head commit, full or abbreviated. When given, a ProductVersion's +<sha> must match it too.
    [string] $Sha,

    # android: the All CD run whose android-bundle artifacts get installed, as its id or any url of it.
    [string] $CdRun,

    # windows: taps the version of an app that is already current too, which then only checks its feed.
    [switch] $Force,

    # e2e: the stages to run, in RunTests.bat's order when omitted.
    [ValidateSet('web-chromium', 'web-webkit', 'web-firefox', 'android', 'windows', 'api')]
    [string[]] $Stage = @('web-chromium', 'web-webkit', 'web-firefox', 'android', 'windows', 'api'),

    # e2e: narrows every stage further, e.g. 'FullyQualifiedName~WebSmokeTests'.
    [string] $Filter,

    # e2e: a stage still running after this long is killed and reported as timed out.
    [int] $TimeoutMinutes = 120,

    # e2e: starts the stages without first checking that the global admin can sign in.
    [switch] $SkipPreflight,

    # mcp: the ?v= to ask for; omitted asks for the newest release the endpoint serves.
    [string] $McpVersion,

    # mcp: the tool to call; tools/list when omitted.
    [string] $Tool,

    # mcp: the tool's arguments, as json.
    [string] $Arguments = '{}'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$repository = 'bitfoundation/bitplatform'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$workflows = Get-ChildItem (Join-Path $repositoryRoot '.github\workflows') -Filter '*.cd.yml'
$e2eProject = Join-Path $repositoryRoot 'src\Templates\Boilerplate\Bit.Boilerplate\src\Internal\Boilerplate.Tests.E2E'
$deployedAppsFile = Join-Path $e2eProject 'Infrastructure\DeployedApps.cs'

#region What the CD workflows deploy

# Every deploy job hands .github/actions/deploy-to-server a site folder and the dll that proves its bundle is whole.
function Get-IisSites {
    foreach ($workflow in $workflows) {
        $content = Get-Content $workflow.FullName -Raw
        foreach ($match in [regex]::Matches($content, "site-path:\s*(?<path>\S+)\s+sentinel-file:\s*(?<dll>\S+)\s+health-check-url:\s*'(?<url>[^']+)'")) {
            [pscustomobject]@{ Path = $match.Groups['path'].Value; Dll = $match.Groups['dll'].Value; Url = $match.Groups['url'].Value }
        }
    }
}

# vpk pack -u names the Velopack app id, which is also the folder its setup installs into.
function Get-WindowsApps {
    $ids = foreach ($workflow in $workflows) {
        [regex]::Matches((Get-Content $workflow.FullName -Raw), 'vpk@[\d.]+ -- pack -u (?<id>\S+)') | ForEach-Object { $_.Groups['id'].Value }
    }

    foreach ($id in $ids | Sort-Object -Unique) {
        $root = Join-Path $env:LOCALAPPDATA $id
        [pscustomobject]@{ Id = $id; Root = $root; Exe = Join-Path $root "current\$id.exe" }
    }
}

# The workflows that upload an android-bundle artifact name their package in APP_BUNDLE_ID.
function Get-AndroidPackages {
    foreach ($workflow in $workflows) {
        $content = Get-Content $workflow.FullName -Raw

        if ($content -match 'name: android-bundle') {
            [regex]::Match($content, "APP_BUNDLE_ID:\s*'(?<id>[^']+)'").Groups['id'].Value
        }
    }
}

# The Boilerplate web apps, as the E2E suite addresses them in DeployedApps.AddressOf.
function Get-WebApps {
    $content = Get-Content $deployedAppsFile -Raw

    $addresses = @{}
    foreach ($match in [regex]::Matches($content, 'public const string (?<name>\w+) = "(?<url>https://[^"]+)";')) {
        $addresses[$match.Groups['name'].Value] = $match.Groups['url'].Value
    }

    $arms = [regex]::Match($content, 'AddressOf\(App app\) => app switch\s*\{(?<arms>[^}]*)\}').Groups['arms'].Value
    foreach ($match in [regex]::Matches($arms, 'App\.(?<app>\w+) => (?<name>\w+),')) {
        [pscustomobject]@{ App = $match.Groups['app'].Value; Url = $addresses[$match.Groups['name'].Value] }
    }
}

# The Windows apps built from the Boilerplate, which are the ones with a version button in their nav panel.
function Get-BoilerplateWindowsAppIds {
    [regex]::Matches((Get-Content $deployedAppsFile -Raw), 'public const string \w+WindowsAppId = "(?<id>[^"]+)";') | ForEach-Object { $_.Groups['id'].Value }
}

#endregion

#region Versions

function Get-ExpectedVersion {
    if ($Expected) {
        return $Expected
    }

    $value = (gh variable get APP_VERSION -R $repository) -join ''

    if ($LASTEXITCODE -ne 0 -or -not $value) {
        throw 'Could not read the APP_VERSION variable with gh. Pass -Expected instead.'
    }

    $value.Trim()
}

# 325.32.8, 325.32.8.0 (an assembly's four parts) and 325.32.8+<sha> (an informational version) all match 325.32.8.
function Test-Version([string] $actual) {
    if (-not $actual) {
        return $false
    }

    $version, $commit = $actual.Trim() -split '\+', 2

    if ($version -ne $script:expectedVersion -and $version -ne "$script:expectedVersion.0") {
        return $false
    }

    -not ($Sha -and $commit -and -not $commit.StartsWith($Sha, [StringComparison]::OrdinalIgnoreCase))
}

# Null while the file is missing - or mid-swap, when Velopack replaces the current folder under it.
function Get-ProductVersion([string] $path) {
    try {
        if (Test-Path $path) {
            (Get-Item $path).VersionInfo.ProductVersion
        }
    }
    catch {
    }
}

function New-VersionRow([string] $area, [string] $name, [string] $actual, [string] $missing = '(missing)') {
    [pscustomobject]@{
        OK = if (Test-Version $actual) { 'yes' } else { 'NO' }
        Area = $area
        Name = $name
        Actual = if ($actual) { $actual } else { $missing }
    }
}

#endregion

#region Android

function Initialize-Adb {
    $candidates = @(
        (Get-Command adb -ErrorAction SilentlyContinue | ForEach-Object { Split-Path (Split-Path $_.Source) }),
        $env:ANDROID_HOME,
        $env:ANDROID_SDK_ROOT,
        "${env:ProgramFiles(x86)}\Android\android-sdk",
        "$env:LOCALAPPDATA\Android\Sdk")

    $script:androidSdk = $candidates | Where-Object { $_ -and (Test-Path (Join-Path $_ 'platform-tools\adb.exe')) } | Select-Object -First 1

    if (-not $script:androidSdk) {
        throw 'No Android SDK with platform-tools was found on PATH, in ANDROID_HOME/ANDROID_SDK_ROOT, or where Visual Studio and Android Studio install it.'
    }

    $script:adb = Join-Path $script:androidSdk 'platform-tools\adb.exe'
}

function Invoke-Adb {
    (& $script:adb @args 2>&1 | ForEach-Object { "$_" }) -join "`n"
}

function Get-AndroidDevices {
    @((Invoke-Adb devices) -split "`n" | Select-Object -Skip 1 | Where-Object { $_ -match '^\S+\s+device\b' })
}

# Exactly one device, since adb and the E2E suite both address "the" device. With none, the first AVD is booted.
function Initialize-AndroidDevice {
    $devices = Get-AndroidDevices

    if ($devices.Count -gt 1) {
        throw "More than one Android device is connected, so adb would not know which one to use:`n$($devices -join "`n")"
    }

    if ($devices.Count -eq 1) {
        return
    }

    $emulator = Join-Path $script:androidSdk 'emulator\emulator.exe'

    # Newer emulators mix INFO lines into -list-avds, while an AVD name has no spaces.
    $avd = (& $emulator -list-avds 2>$null) | Where-Object { $_ -and $_ -notmatch '\s' } | Select-Object -First 1

    if (-not $avd) {
        throw "No Android device is connected and $emulator lists no AVD to boot."
    }

    Write-Host "Booting the $avd emulator..."
    Start-Process $emulator -ArgumentList '-avd', $avd | Out-Null

    $deadline = (Get-Date).AddMinutes(5)

    while ((Get-Date) -lt $deadline) {
        if ((Get-AndroidDevices).Count -eq 1 -and (Invoke-Adb shell getprop sys.boot_completed).Trim() -eq '1') {
            return
        }

        Start-Sleep -Seconds 3
    }

    throw "The $avd emulator did not finish booting within 5 minutes."
}

function Get-AndroidVersion([string] $package) {
    $match = [regex]::Match((Invoke-Adb shell dumpsys package $package), 'versionName=(\S+)')

    if ($match.Success) {
        $match.Groups[1].Value
    }
}

function Install-AndroidApps {
    if ($CdRun -match 'runs/(\d+)') {
        $runId = $Matches[1]
    }
    elseif ($CdRun -match '^\d+$') {
        $runId = $CdRun
    }
    else {
        throw 'Pass the All CD run with -CdRun, as its id or any url of it.'
    }

    Initialize-Adb
    Initialize-AndroidDevice

    $aapt = Get-ChildItem (Join-Path $script:androidSdk 'build-tools') -Filter aapt2.exe -Recurse | Sort-Object FullName -Descending | Select-Object -First 1

    $work = Join-Path ([IO.Path]::GetTempPath()) "test-platform-apks-$runId"
    New-Item -ItemType Directory -Force $work | Out-Null

    $token = (gh auth token) -join ''

    # Every Android job uploads an artifact called android-bundle and a re-run job adds one more, so they are told apart by
    # the package inside, and only the newest upload of each package counts.
    $artifacts = gh api "repos/$repository/actions/runs/$runId/artifacts" --paginate --jq '.artifacts[] | select(.name == "android-bundle" and (.expired | not)) | "\(.id) \(.created_at)"'

    $apks = foreach ($artifact in $artifacts) {
        $id, $createdAt = $artifact -split ' '
        $folder = Join-Path $work $id

        if (-not (Test-Path $folder)) {
            Write-Host "Downloading artifact $id..."
            Invoke-WebRequest "https://api.github.com/repos/$repository/actions/artifacts/$id/zip" -Headers @{ Authorization = "Bearer $token" } -OutFile "$folder.zip"
            Expand-Archive "$folder.zip" $folder -Force
            Remove-Item "$folder.zip"
        }

        foreach ($apk in Get-ChildItem $folder -Filter *.apk -Recurse) {
            $badging = [regex]::Match(((& $aapt.FullName dump badging $apk.FullName 2>$null) -join "`n"), "package: name='(?<package>[^']+)' versionCode='\d+' versionName='(?<version>[^']+)'")

            [pscustomobject]@{ Package = $badging.Groups['package'].Value; Version = $badging.Groups['version'].Value; CreatedAt = [datetimeoffset] $createdAt; Path = $apk.FullName }
        }
    }

    if (-not $apks) {
        throw "Run $runId has no android-bundle artifact that has not expired."
    }

    foreach ($apk in $apks | Group-Object Package | ForEach-Object { $_.Group | Sort-Object CreatedAt -Descending | Select-Object -First 1 }) {
        Write-Host "Installing $($apk.Package) $($apk.Version)..."

        $output = Invoke-Adb install -r -d $apk.Path

        # A release build cannot replace one signed with another key, nor - on a user build - a newer one.
        if ($output -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE|INSTALL_FAILED_VERSION_DOWNGRADE') {
            Write-Host "  $($Matches[0]): uninstalling it first, which drops its data."
            Invoke-Adb uninstall $apk.Package | Out-Null
            $output = Invoke-Adb install $apk.Path
        }

        if ($output -notmatch '(?m)^Success') {
            throw "Installing $($apk.Path) failed:`n$output"
        }
    }

    $failed = $false

    foreach ($package in Get-AndroidPackages) {
        $installed = Get-AndroidVersion $package
        $failed = $failed -or -not (Test-Version $installed)
        Write-Host ("{0}: {1}" -f $package, $(if ($installed) { $installed } else { 'not installed' }))
    }

    if ($failed) {
        exit 1
    }
}

#endregion

#region Chrome DevTools Protocol

function Invoke-Cdp([string] $webSocketUrl, [string] $method, [hashtable] $parameters = @{}) {
    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    $cancellation = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(30))

    try {
        $socket.ConnectAsync([uri] $webSocketUrl, $cancellation.Token).GetAwaiter().GetResult()

        $request = [Text.Encoding]::UTF8.GetBytes((@{ id = 1; method = $method; params = $parameters } | ConvertTo-Json -Depth 10 -Compress))
        $socket.SendAsync([ArraySegment[byte]]::new($request), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $cancellation.Token).GetAwaiter().GetResult()

        $buffer = [byte[]]::new(64KB)

        while ($true) {
            $message = [IO.MemoryStream]::new()

            do {
                $received = $socket.ReceiveAsync([ArraySegment[byte]]::new($buffer), $cancellation.Token).GetAwaiter().GetResult()
                $message.Write($buffer, 0, $received.Count)
            } while (-not $received.EndOfMessage)

            $response = [Text.Encoding]::UTF8.GetString($message.ToArray()) | ConvertFrom-Json -Depth 64

            # Events carry no id; the answer to this call is the one with id 1.
            if ($response.id -eq 1) {
                return $response
            }
        }
    }
    finally {
        $socket.Dispose()
        $cancellation.Dispose()
    }
}

function Get-CdpPage([int] $port, [int] $timeoutSeconds = 90) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            # Parenthesized, since Invoke-RestMethod hands a json array down the pipeline as one object.
            $page = (Invoke-RestMethod "http://localhost:$port/json/list" -TimeoutSec 5) | Where-Object type -eq 'page' | Select-Object -First 1

            if ($page) {
                return $page
            }
        }
        catch {
        }

        Start-Sleep -Seconds 1
    }

    throw "Nothing on localhost:$port exposed a page over CDP within $timeoutSeconds seconds."
}

# The version the page shows in its nav panel, once it shows one. A navigation that replaces the page's context in the
# meantime only fails one evaluation, so the question is simply asked again.
function Wait-AppVersion([string] $webSocketUrl, [int] $timeoutSeconds = 120) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $value = (Invoke-Cdp $webSocketUrl 'Runtime.evaluate' @{ expression = "document.querySelector('.app-version')?.innerText?.trim() ?? ''"; returnByValue = $true }).result.result.value

            if ($value) {
                return $value
            }
        }
        catch {
        }

        Start-Sleep -Seconds 1
    }
}

# A headless Edge of its own - nothing cached, no service worker from an earlier visit - reads each web app's version.
function Get-WebAppVersions {
    $edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe" | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $edge) {
        throw 'Microsoft Edge is not installed, and it is what reads the web apps'' versions.'
    }

    $port = 9333
    $userDataDir = Join-Path ([IO.Path]::GetTempPath()) "test-platform-edge-$PID"
    $browser = Start-Process $edge -ArgumentList '--headless=new', "--remote-debugging-port=$port", "--user-data-dir=`"$userDataDir`"", '--window-size=1440,900', '--no-first-run', 'about:blank' -PassThru

    try {
        $endpoint = $null
        $deadline = (Get-Date).AddSeconds(30)

        while (-not $endpoint -and (Get-Date) -lt $deadline) {
            try {
                $endpoint = Invoke-RestMethod "http://localhost:$port/json/version" -TimeoutSec 5
            }
            catch {
                Start-Sleep -Seconds 1
            }
        }

        if (-not $endpoint) {
            throw "Headless Edge did not open its CDP endpoint on port $port."
        }

        foreach ($app in Get-WebApps) {
            $target = (Invoke-Cdp $endpoint.webSocketDebuggerUrl 'Target.createTarget' @{ url = $app.Url }).result.targetId
            $shown = Wait-AppVersion "ws://localhost:$port/devtools/page/$target"
            Invoke-Cdp $endpoint.webSocketDebuggerUrl 'Target.closeTarget' @{ targetId = $target } | Out-Null

            [pscustomobject]@{ App = $app.App; Url = $app.Url; Version = $shown }
        }
    }
    finally {
        taskkill /PID $browser.Id /T /F 2>&1 | Out-Null
        Remove-Item $userDataDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

#endregion

#region Windows

# Every Client.Windows app starts WebView2 with --remote-debugging-port=9222, so only one of them may run while one is
# driven over CDP - a leftover instance would be the one answering. /T takes its WebView2 processes along.
function Stop-WindowsApps {
    foreach ($process in Get-Process | Where-Object { $_.ProcessName -like '*.Client.Windows' }) {
        taskkill /PID $process.Id /T /F 2>&1 | Out-Null
    }
}

function Update-WindowsApps {
    $boilerplateApps = Get-BoilerplateWindowsAppIds
    $failed = $false

    foreach ($app in Get-WindowsApps) {
        if (-not (Test-Path $app.Exe)) {
            Write-Host "$($app.Id): not installed. Install it from its feed's Setup.exe first."
            $failed = $true
            continue
        }

        $before = Get-ProductVersion $app.Exe

        if ((Test-Version $before) -and -not $Force) {
            Write-Host "$($app.Id): already at $before."
            continue
        }

        Write-Host "$($app.Id): at $before, opening it..."

        Stop-WindowsApps
        Start-Process $app.Exe -WindowStyle Minimized

        if ($app.Id -in $boilerplateApps) {
            # The version button runs ForceUpdate: Velopack downloads the release and restarts the app into it.
            $page = Get-CdpPage 9222
            $shown = Wait-AppVersion $page.webSocketDebuggerUrl

            if (-not $shown) {
                throw "$($app.Id) never showed its version in the nav panel."
            }

            Write-Host "  its nav panel shows $shown, tapping it."
            Invoke-Cdp $page.webSocketDebuggerUrl 'Runtime.evaluate' @{ expression = "document.querySelector('.app-version').click()" } | Out-Null
        }
        else {
            # No version button: the app downloads a release at startup, and Velopack applies a downloaded one on the next start.
            $deadline = (Get-Date).AddMinutes(10)

            while (-not (Get-ChildItem (Join-Path $app.Root 'packages') -Filter "*-$script:expectedVersion-full.nupkg" -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) {
                Start-Sleep -Seconds 5
            }

            Stop-WindowsApps
            Start-Process $app.Exe -WindowStyle Minimized
        }

        $deadline = (Get-Date).AddMinutes(10)

        while (-not (Test-Version (Get-ProductVersion $app.Exe)) -and (Get-Date) -lt $deadline) {
            Start-Sleep -Seconds 5
        }

        Stop-WindowsApps

        $after = Get-ProductVersion $app.Exe

        if (Test-Version $after) {
            Write-Host "$($app.Id): now at $after."
        }
        else {
            Write-Host "$($app.Id): still at $after after 10 minutes."
            $failed = $true
        }
    }

    if ($failed) {
        exit 1
    }
}

#endregion

#region E2E

# A remote Playwright server refuses a client of another version, and then every test of the stage fails at connect.
# Its WebSocket upgrade already says so: 428 with the two versions, instead of 101.
function Test-RemotePlaywright([string] $endpoint) {
    $packages = Get-Content (Join-Path $repositoryRoot 'src\Templates\Boilerplate\Bit.Boilerplate\src\Directory.Packages.props') -Raw
    $version = [regex]::Match($packages, 'Include="Microsoft\.Playwright\.MSTest\.v4" Version="(?<version>[^"]+)"').Groups['version'].Value

    $client = [Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(10)

    try {
        $request = [Net.Http.HttpRequestMessage]::new('GET', ($endpoint -replace '^ws', 'http'))
        $request.Headers.Connection.Add('Upgrade')
        $request.Headers.Upgrade.ParseAdd('websocket')
        $request.Headers.Add('Sec-WebSocket-Version', '13')
        $request.Headers.Add('Sec-WebSocket-Key', [Convert]::ToBase64String([Guid]::NewGuid().ToByteArray()))
        $request.Headers.TryAddWithoutValidation('User-Agent', "Playwright/$version (x64; windows 10.0) CSharp/11.0") | Out-Null

        $response = $client.Send($request, [Net.Http.HttpCompletionOption]::ResponseHeadersRead)

        if ([int] $response.StatusCode -eq 101) {
            return $null
        }

        $banner = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() -split "`n" | Where-Object { $_ -match 'server version' } | ForEach-Object { ($_ -replace '[║\s-]+', ' ').Trim() }

        "the Playwright server at $endpoint refuses a $version client ($([int] $response.StatusCode)$(if ($banner) { ", $banner" })). Restart it with the command in .runsettings."
    }
    catch {
        "nothing answers at $endpoint. Start the Playwright server on that machine with the command in .runsettings."
    }
    finally {
        $client.Dispose()
    }
}

function Invoke-E2EStages {
    $runTests = Get-Content (Join-Path $e2eProject 'RunTests.bat') -Raw
    $remoteEndpoint = [regex]::Match($runTests, 'set PLAYWRIGHT_SERVER_ENDPOINT=(?<endpoint>\S+)').Groups['endpoint'].Value

    $stages = @{
        'web-chromium' = @{ Category = 'Web'; Remote = $true; Browser = $null }
        'web-webkit' = @{ Category = 'Web'; Remote = $true; Browser = 'webkit' }
        'web-firefox' = @{ Category = 'Web'; Remote = $false; Browser = 'firefox' }
        'android' = @{ Category = 'Android'; Remote = $false; Browser = $null }
        'windows' = @{ Category = 'Windows'; Remote = $false; Browser = $null }
        'api' = @{ Category = 'Api'; Remote = $false; Browser = $null }
    }

    Push-Location $e2eProject

    try {
        $results = Join-Path $e2eProject 'TestResults'
        New-Item -ItemType Directory -Force $results | Out-Null

        # Into a file: a warm build's warnings run to hundreds of lines.
        Write-Host 'Building Boilerplate.Tests.E2E...'
        dotnet build --nologo -v q > (Join-Path $results 'build.log')

        if ($LASTEXITCODE -ne 0) {
            Select-String -Path (Join-Path $results 'build.log') -Pattern ': error ' | ForEach-Object Line | Select-Object -Unique | Write-Host
            throw 'Boilerplate.Tests.E2E does not build.'
        }

        # One sign-in first. Every test that needs the global admin signs in again after a failed attempt, and a wrong
        # password repeated that often locks the live account out for everyone.
        if (-not $SkipPreflight) {
            Write-Host 'Pre-flight: signing the global admin in once...'
            dotnet test --no-build --filter 'FullyQualifiedName~GlobalAdminTwoFactorCodeTests' > (Join-Path $results 'preflight.log')

            if ($LASTEXITCODE -ne 0) {
                Select-String -Path (Join-Path $results 'preflight.log') -Pattern 'Exception: |Assert' | Select-Object -First 3 | ForEach-Object { $_.Line.Trim() } | Write-Host
                throw 'The global admin cannot sign in with this project''s user secrets, so no stage was started. Fix GlobalAdminEmail, GlobalAdminPassword or GlobalAdminAuthenticatorKey first.'
            }
        }

        $summary = foreach ($name in $Stage) {
            $settings = $stages[$name]

            if ($settings.Remote) {
                $problem = Test-RemotePlaywright $remoteEndpoint

                if ($problem) {
                    Write-Host "$name skipped: $problem"
                    [pscustomobject]@{ Stage = $name; Outcome = "skipped: $problem"; Log = $null }
                    continue
                }
            }

            if ($settings.Browser -eq 'firefox') {
                # Idempotent: downloads the Firefox build this Playwright version drives only when it is missing.
                # Searched for rather than spelled out, since the target framework moves with every .NET upgrade.
                $playwrightScript = Get-ChildItem (Join-Path $e2eProject 'bin\Debug') -Filter playwright.ps1 -Recurse | Select-Object -First 1
                pwsh -NoProfile $playwrightScript.FullName install firefox | Out-Host
            }

            $env:PLAYWRIGHT_SERVER_ENDPOINT = if ($settings.Remote) { $remoteEndpoint } else { $null }
            $env:BROWSER = $settings.Browser

            # The suite runs adb by name.
            if ($settings.Category -eq 'Android') {
                Initialize-Adb
                $env:ANDROID_HOME = $script:androidSdk
                $env:PATH = "$(Join-Path $script:androidSdk 'platform-tools');$env:PATH"
            }

            $testFilter = "TestCategory=$($settings.Category)" + $(if ($Filter) { "&$Filter" })

            # Timestamped, so re-running one failed test keeps the full run's log and TRX next to it.
            $runName = '{0}-{1:yyyyMMdd-HHmmss}' -f $name, (Get-Date)
            $log = Join-Path $results "$runName.log"

            Write-Host "$name started at $(Get-Date -Format 'HH:mm:ss'), log: $log"

            $process = Start-Process dotnet -ArgumentList 'test', '--no-build', '--filter', "`"$testFilter`"" -NoNewWindow -PassThru -RedirectStandardOutput $log -RedirectStandardError "$log.err"

            if (-not $process.WaitForExit([TimeSpan]::FromMinutes($TimeoutMinutes))) {
                taskkill /PID $process.Id /T /F 2>&1 | Out-Null
                $outcome = "timed out after $TimeoutMinutes minutes"
            }
            else {
                $outcome = switch ($process.ExitCode) { 0 { 'passed' } 2 { 'failed' } 8 { 'no test matched' } default { "exited with $_" } }
            }

            # Every stage writes TestResults.trx, so each one's is kept under its own name.
            $trx = Join-Path $results 'TestResults.trx'

            if (Test-Path $trx) {
                Move-Item $trx (Join-Path $results "$runName.trx") -Force
            }

            $counts = (Select-String -Path $log -Pattern '^\s*(total|failed|succeeded|skipped): \d+' | ForEach-Object { $_.Line.Trim() }) -join ', '
            Write-Host "$name $outcome at $(Get-Date -Format 'HH:mm:ss'). $counts"

            [pscustomobject]@{ Stage = $name; Outcome = $outcome; Counts = $counts; Log = $log }
        }

        $summary | Format-Table -AutoSize -Wrap | Out-String -Width 220 | Write-Host

        if ($summary | Where-Object Outcome -ne 'passed') {
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

#endregion

#region MCP

function Invoke-BitMcp([string] $version, [string] $method, [string] $parameters = '{}') {
    $endpoint = 'https://bitplatform.dev/mcp' + $(if ($version) { "?v=$version" })
    $headers = @{ Accept = 'application/json, text/event-stream' }

    $initialize = Invoke-WebRequest $endpoint -Method Post -ContentType 'application/json' -Headers $headers -TimeoutSec 120 `
        -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test-platform","version":"1.0"}}}'

    $headers['MCP-Protocol-Version'] = '2025-06-18'
    $session = $initialize.Headers['Mcp-Session-Id'] | Select-Object -First 1

    if ($session) {
        $headers['Mcp-Session-Id'] = $session
    }

    try {
        Invoke-WebRequest $endpoint -Method Post -ContentType 'application/json' -Headers $headers -TimeoutSec 60 -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' | Out-Null

        $answer = Invoke-WebRequest $endpoint -Method Post -ContentType 'application/json' -Headers $headers -TimeoutSec 600 `
            -Body "{`"jsonrpc`":`"2.0`",`"id`":2,`"method`":`"$method`",`"params`":$parameters}"

        [pscustomobject]@{ Server = (Read-JsonRpc $initialize).result.serverInfo; Response = Read-JsonRpc $answer }
    }
    finally {
        if ($session) {
            try { Invoke-WebRequest $endpoint -Method Delete -Headers $headers -TimeoutSec 30 | Out-Null } catch { }
        }
    }
}

# Streamable HTTP answers with plain json, or with an event stream whose data lines carry it.
function Read-JsonRpc($response) {
    $content = if ($response.Content -is [byte[]]) { [Text.Encoding]::UTF8.GetString($response.Content) } else { $response.Content }
    $data = ($content -split "`r?`n" | Where-Object { $_.StartsWith('data: ') } | ForEach-Object { $_.Substring(6) }) -join ''

    $(if ($data) { $data } else { $content }) | ConvertFrom-Json -Depth 64
}

#endregion

switch ($Command) {
    'versions' {
        $script:expectedVersion = Get-ExpectedVersion
        Write-Host "Expected: $script:expectedVersion$(if ($Sha) { "+$Sha" })"

        $rows = [Collections.Generic.List[object]]::new()

        foreach ($site in Get-IisSites) {
            $rows.Add((New-VersionRow 'IIS' "$(Split-Path $site.Path -Leaf) $($site.Url)" (Get-ProductVersion (Join-Path $site.Path $site.Dll))))
        }

        foreach ($app in Get-WindowsApps) {
            $rows.Add((New-VersionRow 'Windows' $app.Id (Get-ProductVersion $app.Exe) '(not installed)'))
        }

        Initialize-Adb
        $devices = Get-AndroidDevices

        foreach ($package in Get-AndroidPackages) {
            if ($devices.Count -eq 1) {
                $rows.Add((New-VersionRow 'Android' $package (Get-AndroidVersion $package) '(not installed)'))
            }
            else {
                $rows.Add((New-VersionRow 'Android' $package $null "($($devices.Count) devices connected, expected 1)"))
            }
        }

        foreach ($app in Get-WebAppVersions) {
            $rows.Add((New-VersionRow 'Web' "$($app.App) $($app.Url)" $app.Version '(no version in the nav panel within 2 minutes)'))
        }

        $mcp = Invoke-BitMcp '' 'tools/list'
        $rows.Add((New-VersionRow 'MCP' "bitplatform.dev/mcp, $($mcp.Response.result.tools.Count) tools" $mcp.Server.version))

        $rows | Format-Table -AutoSize -Wrap | Out-String -Width 220 | Write-Host

        if ($rows | Where-Object OK -ne 'yes') {
            exit 1
        }
    }

    'android' {
        $script:expectedVersion = Get-ExpectedVersion
        Install-AndroidApps
    }

    'windows' {
        $script:expectedVersion = Get-ExpectedVersion
        Update-WindowsApps
    }

    'e2e' {
        Invoke-E2EStages
    }

    'mcp' {
        if ($Tool) {
            $call = Invoke-BitMcp $McpVersion 'tools/call' (@{ name = $Tool; arguments = ($Arguments | ConvertFrom-Json -AsHashtable) } | ConvertTo-Json -Depth 20 -Compress)
        }
        else {
            $call = Invoke-BitMcp $McpVersion 'tools/list'
        }

        Write-Host "$($call.Server.name) $($call.Server.version), ?v=$McpVersion"

        if ($call.Response.error) {
            Write-Host "JSON-RPC error: $($call.Response.error | ConvertTo-Json -Compress)"
            exit 1
        }

        if ($Tool) {
            if ($call.Response.result.isError) {
                Write-Host 'The tool reported an error:'
            }

            $call.Response.result.content | Where-Object type -eq 'text' | ForEach-Object text

            if ($call.Response.result.isError) {
                exit 1
            }
        }
        else {
            Write-Host "$($call.Response.result.tools.Count) tools:"
            $call.Response.result.tools | ForEach-Object name
        }
    }
}
