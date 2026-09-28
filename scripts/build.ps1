param([switch]$FrameworkDependent)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools\cli'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.tools\nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$outputName = if ($FrameworkDependent) { 'framework-dependent' } else { 'portable' }
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
& $dotnet publish (Join-Path $projectRoot 'src\VibeClock\VibeClock.csproj') -c Release -r win-x64 --self-contained $selfContained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o (Join-Path $projectRoot "dist\$outputName")
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Output "Ready: $projectRoot\dist\$outputName\VibeClock.exe"
