#Requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)][ValidateSet('scan', 'bump')][string]$Command,
    [string]$To,
    [string]$Repo,
    [switch]$Save,
    [switch]$DryRun,
    [switch]$AcceptSuspects
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)

if (-not $Repo) { $Repo = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim() }
$Repo = (Resolve-Path $Repo).Path
$rulesFile = Join-Path $PSScriptRoot 'rules.json'
$reviewedFile = Join-Path $PSScriptRoot 'reviewed.json'
$locationsFile = Join-Path $PSScriptRoot 'locations.json'
$utf8Strict = [Text.UTF8Encoding]::new($false, $true)
$utf8 = [Text.UTF8Encoding]::new($false)
$hint = [regex]'bitplatform|bitfoundation|\bBit\.[A-Z]|\bbit\b|\bbit-|\bBit [A-Z]'

function Get-VersionKey([string]$v) {
    if ($v -notmatch '^(\d+)\.(\d+)\.(\d+)(?:-pre-(\d+))?$') { throw "'$v' is not a bit version such as 1.2.0 or 1.2.0-pre-03" }
    $pre = if ($Matches[4]) { [int]$Matches[4] } else { 999999 }
    [version]::new([int]$Matches[1], [int]$Matches[2], [int]$Matches[3], $pre)
}

function Get-Core([string]$v) { $v.Split('-')[0] }

function Get-Token([string]$v) { '(?<![\w.])' + [regex]::Escape($v) + '(?![\w]|[.-]\w)' }

function Get-Clean([string]$s) { $s.Trim().TrimStart([char]0xFEFF).Trim() }

function Get-ReleaseVersion {
    $text = [IO.File]::ReadAllText((Join-Path $Repo 'src/Bit.Build.props'))
    if ($text -notmatch '<ReleaseVersion>([^<]+)</ReleaseVersion>') { throw 'ReleaseVersion not found in src/Bit.Build.props' }
    $Matches[1].Trim()
}

function Get-Rules {
    $rules = Get-Content $rulesFile -Raw | ConvertFrom-Json
    $ids = @{}
    foreach ($r in $rules) {
        $placeholder = switch ($r.kind) { 'full' { '{V}' } 'kept' { '{V}' } 'core' { '{CORE}' } default { throw "rule $($r.id): kind must be full, core or kept" } }
        $parts = $r.regex -split [regex]::Escape($placeholder)
        if ($parts.Count -ne 2) { throw "rule $($r.id): the regex must hold $placeholder exactly once" }
        if ($ids.ContainsKey($r.id)) { throw "duplicate rule id $($r.id)" }
        $ids[$r.id] = $true
        $globs = foreach ($g in $r.files) { [regex]::new('^' + ([regex]::Escape($g) -replace '\\\*', '.*' -replace '\\\?', '.') + '$') }
        $r | Add-Member -NotePropertyName Before -NotePropertyValue $parts[0]
        $r | Add-Member -NotePropertyName After -NotePropertyValue $parts[1]
        $r | Add-Member -NotePropertyName Globs -NotePropertyValue @($globs)
    }
    $rules
}

function Get-Reviewed {
    $set = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    if (Test-Path $reviewedFile) {
        foreach ($e in (Get-Content $reviewedFile -Raw | ConvertFrom-Json)) { $null = $set.Add("$($e.file)`n$(Get-Clean $e.line)") }
    }
    , $set
}

