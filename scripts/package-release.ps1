#Requires -Version 7.0
<#
.SYNOPSIS
  Build a Release zip of 이어서 (Ieseo.exe) with libmpv, ffmpeg, and yt-dlp.

.DESCRIPTION
  Publishes the WPF app self-contained for win-x64, downloads pinned public
  archives, checks SHA-256, and writes artifacts/Ieseo[-<version>]-win-x64.zip.
  Native binaries are never committed. libmpv-2.dll and ffmpeg.exe import
  vulkan-1.dll as a normal (not delay-loaded) dependency, so the Vulkan loader
  is fetched from a pinned LunarG runtime zip and placed beside them.
  yt-dlp.exe is the official Windows executable from one yt-dlp GitHub release.
  Its SHA-256 is pinned. The zip places that file beside Ieseo.exe.

.PARAMETER Version
  Optional release tag such as v0.1.0. When set, the zip is named
  Ieseo-<version>-win-x64.zip. Empty produces Ieseo-win-x64.zip.

.PARAMETER Configuration
  Build configuration. Release is the package that ships.
#>
[CmdletBinding()]
param(
    [string]$Version = "",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

if ([string]::IsNullOrWhiteSpace($Version) -and -not [string]::IsNullOrWhiteSpace($env:PACKAGE_VERSION)) {
    $Version = $env:PACKAGE_VERSION.Trim()
}

if (-not [string]::IsNullOrWhiteSpace($Version) -and $Version -notmatch '^v[0-9A-Za-z][0-9A-Za-z._-]*$') {
    throw "Version must look like v1.2.3. Got '$Version'."
}

$Root = Split-Path -Parent $PSScriptRoot
$Artifacts = Join-Path $Root "artifacts"
$Cache = Join-Path $Artifacts "cache"
$NativeDir = Join-Path $Artifacts "native"
$PublishDir = Join-Path $Artifacts "publish"
$StageDir = Join-Path $Artifacts "stage"
New-Item -ItemType Directory -Force -Path $Cache, $NativeDir | Out-Null

# Verified 2026-11-22 against the published GitHub release digests and a local SHA-256 of the LunarG zip.
$LibmpvUrl = "https://github.com/shinchiro/mpv-winbuild-cmake/releases/download/20261111/mpv-dev-x86_64-20261111-git-bcde6b93cc.7z"
$LibmpvSha256 = "273c079b11ea5575822f972756f4c9a71149a5c10130353abeb8586738938452"
$FfmpegUrl = "https://github.com/shinchiro/mpv-winbuild-cmake/releases/download/20261111/ffmpeg-x86_64-git-cd4e96eb3.7z"
$FfmpegSha256 = "b943ccb7b7760fd5d2c449ebcbedaac5ce85b64437a9d877af0761ba402ed118"
$VulkanUrl = "https://sdk.lunarg.com/sdk/download/1.4.321.0/windows/vulkan-runtime-components.zip"
$VulkanSha256 = "f8ba79303face4b72a2f302f1a46e8c918995b54fbf24c0c9fc6c3d3ef525dca"
# Verified 2026-11-24. SHA2-256SUMS, the GitHub asset digest, and a local hash of the downloaded exe match.
$YtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/download/2026.11.23/yt-dlp.exe"
$YtDlpSha256 = "622ed48023cac1daa52a3bf3a6ac38f03514de214b5f0fa069389fd0f6d17e5c"

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

    throw "7-Zip (7z.exe) is required to extract the pinned libmpv and ffmpeg archives. GitHub-hosted Windows runners include it under Program Files\7-Zip."
}

function Save-PinnedFile {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$Sha256,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    $expected = $Sha256.ToLowerInvariant()
    if (Test-Path -LiteralPath $Destination) {
        $have = (Get-FileHash -Algorithm SHA256 -LiteralPath $Destination).Hash.ToLowerInvariant()
        if ($have -eq $expected) {
            Write-Host "Cached $Destination"
            return
        }

        Write-Host "Replacing $Destination because its SHA-256 changed."
        Remove-Item -LiteralPath $Destination -Force
    }

    $partial = "$Destination.partial"
    $headers = @{ "User-Agent" = "iese-o-packaging" }
    $downloaded = $false
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            if (Test-Path -LiteralPath $partial) {
                Remove-Item -LiteralPath $partial -Force
            }

            Invoke-WebRequest -Uri $Url -OutFile $partial -UseBasicParsing -Headers $headers
            $downloaded = $true
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

    if (-not $downloaded) {
        throw "Failed to download $Url"
    }

    $got = (Get-FileHash -Algorithm SHA256 -LiteralPath $partial).Hash.ToLowerInvariant()
    if ($got -ne $expected) {
        Remove-Item -LiteralPath $partial -Force
        throw "SHA-256 mismatch for $Url. Expected $expected, got $got."
    }

    Move-Item -LiteralPath $partial -Destination $Destination -Force
    Write-Host "Verified $Destination"
}

