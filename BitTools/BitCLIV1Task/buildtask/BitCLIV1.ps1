[CmdletBinding()]
param()

Import-VstsLocStrings "$PSScriptRoot\task.json"

[string]$action = Get-VstsInput -Name Action
[string]$path = Get-VstsInput -Name Path
[string]$bitCLIV1Exe = $PSScriptRoot + "\\bin\\BitCLIV1.exe"

# [string]$action = "Generate"
# [string]$path = "D:\bit-foundation\BitSampleApp\SampleApp.sln"
# [string]$bitCLIV1Exe = "D:\bit-foundation\bit-framework\BitTools\BitCLIV1Task\buildtask\bin\BitCLIV1.exe"

$proc = Start-Process $bitCLIV1Exe -NoNewWindow -Wait -ArgumentList (" -a " + $action + " -p " + $path) -PassThru

if(-not ($proc.ExitCode -eq 0)) {
    Write-Host "Bit CLI V1 failed" -ForegroundColor Red
    throw "Bit CLI V1 failed"
}
else {
    Write-Host "Bit CLI V1 Success" -ForegroundColor Green
}

