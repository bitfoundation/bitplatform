[CmdletBinding()]
param()

Import-VstsLocStrings "$PSScriptRoot\task.json"

[string]$action = Get-VstsInput -Name Action
[string]$path = Get-VstsInput -Name Path

./bin/BitCLIV1.exe -a $action -p $path