function Expand-SevenZip {
    param(
        [Parameter(Mandatory = $true)][string]$SevenZip,
        [Parameter(Mandatory = $true)][string]$Archive,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    & $SevenZip x $Archive "-o$Destination" -y | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "7-Zip failed ($LASTEXITCODE) extracting $Archive"
    }
}

function Copy-FirstFile {
    param(
        [Parameter(Mandatory = $true)][string]$RootDir,
        [Parameter(Mandatory = $true)][string]$Filter,
        [Parameter(Mandatory = $true)][string]$DestinationFile
    )

    $found = Get-ChildItem -LiteralPath $RootDir -Recurse -File -Filter $Filter | Select-Object -First 1
    if (-not $found) {
        throw "Did not find $Filter under $RootDir"
    }

    Copy-Item -LiteralPath $found.FullName -Destination $DestinationFile -Force
    return (Get-Item -LiteralPath $DestinationFile)
}

function Copy-VulkanLoader {
    param(
        [Parameter(Mandatory = $true)][string]$RootDir,
        [Parameter(Mandatory = $true)][string]$DestinationFile
    )

    $candidates = @(Get-ChildItem -LiteralPath $RootDir -Recurse -File -Filter "vulkan-1.dll")
    $match = $candidates | Where-Object { $_.Directory.Name -eq "x64" } | Select-Object -First 1
    if (-not $match) {
        $match = $candidates | Where-Object { $_.FullName -notmatch '[\\/]x86[\\/]' } | Select-Object -First 1
    }

    if (-not $match) {
        throw "vulkan-1.dll (x64) was not in $RootDir"
    }

    Copy-Item -LiteralPath $match.FullName -Destination $DestinationFile -Force
    return (Get-Item -LiteralPath $DestinationFile)
}

function Assert-FfmpegEncoders {
    param([Parameter(Mandatory = $true)][string]$FfmpegPath)

    $encoders = & $FfmpegPath -hide_banner -encoders 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "ffmpeg.exe -encoders exited $LASTEXITCODE. vulkan-1.dll must sit beside ffmpeg.exe."
    }

    if ($encoders -notmatch '(?m)\blibwebp\b') {
        throw "Pinned ffmpeg.exe does not list the libwebp encoder required for webp clip export."
    }

    $formats = & $FfmpegPath -hide_banner -formats 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "ffmpeg.exe -formats exited $LASTEXITCODE."
    }

    if ($formats -notmatch '(?m)\bgif\b') {
        throw "Pinned ffmpeg.exe does not list a gif muxer."
    }

    Write-Host "ffmpeg.exe provides libwebp and gif."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found on PATH. Install the .NET 8 SDK."
}

$sevenZip = Find-SevenZip
$libmpvArchive = Join-Path $Cache "mpv-dev-x86_64-20261111-git-bcde6b93cc.7z"
$ffmpegArchive = Join-Path $Cache "ffmpeg-x86_64-git-cd4e96eb3.7z"
$vulkanArchive = Join-Path $Cache "vulkan-runtime-1.4.321.0-components.zip"
$ytdlpFile = Join-Path $Cache "yt-dlp-2026.11.23.exe"

Save-PinnedFile -Url $LibmpvUrl -Sha256 $LibmpvSha256 -Destination $libmpvArchive
Save-PinnedFile -Url $FfmpegUrl -Sha256 $FfmpegSha256 -Destination $ffmpegArchive
Save-PinnedFile -Url $VulkanUrl -Sha256 $VulkanSha256 -Destination $vulkanArchive
Save-PinnedFile -Url $YtDlpUrl -Sha256 $YtDlpSha256 -Destination $ytdlpFile

$libmpvExtract = Join-Path $Cache "libmpv"
$ffmpegExtract = Join-Path $Cache "ffmpeg"
Expand-SevenZip -SevenZip $sevenZip -Archive $libmpvArchive -Destination $libmpvExtract
Expand-SevenZip -SevenZip $sevenZip -Archive $ffmpegArchive -Destination $ffmpegExtract