function Invoke-Scan([string]$full) {
    $core = Get-Core $full
    $isPre = $full.Contains('-pre-')
    $compiled = foreach ($r in $rules) {
        $v = if ($r.kind -eq 'core') { $core } else { $full }
        [pscustomobject]@{ Rule = $r; Regex = [regex]::new($r.Before + '(?<v>' + (Get-Token $v) + ')' + $r.After) }
    }
    $fullRe = [regex]::new((Get-Token $full))
    $bareRe = if ($isPre) { [regex]::new((Get-Token $core)) } else { $fullRe }
    $scan = [pscustomobject]@{
        Full = $full; Core = $core; IsPre = $isPre
        Hits = [Collections.Generic.List[object]]::new()
        Kept = [Collections.Generic.List[object]]::new()
        Uncovered = [Collections.Generic.List[object]]::new()
        Suspects = [Collections.Generic.List[object]]::new()
        Conflicts = [Collections.Generic.List[string]]::new()
        Undecodable = [Collections.Generic.List[string]]::new()
        Texts = @{}
    }
    $self = [IO.Path]::GetRelativePath($Repo, $PSScriptRoot).Replace('\', '/') + '/'
    $found = @((git -C $Repo grep -I -l -z -F -e $core) -split "`0") + @((git -C $Repo grep --untracked -I -l -z -F -e $core) -split "`0")
    $paths = @($found | Where-Object { $_ -and -not $_.StartsWith($self, [StringComparison]::Ordinal) } | Select-Object -Unique)
    foreach ($path in $paths) {
        $bytes = [IO.File]::ReadAllBytes((Join-Path $Repo $path))
        try { $text = $utf8Strict.GetString($bytes) }
        catch { if ($fullRe.IsMatch([Text.Encoding]::Latin1.GetString($bytes))) { $scan.Undecodable.Add($path) }; continue }
        $scan.Texts[$path] = $text
        $applicable = @($compiled | Where-Object { $p = $path; @($_.Rule.Globs | Where-Object { $_.IsMatch($p) }).Count })
        $lines = $text -split '(?<=\n)'
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $body = $lines[$i].TrimEnd("`r", "`n")
            if (-not $body.Contains($core)) { continue }
            $spans = [ordered]@{}
            foreach ($c in $applicable) {
                foreach ($m in $c.Regex.Matches($body)) {
                    $g = $m.Groups['v']
                    $key = "$($g.Index):$($g.Length)"
                    if ($spans.Contains($key)) {
                        if ($spans[$key].Kind -ne $c.Rule.kind) { $scan.Conflicts.Add("${path}:$($i + 1): rules $($spans[$key].Rule) and $($c.Rule.id) disagree on the kind") }
                        continue
                    }
                    $spans[$key] = [pscustomobject]@{ Start = $g.Index; End = $g.Index + $g.Length; Rule = $c.Rule.id; Kind = $c.Rule.kind }
                }
            }
            foreach ($s in $spans.Values) {
                $hit = [pscustomobject]@{ File = $path; Line = $i + 1; Start = $s.Start; End = $s.End; Rule = $s.Rule; Kind = $s.Kind; Text = $body }
                if ($s.Kind -eq 'kept') { $scan.Kept.Add($hit) } else { $scan.Hits.Add($hit) }
            }
            if ($isPre) {
                foreach ($m in $fullRe.Matches($body)) {
                    if (-not $spans.Contains("$($m.Index):$($m.Length)")) { $scan.Uncovered.Add([pscustomobject]@{ File = $path; Line = $i + 1; Text = (Get-Clean $body) }) }
                }
            }
            $loose = @($bareRe.Matches($body) | Where-Object { -not $spans.Contains("$($_.Index):$($_.Length)") })
            if ($loose.Count -and $hint.IsMatch($body) -and -not $reviewed.Contains("$path`n$(Get-Clean $body)")) {
                $scan.Suspects.Add([pscustomobject]@{ File = $path; Line = $i + 1; Text = (Get-Clean $body) })
            }
        }
    }
    $scan
}

function Get-Locations($scan) {
    $byFile = @{}
    foreach ($group in ($scan.Hits | Group-Object { "$($_.File)`n$($_.Line)" })) {
        $first = $group.Group[0]
        $text = $first.Text
        foreach ($h in ($group.Group | Sort-Object Start -Descending)) {
            $text = $text.Substring(0, $h.Start) + $(if ($h.Kind -eq 'full') { '{V}' } else { '{CORE}' }) + $text.Substring($h.End)
        }
        if (-not $byFile.ContainsKey($first.File)) { $byFile[$first.File] = [Collections.Generic.List[object]]::new() }
        $byFile[$first.File].Add([pscustomobject]@{ Line = $first.Line; Pattern = (Get-Clean $text) })
    }
    $files = [string[]]@($byFile.Keys)
    [Array]::Sort($files, [StringComparer]::Ordinal)
    $locations = [ordered]@{}
    foreach ($f in $files) { $locations[$f] = @($byFile[$f] | Sort-Object Line | ForEach-Object Pattern) }
    $locations
}

