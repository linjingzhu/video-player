#Requires -Version 7.0
<#
.SYNOPSIS
  Download the pinned libmpv-2.dll and the Vulkan loader it imports.

.DESCRIPTION
  Checks SHA-256 before use. Writes libmpv-2.dll and vulkan-1.dll into
  -Destination. The pinned libmpv build imports vulkan-1.dll by name (it is
  not delay-loaded), so the loader is required for the DLL to map.
  Does not build the app and does not download ffmpeg.

.PARAMETER Destination
  Directory that should receive the two DLLs. Created when missing.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Destination
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Same libmpv and Vulkan pins as the Release package script. Verified 2026-11-22.
$LibmpvUrl = "https://github.com/shinchiro/mpv-winbuild-cmake/releases/download/20261111/mpv-dev-x86_64-20261111-git-bcde6b93cc.7z"
$LibmpvSha256 = "273c079b11ea5575822f972756f4c9a71149a5c10130353abeb8586738938452"
$VulkanUrl = "https://sdk.lunarg.com/sdk/download/1.4.321.0/windows/vulkan-runtime-components.zip"
$VulkanSha256 = "f8ba79303face4b72a2f302f1a46e8c918995b54fbf24c0c9fc6c3d3ef525dca"

$Root = Split-Path -Parent $PSScriptRoot
$Cache = Join-Path $Root "artifacts/cache"
New-Item -ItemType Directory -Force -Path $Cache | Out-Null
New-Item -ItemType Directory -Force -Path $Destination | Out-Null

function Find-SevenZip {
    $candidates = @(
        "$env:ProgramFiles\7-Zip\7z.exe",
        "${env:ProgramFiles(x86)}\7-Zip\7z.exe",
        "$env:ProgramW6432\7-Zip\7z.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    $command = Get-Command 7z -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    throw "7-Zip (7z.exe) is required to extract the pinned libmpv archive. GitHub-hosted Windows runners include it under Program Files\7-Zip."
}

function Save-PinnedFile {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$Sha256,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $expected = $Sha256.ToLowerInvariant()
    if (Test-Path -LiteralPath $Path) {
        $have = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
        if ($have -eq $expected) {
            Write-Host "Cached $Path"
            return
        }

        Remove-Item -LiteralPath $Path -Force
    }

    $partial = "$Path.partial"
    $headers = @{ "User-Agent" = "iese-o-ci" }
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            if (Test-Path -LiteralPath $partial) {
                Remove-Item -LiteralPath $partial -Force
            }

            Invoke-WebRequest -Uri $Url -OutFile $partial -UseBasicParsing -Headers $headers
            break
        }
        catch {
            Write-Warning "Download attempt $attempt failed for ${Url}: $($_.Exception.Message)"
            if ($attempt -eq 3) {
                throw
            }

            Start-Sleep -Seconds (5 * $attempt)
        }
    }

    $got = (Get-FileHash -Algorithm SHA256 -LiteralPath $partial).Hash.ToLowerInvariant()
    if ($got -ne $expected) {
        Remove-Item -LiteralPath $partial -Force
        throw "SHA-256 mismatch for $Url. Expected $expected, got $got."
    }

    Move-Item -LiteralPath $partial -Destination $Path -Force
    Write-Host "Verified $Path"
}

function Expand-SevenZip {
    param(
        [Parameter(Mandatory = $true)][string]$SevenZip,
        [Parameter(Mandatory = $true)][string]$Archive,
        [Parameter(Mandatory = $true)][string]$OutDir
    )

    if (Test-Path -LiteralPath $OutDir) {
        Remove-Item -LiteralPath $OutDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    & $SevenZip x $Archive "-o$OutDir" -y | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "7-Zip failed ($LASTEXITCODE) extracting $Archive"
    }
}

$sevenZip = Find-SevenZip
$libmpvArchive = Join-Path $Cache "mpv-dev-x86_64-20261111-git-bcde6b93cc.7z"
$vulkanArchive = Join-Path $Cache "vulkan-runtime-1.4.321.0-components.zip"
Save-PinnedFile -Url $LibmpvUrl -Sha256 $LibmpvSha256 -Path $libmpvArchive
Save-PinnedFile -Url $VulkanUrl -Sha256 $VulkanSha256 -Path $vulkanArchive

$libmpvExtract = Join-Path $Cache "libmpv"
Expand-SevenZip -SevenZip $sevenZip -Archive $libmpvArchive -OutDir $libmpvExtract
$libmpv = Get-ChildItem -LiteralPath $libmpvExtract -Recurse -File -Filter "libmpv-2.dll" | Select-Object -First 1
if (-not $libmpv) {
    throw "libmpv-2.dll was not in the pinned archive."
}

$vulkanExtract = Join-Path $Cache "vulkan"
if (Test-Path -LiteralPath $vulkanExtract) {
    Remove-Item -LiteralPath $vulkanExtract -Recurse -Force
}
Expand-Archive -LiteralPath $vulkanArchive -DestinationPath $vulkanExtract -Force
$vulkanCandidates = @(Get-ChildItem -LiteralPath $vulkanExtract -Recurse -File -Filter "vulkan-1.dll")
$vulkan = $vulkanCandidates | Where-Object { $_.Directory.Name -eq "x64" } | Select-Object -First 1
if (-not $vulkan) {
    $vulkan = $vulkanCandidates | Where-Object { $_.FullName -notmatch '[\\/]x86[\\/]' } | Select-Object -First 1
}
if (-not $vulkan) {
    throw "x64 vulkan-1.dll was not in the pinned runtime zip."
}

Copy-Item -LiteralPath $libmpv.FullName -Destination (Join-Path $Destination "libmpv-2.dll") -Force
Copy-Item -LiteralPath $vulkan.FullName -Destination (Join-Path $Destination "vulkan-1.dll") -Force
Write-Host "Copied libmpv-2.dll and vulkan-1.dll to $Destination"
