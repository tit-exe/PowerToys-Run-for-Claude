<#
    .SYNOPSIS
    Builds the plugin and installs it into PowerToys Run.

    .DESCRIPTION
    Builds in Release, then replaces the plugin folder under
    %LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins and restarts PowerToys.

    PowerToys has to be closed while the folder is replaced, because it holds the plugin
    assembly open. The script stops it and starts it again afterwards. If it was not
    running to begin with, it is left closed.

    .PARAMETER Platform
    Build platform. Defaults to the architecture of the machine running the script.

    .PARAMETER SkipRestart
    Leave PowerToys closed instead of starting it again.

    .EXAMPLE
    .\scripts\deploy.ps1

    .EXAMPLE
    .\scripts\deploy.ps1 -Platform ARM64
#>
[CmdletBinding()]
param (
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform,

    [switch]$SkipRestart
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

if (-not $Platform) {
    $Platform = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'ARM64' } else { 'x64' }
}

$destination = Join-Path $env:LOCALAPPDATA "Microsoft\PowerToys\PowerToys Run\Plugins\$script:PluginFolderName"

Write-Host "Platform   : $Platform"
Write-Host "Destination: $destination"
Write-Host ''

$wasRunning = [bool](Get-Process -Name 'PowerToys' -ErrorAction SilentlyContinue)
$stopped = $false

if ($wasRunning) {
    Write-Host 'Closing PowerToys...'
    try {
        Stop-Process -Name 'PowerToys' -Force -ErrorAction Stop
        $stopped = $true

        # Give the launcher a moment to release the plugin assembly.
        Start-Sleep -Milliseconds 750
    }
    catch {
        # PowerToys running elevated cannot be stopped from a normal prompt. That only
        # matters if it currently holds the plugin assembly open.
        if (Test-Path -LiteralPath $destination) {
            throw 'PowerToys is running elevated and the plugin is already installed, so its files are locked. Run this script from an elevated prompt.'
        }

        Write-Warning 'PowerToys is running elevated and could not be closed. Installing anyway; restart PowerToys yourself to load the plugin.'
    }
}

Write-Host 'Building...'
Invoke-PluginBuild -Platform $Platform

Write-Host 'Installing...'
if (Test-Path -LiteralPath $destination) {
    Remove-Item -LiteralPath $destination -Recurse -Force
}

Copy-PluginFiles -From (Get-BuildOutput -Platform $Platform) -To $destination

if ($SkipRestart -or -not $stopped) {
    Write-Host ''
    Write-Host $(if ($wasRunning) { 'Installed. Restart PowerToys to load the plugin.' }
                 else { 'Installed. Start PowerToys to load the plugin.' })
    return
}

$powerToys = @(
    (Join-Path $env:ProgramFiles 'PowerToys\PowerToys.exe'),
    (Join-Path $env:LOCALAPPDATA 'PowerToys\PowerToys.exe')
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($powerToys) {
    Write-Host 'Starting PowerToys...'
    Start-Process -FilePath $powerToys
}
else {
    Write-Warning 'Could not find PowerToys.exe. Start it yourself to load the plugin.'
}

Write-Host ''
Write-Host 'Done.'
