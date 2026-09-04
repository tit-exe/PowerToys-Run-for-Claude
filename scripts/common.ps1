<#
    .SYNOPSIS
    Shared helpers for deploy.ps1 and pack.ps1.

    .DESCRIPTION
    Dot source this file; it defines no behaviour of its own.
#>

$script:ProjectName = 'Community.PowerToys.Run.Plugin.Claude'

# The folder name users see under PowerToys Run\Plugins, and the stem of the release
# archives. Deliberately not just the product name it opens.
$script:PluginFolderName = 'NewChatForClaude'

<#
    Files the build produces that the plugin does not need at run time.

    The five PowerToys assemblies are compile time references: PowerToys loads its own
    copies, and a second set next to the plugin makes the launcher load the same types
    twice, after which the plugin fails to start. The XML file is documentation the SDK
    emits alongside the assembly.
#>
$script:UnshippedFiles = @(
    'PowerToys.Common.UI.*',
    'PowerToys.ManagedCommon.*',
    'PowerToys.Settings.UI.Lib.*',
    'Wox.Infrastructure.*',
    'Wox.Plugin.*',
    '*.xml'
)

function Get-RepositoryRoot {
    return Split-Path -Parent $PSScriptRoot
}

function Get-ProjectFolder {
    return Join-Path (Get-RepositoryRoot) "src\$script:ProjectName"
}

function Get-ProjectFile {
    return Join-Path (Get-ProjectFolder) "$script:ProjectName.csproj"
}

function Get-PluginVersion {
    $manifest = Get-Content -Path (Join-Path (Get-ProjectFolder) 'plugin.json') -Raw -Encoding UTF8 | ConvertFrom-Json

    if (-not $manifest.Version) {
        throw 'Could not read the version from plugin.json.'
    }

    return $manifest.Version
}

function Get-BuildOutput {
    param ([Parameter(Mandatory = $true)][string]$Platform)

    $root = Join-Path (Get-ProjectFolder) "bin\$Platform\Release"

    if (-not (Test-Path $root)) {
        throw "No build output under $root."
    }

    $folders = @(Get-ChildItem -LiteralPath $root -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName 'plugin.json') })

    if ($folders.Count -ne 1) {
        throw "Expected exactly one target framework folder under $root, found $($folders.Count)."
    }

    return $folders[0].FullName
}

function Invoke-PluginBuild {
    param ([Parameter(Mandatory = $true)][string]$Platform)

    & dotnet build (Get-ProjectFile) -c Release /p:Platform=$Platform

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $Platform with exit code $LASTEXITCODE."
    }
}

function Copy-PluginFiles {
    param (
        [Parameter(Mandatory = $true)][string]$From,
        [Parameter(Mandatory = $true)][string]$To
    )

    New-Item -ItemType Directory -Path $To -Force | Out-Null
    Copy-Item -Path (Join-Path $From '*') -Destination $To -Recurse -Force

    foreach ($pattern in $script:UnshippedFiles) {
        Get-ChildItem -LiteralPath $To -Filter $pattern -File -Recurse | Remove-Item -Force
    }
}