function Read-Locations {
    if (-not (Test-Path $locationsFile)) { return $null }
    $locations = [ordered]@{}
    foreach ($p in (Get-Content $locationsFile -Raw | ConvertFrom-Json).PSObject.Properties) { $locations[$p.Name] = @($p.Value) }
    $locations
}

function Save-Locations($locations) {
    [IO.File]::WriteAllText($locationsFile, ($locations | ConvertTo-Json -Depth 5) + [Environment]::NewLine, $utf8)
}

function Get-Bag($locations) {
    $bag = [Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)
    foreach ($f in $locations.Keys) { foreach ($p in $locations[$f]) { $k = "$f`n$p"; $bag[$k] = 1 + $(if ($bag.ContainsKey($k)) { $bag[$k] } else { 0 }) } }
    $bag
}

function Compare-Locations($old, $new) {
    if ($null -eq $old) { return 'no locations.json yet to compare with' }
    $oldBag = Get-Bag $old
    $newBag = Get-Bag $new
    $out = [Collections.Generic.List[string]]::new()
    $out.Add("compared with locations.json ($($old.Count) files):")
    foreach ($f in $new.Keys) { if (-not $old.Contains($f)) { $out.Add("  + new file   $f") } }
    foreach ($f in $old.Keys) { if (-not $new.Contains($f)) { $out.Add("  - gone file  $f") } }
    foreach ($k in $newBag.Keys) {
        $f, $p = $k -split "`n", 2
        $n = $newBag[$k] - $(if ($oldBag.ContainsKey($k)) { $oldBag[$k] } else { 0 })
        if ($n -gt 0 -and $old.Contains($f)) { $out.Add("  + new line   ${f}: $p") }
    }
    foreach ($k in $oldBag.Keys) {
        $f, $p = $k -split "`n", 2
        $n = $oldBag[$k] - $(if ($newBag.ContainsKey($k)) { $newBag[$k] } else { 0 })
        if ($n -gt 0 -and $new.Contains($f)) { $out.Add("  - gone line  ${f}: $p") }
    }
    if ($out.Count -eq 1) { $out.Add('  same files, same lines') }
    $out.ToArray()
}

function Write-Report($scan) {
    $ok = $true
    $files = @($scan.Hits | ForEach-Object File | Select-Object -Unique).Count
    $full = @($scan.Hits | Where-Object Kind -EQ 'full').Count
    Write-Host "version $($scan.Full) (core $($scan.Core)): $($scan.Hits.Count) spans in $files files ($full full, $($scan.Hits.Count - $full) core), $($scan.Kept.Count) kept as written"
    foreach ($r in $rules) {
        $n = @(@($scan.Hits) + @($scan.Kept) | Where-Object Rule -EQ $r.id).Count
        Write-Host ('  {0,4}  {1}{2}' -f $n, $r.id, $(if ($r.kind -eq 'kept') { '   (kept)' } elseif ($n) { '' } else { '   <- matches nothing' }))
    }
    if ($scan.Conflicts.Count) {
        $ok = $false
        Write-Host "`nCONFLICTS (fix rules.json):"
        $scan.Conflicts | ForEach-Object { Write-Host "  $_" }
    }
    if ($scan.Undecodable.Count) {
        $ok = $false
        Write-Host "`nNOT UTF-8 but holds the version (handle by hand):"
        $scan.Undecodable | ForEach-Object { Write-Host "  $_" }
    }
    if ($scan.Uncovered.Count) {
        $ok = $false
        Write-Host "`nUNCOVERED: $($scan.Uncovered.Count) occurrences of $($scan.Full) that no rule matches. Add a narrow rule to rules.json, of kind kept when the line records the version on purpose:"
        $scan.Uncovered | ForEach-Object { Write-Host "  $($_.File):$($_.Line): $($_.Text)" }
    }
    if ($scan.Suspects.Count) {
        $bare = if ($scan.IsPre) { $scan.Core } else { $scan.Full }
        Write-Host "`nSUSPECTS: lines with a bare $bare next to a bit hint that no rule matches."
        Write-Host '  Ours: add a rule to rules.json. Not ours: add the line to reviewed.json.'
        $scan.Suspects | ForEach-Object { Write-Host "  $($_.File):$($_.Line): $($_.Text)" }
    }
    $ok
}

