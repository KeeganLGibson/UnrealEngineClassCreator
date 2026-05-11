<#
.SYNOPSIS
    Bumps the version, publishes the app, and builds the Inno Setup installer.

.PARAMETER Bump
    Which part of the version to increment: major, minor, or patch (default).

.PARAMETER NoBump
    Skip version increment — rebuild and repackage the current version.

.EXAMPLE
    .\Publish-Release.ps1              # 0.1.0 → 0.1.1
    .\Publish-Release.ps1 -Bump minor  # 0.1.0 → 0.2.0
    .\Publish-Release.ps1 -Bump major  # 0.1.0 → 1.0.0
    .\Publish-Release.ps1 -NoBump      # rebuild 0.1.0
#>
param(
    [ValidateSet("major", "minor", "patch")]
    [string]$Bump = "patch",

    [switch]$NoBump
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$InstallerDir = $PSScriptRoot
$RepoRoot     = Split-Path $InstallerDir -Parent
$CsprojPath   = Join-Path $RepoRoot "UEClassCreator\UEClassCreator.csproj"
$IssPath      = Join-Path $InstallerDir "UEClassCreator.iss"

# ── Read current version ───────────────────────────────────────────────────────
$csprojContent = [System.IO.File]::ReadAllText($CsprojPath)
if ($csprojContent -notmatch '<Version>([\d.]+)</Version>') {
    throw "Could not find <Version> in $CsprojPath"
}
$currentVersion = $Matches[1]

# ── Increment ──────────────────────────────────────────────────────────────────
if ($NoBump) {
    $newVersion = $currentVersion
} else {
    $parts = $currentVersion.Split('.')
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]

    switch ($Bump) {
        "major" { $major++; $minor = 0; $patch = 0 }
        "minor" { $minor++; $patch = 0 }
        "patch" { $patch++ }
    }

    $newVersion = "$major.$minor.$patch"
}

Write-Host "Version: $currentVersion → $newVersion" -ForegroundColor Cyan

# ── Update csproj ──────────────────────────────────────────────────────────────
if (-not $NoBump) {
    $csprojContent = $csprojContent -replace '<Version>[\d.]+</Version>', "<Version>$newVersion</Version>"
    [System.IO.File]::WriteAllText($CsprojPath, $csprojContent)
    Write-Host "  Updated $CsprojPath"
}

# ── Update .iss ────────────────────────────────────────────────────────────────
if (-not $NoBump) {
    $issContent = [System.IO.File]::ReadAllText($IssPath)
    $issContent = $issContent -replace '#define AppVersion\s+"[\d.]+"', "#define AppVersion   `"$newVersion`""
    [System.IO.File]::WriteAllText($IssPath, $issContent)
    Write-Host "  Updated $IssPath"
}

# ── Publish ────────────────────────────────────────────────────────────────────
Write-Host "`nPublishing win-x64..." -ForegroundColor Cyan
Push-Location (Join-Path $RepoRoot "UEClassCreator")
try {
    dotnet publish -p:PublishProfile=Properties/PublishProfiles/win-x64.pubxml
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)" }
} finally {
    Pop-Location
}

# ── Build installer ────────────────────────────────────────────────────────────
Write-Host "`nBuilding installer..." -ForegroundColor Cyan

$isccCmd = Get-Command iscc -ErrorAction SilentlyContinue
$isccPath = if ($isccCmd) { $isccCmd.Source } else { "C:\Program Files\Inno Setup 7\iscc.exe" }

if (-not (Test-Path $isccPath)) {
    throw "iscc.exe not found at '$isccPath'. Install Inno Setup 6 from https://jrsoftware.org/isinfo.php"
}

& $isccPath $IssPath
if ($LASTEXITCODE -ne 0) { throw "iscc failed (exit code $LASTEXITCODE)" }

# ── Done ───────────────────────────────────────────────────────────────────────
$output = Join-Path $InstallerDir "Output\UEClassCreator-Setup-$newVersion.exe"
Write-Host "`nDone! " -ForegroundColor Green -NoNewline
Write-Host $output
