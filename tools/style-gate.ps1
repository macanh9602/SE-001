param(
    [string]$Baseline = "bd1621b"
)

$ErrorActionPreference = "Stop"
$tracked = @(git diff --name-only --diff-filter=ACMR "$Baseline" -- "Assets/_Core/4_Scripts/*.cs" "Assets/_Core/4_Scripts/**/*.cs")
$untracked = @(git ls-files --others --exclude-standard -- "Assets/_Core/4_Scripts/*.cs" "Assets/_Core/4_Scripts/**/*.cs")
$changed = @($tracked + $untracked | Sort-Object -Unique)
$violations = New-Object System.Collections.Generic.List[string]

foreach ($relativePath in $changed) {
    $fullPath = Join-Path (Get-Location) $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { continue }
    $lines = Get-Content -LiteralPath $fullPath
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = [string]$lines[$index]
        $lineNumber = $index + 1
        if ($line.Length -gt 160) {
            $violations.Add("${relativePath}:$lineNumber line length $($line.Length) > 160")
        }
        $codeWithoutStrings = [regex]::Replace($line, '"([^"\\]|\\.)*"', '""')
        if ($codeWithoutStrings -notmatch '\bfor\s*\(' -and ([regex]::Matches($codeWithoutStrings, ';')).Count -ge 2) {
            $violations.Add("${relativePath}:$lineNumber multiple statements")
        }
        if ($line -match 'ScriptableObject\.CreateInstance' -and $relativePath -notmatch '/(Tests|Editor)/') {
            $violations.Add("${relativePath}:$lineNumber runtime ScriptableObject.CreateInstance")
        }
    }
}

Write-Output "Baseline: $Baseline"
Write-Output "Changed C# files: $($changed.Count)"
if ($violations.Count -eq 0) {
    Write-Output "STYLE-GATE: PASS"
    exit 0
}

Write-Output "STYLE-GATE: FAIL"
$violations | ForEach-Object { Write-Output " - $_" }
exit 1
