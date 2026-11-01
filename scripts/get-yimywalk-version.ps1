[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
[xml]$projectVersionDocument = Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw -Encoding UTF8
$projectVersion = [string]$projectVersionDocument.Project.PropertyGroup.VersionPrefix.'#text'
if ($projectVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Directory.Build.props must declare a valid VersionPrefix.' }
Write-Output $projectVersion