function Invoke-Apply($scan, [string]$newFull) {
    $newCore = Get-Core $newFull
    $changed = 0
    foreach ($group in ($scan.Hits | Group-Object File)) {
        $text = $scan.Texts[$group.Name]
        $lines = $text -split '(?<=\n)'
        foreach ($h in ($group.Group | Sort-Object Line, @{ Expression = 'Start'; Descending = $true })) {
            $line = $lines[$h.Line - 1]
            $lines[$h.Line - 1] = $line.Substring(0, $h.Start) + $(if ($h.Kind -eq 'full') { $newFull } else { $newCore }) + $line.Substring($h.End)
        }
        $new = -join $lines
        if ($new -cne $text) {
            [IO.File]::WriteAllBytes((Join-Path $Repo $group.Name), $utf8.GetBytes($new))
            $changed++
        }
    }
    $changed
}

$rules = @(Get-Rules)
$reviewed = Get-Reviewed
$old = Get-ReleaseVersion
$null = Get-VersionKey $old
$scan = Invoke-Scan $old
$ok = Write-Report $scan
$locations = Get-Locations $scan
Write-Host ''
Compare-Locations (Read-Locations) $locations | ForEach-Object { Write-Host $_ }

if ($Command -eq 'scan') {
    if ($Save) {
        if (-not $ok) { Write-Host "`nnot saved: fix the problems above first"; exit 2 }
        Save-Locations $locations
        Write-Host "`nsaved $locationsFile"
    }
    exit $(if ($ok) { 0 } else { 2 })
}

if (-not $To) { throw 'bump needs -To' }
if ((Get-VersionKey $To) -le (Get-VersionKey $old)) { throw "$To is not after $old" }
$coreMoves = (Get-Core $To) -ne (Get-Core $old)
Write-Host "`nbump $old -> ${To}: full spans -> $To, core spans $(if ($coreMoves) { "$(Get-Core $old) -> $(Get-Core $To)" } else { 'stay' })"
if (-not $ok) { Write-Host "`nstopped: fix the problems above first"; exit 2 }
if ($scan.Suspects.Count -and ($coreMoves -or -not $scan.IsPre) -and -not $AcceptSuspects) {
    Write-Host "`nstopped: this bump changes bare versions, so every suspect needs a rule or a reviewed.json entry first"
    exit 2
}
if ($DryRun) { Write-Host "`ndry run: nothing written"; exit 0 }

$changed = Invoke-Apply $scan $To
$after = Invoke-Scan $To
$problems = [Collections.Generic.List[string]]::new()
if ((Get-ReleaseVersion) -ne $To) { $problems.Add('ReleaseVersion did not change') }
if ($scan.IsPre) {
    $oldRe = [regex]::new((Get-Token $old))
    $keptLines = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($k in $scan.Kept) { $null = $keptLines.Add("$($k.File):$($k.Line):") }
    $left = @(@(git -C $Repo grep -n -I -F -e $old) + @(git -C $Repo grep --untracked -n -I -F -e $old) | Where-Object { $_ -and $oldRe.IsMatch($_) -and -not ($_ -match '^(.+?:\d+:)' -and $keptLines.Contains($Matches[1])) } | Select-Object -Unique)
    if ($left.Count) { $problems.Add("$old is still in the repo:`n$($left -join "`n")") }
}
if ($after.Hits.Count -ne $scan.Hits.Count) { $problems.Add("$($scan.Hits.Count) spans before, $($after.Hits.Count) after: the rules match a different set of lines at $To") }
if ($problems.Count) { Write-Host "`nAFTER THE BUMP:`n$($problems -join "`n")"; exit 2 }
$newLocations = Get-Locations $after
Save-Locations $newLocations
Write-Host "`nchanged $changed files, $($after.Hits.Count) spans; locations.json updated"