$vulkanExtract = Join-Path $Cache "vulkan"
if (Test-Path -LiteralPath $vulkanExtract) {
    Remove-Item -LiteralPath $vulkanExtract -Recurse -Force
}
Expand-Archive -LiteralPath $vulkanArchive -DestinationPath $vulkanExtract -Force

if (Test-Path -LiteralPath $NativeDir) {
    Remove-Item -LiteralPath $NativeDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $NativeDir | Out-Null

$libmpv = Copy-FirstFile -RootDir $libmpvExtract -Filter "libmpv-2.dll" -DestinationFile (Join-Path $NativeDir "libmpv-2.dll")
$ffmpeg = Copy-FirstFile -RootDir $ffmpegExtract -Filter "ffmpeg.exe" -DestinationFile (Join-Path $NativeDir "ffmpeg.exe")
$vulkan = Copy-VulkanLoader -RootDir $vulkanExtract -DestinationFile (Join-Path $NativeDir "vulkan-1.dll")
Assert-FfmpegEncoders -FfmpegPath $ffmpeg.FullName

if (Test-Path -LiteralPath $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

$project = Join-Path $Root "src/VideoPlayer.App/VideoPlayer.App.csproj"
& dotnet publish $project -c $Configuration -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $PublishDir "Ieseo.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Publish did not produce Ieseo.exe at $exe"
}

if (Test-Path -LiteralPath $StageDir) {
    Remove-Item -LiteralPath $StageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $StageDir | Out-Null

Get-ChildItem -LiteralPath $PublishDir -Recurse -File | ForEach-Object {
    if ($_.Extension -in ".pdb", ".xml") {
        return
    }

    $relative = $_.FullName.Substring($PublishDir.Length).TrimStart('\', '/')
    $target = Join-Path $StageDir $relative
    $parent = Split-Path -Parent $target
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Copy-Item -LiteralPath $_.FullName -Destination $target -Force
}

Copy-Item -LiteralPath $libmpv.FullName, $ffmpeg.FullName, $vulkan.FullName -Destination $StageDir -Force
Copy-Item -LiteralPath $ytdlpFile -Destination (Join-Path $StageDir "yt-dlp.exe") -Force

$zipLeaf = if ([string]::IsNullOrWhiteSpace($Version)) { "Ieseo-win-x64.zip" } else { "Ieseo-$Version-win-x64.zip" }
$notice = @"
이어서 ($zipLeaf)
Unzip this folder and run Ieseo.exe.
The build is self-contained for 64-bit Windows 10 or later. No separate .NET install is required.
ffmpeg.exe is included for webp and gif clip export.

These files are downloaded when the zip is built. They are not stored in the git repository.

libmpv-2.dll
  $LibmpvUrl
  sha256 $LibmpvSha256
  shinchiro mpv-dev x86_64 package (libmpv / FFmpeg, GPL).

ffmpeg.exe
  $FfmpegUrl
  sha256 $FfmpegSha256
  shinchiro ffmpeg x86_64 build (GPL). Provides the libwebp encoder and a gif muxer.

vulkan-1.dll
  $VulkanUrl
  sha256 $VulkanSha256
  LunarG Vulkan Runtime 1.4.321.0 x64 loader (Apache-2.0).
  The pinned libmpv-2.dll and ffmpeg.exe import this DLL by name, so it has to sit beside Ieseo.exe.

yt-dlp.exe
  $YtDlpUrl
  sha256 $YtDlpSha256
  Official yt-dlp 2026.11.23 Windows x64 executable (Unlicense), placed beside Ieseo.exe.
"@
Set-Content -LiteralPath (Join-Path $StageDir "THIRD-PARTY.txt") -Value $notice -Encoding utf8

foreach ($required in @("Ieseo.exe", "libmpv-2.dll", "ffmpeg.exe", "vulkan-1.dll", "yt-dlp.exe", "THIRD-PARTY.txt")) {
    $path = Join-Path $StageDir $required
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Package is missing $required"
    }

    $length = (Get-Item -LiteralPath $path).Length
    Write-Host ("{0} {1:N0} bytes" -f $required, $length)
}

New-Item -ItemType Directory -Force -Path $Artifacts | Out-Null
$zipPath = Join-Path $Artifacts $zipLeaf
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $StageDir,
    $zipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)

$zipItem = Get-Item -LiteralPath $zipPath
Write-Host ("Wrote {0} ({1:N1} MB)" -f $zipItem.FullName, ($zipItem.Length / 1MB))
