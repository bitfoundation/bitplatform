[CmdletBinding()]
param()

Import-VstsLocStrings "$PSScriptRoot\task.json"

[string]$action = Get-VstsInput -Name Action
[string]$path = Get-VstsInput -Name Path
[string]$bitCLIV1Exe = $PSScriptRoot + "\\bin\\BitCLIV1.exe"

#[string]$action = "Generate"
#[string]$path = "D:\bit-foundation\BitSampleApp\SampleApp.sln"
#[string]$bitCLIV1Exe = "D:\bit-foundation\bit-framework\BitTools\BitCLIV1Task\buildtask\bin\BitCLIV1.exe"

$proc = New-Object System.Diagnostics.Process
$proc.StartInfo.UseShellExecute = $false
$proc.StartInfo.RedirectStandardOutput = $true
$proc.StartInfo.FileName = $bitCLIV1Exe
$proc.StartInfo.Arguments = " -a " + $action + " -p " + $path
$proc.StartInfo.CreateNoWindow = $true

$proc.Start()

$proc.WaitForExit()

Write-Host $proc.StandardOutput.ReadToEnd()

if(-not ($proc.ExitCode -eq 0)) {
    throw "Bit CLI V1 failed"
}
else {
    Write-Host "Bit CLI V1 Success";
}