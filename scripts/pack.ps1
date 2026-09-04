<#
    .SYNOPSIS
    Builds the archives that go on a release.

    .DESCRIPTION
    Builds the plugin in Release for each platform, lays the files out in a folder named
    after the plugin, zips it, and writes a SHA256 checksum next to each archive.

    .PARAMETER Platforms
    Platforms to build. Defaults to both.

    .PARAMETER Destination
    Where to write the archives. Defaults to the artifacts folder at the root of the
    repository.

    .EXAMPLE
    .\scripts\pack.ps1

    .EXAMPLE
    .\scripts\pack.ps1 -Destination ..\releases\v1.0\upload
#>
[CmdletBinding()]
param (
    [ValidateSet('x64', 'ARM64')]
    [string[]]$Platforms = @('x64', 'ARM64'),

    [string]$Destination
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

if (-not $Destination) {
    $Destination = Join-Path (Get-RepositoryRoot) 'artifacts'
}

$version = Get-PluginVersion

Write-Host "Version    : $version"
Write-Host "Destination: $Destination"
Write-Host ''

if (Test-Path $Destination) {
    Remove-Item -LiteralPath $Destination -Recurse -Force
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null

# A release build starts from nothing. An incremental one keeps whatever an earlier
# layout produced, and those leftovers end up in the archive.
Write-Host 'Cleaning...'
Get-ChildItem -Path (Join-Path (Get-RepositoryRoot) 'src') -Include 'bin', 'obj' -Directory -Recurse |
    Remove-Item -Recurse -Force

foreach ($platform in $Platforms) {
    Write-Host "Packing $platform..."
    Invoke-PluginBuild -Platform $platform

    $staging = Join-Path $Destination "staging\$script:PluginFolderName"
    Copy-PluginFiles -From (Get-BuildOutput -Platform $platform) -To $staging

    $archive = Join-Path $Destination "$script:PluginFolderName-$version-$($platform.ToLowerInvariant()).zip"
    Compress-Archive -Path $staging -DestinationPath $archive -Force

    $checksum = (Get-FileHash -Path $archive -Algorithm SHA256).Hash

    # Written without a byte order mark. Set-Content -Encoding utf8 adds one in Windows
    # PowerShell, and a checksum tool would read it as part of the hash and report the
    # download as altered.
    [System.IO.File]::WriteAllText(
        "$archive.sha256",
        "$checksum  $(Split-Path -Leaf $archive)`n",
        (New-Object System.Text.UTF8Encoding $false))

    Remove-Item -LiteralPath (Join-Path $Destination 'staging') -Recurse -Force

    Write-Host "  $(Split-Path -Leaf $archive)  $checksum"
}

Write-Host ''
Write-Host "Archives written to $Destination."
