# Applies packet D-A (Layout Bake production UX + contour-hash contract) into the SE-001 project.
# Run from anywhere:  powershell -ExecutionPolicy Bypass -File handoff\phase-D\da-staging\apply-da.ps1
# Idempotent: re-running skips steps that are already applied. Fails loudly if an anchor moved.
# Run it only when Unity is idle (Codex V1 finished) — it triggers one recompile.

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$staging = Join-Path $PSScriptRoot "files"

function Read-Text([string]$path) {
    $bytes = [IO.File]::ReadAllBytes($path)
    $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    return @{ Text = [IO.File]::ReadAllText($path); Bom = $bom }
}

function Write-Text([string]$path, [string]$text, [bool]$bom) {
    $encoding = New-Object System.Text.UTF8Encoding($bom)
    [IO.File]::WriteAllText($path, $text, $encoding)
}

function Patch([string]$relative, [string]$old, [string]$new) {
    $path = Join-Path $root $relative
    $file = Read-Text $path
    if ($file.Text.Contains($new)) { Write-Output "  skip (already applied): $relative"; return }
    if (-not $file.Text.Contains($old)) { throw "Anchor not found in $relative :: $old" }
    Write-Text $path ($file.Text.Replace($old, $new)) $file.Bom
    Write-Output "  patched: $relative"
}

Write-Output "1/4 Copy new/replaced files"
Get-ChildItem -Path $staging -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($staging.Length + 1)
    $target = Join-Path $root $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    Copy-Item $_.FullName $target -Force
    Write-Output "  copied: $relative"
}

Write-Output "2/4 Patch LayoutBaker (partial, public constants, write definition.contourHash)"
$baker = "Assets\_Core\4_Scripts\Editor\Level\C#\LayoutBaker.cs"
Patch $baker "    public static class LayoutBaker" "    public static partial class LayoutBaker"
Patch $baker "private const string LayoutResourceFolder" "public const string LayoutResourceFolder"
Patch $baker "private const string LayoutPrefabFolder" "public const string LayoutPrefabFolder"
Patch $baker "private static string NormalizeProjectPath(" "public static string NormalizeProjectPath("
Patch $baker "                definition.mask = mask;`r`n                definition.boardSize" "                definition.mask = mask;`r`n                definition.contourHash = snapshot.ContourHash;`r`n                definition.boardSize"

Write-Output "3/4 Route runtime through LayoutDefinition.TryBuildMaskSet"
Patch "Assets\_Core\4_Scripts\System\Creation\LevelSpawner.cs" `
    "if (layout.mask == null || !layout.mask.TryBuildMaskSet(profile.cellSize, profile.maxCells, out masks, out maskError))" `
    "if (!layout.TryBuildMaskSet(profile.cellSize, profile.maxCells, out masks, out maskError))"
Patch "Assets\_Core\4_Scripts\Data\LevelDataValidator.cs" `
    "if (!layout.mask.TryBuildMaskSet(cellSize, maxCells, out masks, out maskError))" `
    "if (!layout.TryBuildMaskSet(cellSize, maxCells, out masks, out maskError))"

Write-Output "4/4 Give existing layout definitions their expected contourHash (copied from their baked mask)"
$layouts = Join-Path $root "Assets\_Core\Resources\Layouts"
Get-ChildItem -Path $layouts -Filter "*_Mask.asset" | ForEach-Object {
    $id = $_.BaseName.Substring(0, $_.BaseName.Length - 5)
    $definitionPath = Join-Path $layouts ($id + ".asset")
    if (-not (Test-Path $definitionPath)) { Write-Output "  no definition for mask $($_.Name)"; return }
    $maskText = (Read-Text $_.FullName).Text
    $match = [regex]::Match($maskText, "(?m)^  contourHash: ([0-9a-f]+)\r?$")
    if (-not $match.Success) { throw "Mask $($_.Name) has no contourHash — rebake it in Layout Bake instead." }
    $hash = $match.Groups[1].Value
    $definition = Read-Text $definitionPath
    if ($definition.Text -match "(?m)^  contourHash: ") { Write-Output "  skip (has hash): $id"; return }
    $anchor = "`r`n  sourceSvgPath:"
    if (-not $definition.Text.Contains($anchor)) { throw "Anchor 'sourceSvgPath' not found in $id.asset" }
    Write-Text $definitionPath ($definition.Text.Replace($anchor, "`r`n  contourHash: $hash$anchor")) $definition.Bom
    Write-Output "  hashed: $id -> $($hash.Substring(0, 8))…"
}

Write-Output "Done. Focus Unity to recompile, then run EditMode tests (see handoff\phase-D\evidence\layout-bake-ux\functional-verification.md)."
