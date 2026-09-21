$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$skillsRoot = Join-Path $repoRoot 'skills'
$readmePath = Join-Path $skillsRoot 'README.md'
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$names = @{}

Get-ChildItem -LiteralPath $skillsRoot -Directory | ForEach-Object {
    $skillDir = $_
    $skillPath = Join-Path $skillDir.FullName 'SKILL.md'
    if (-not (Test-Path -LiteralPath $skillPath)) { return }

    $content = Get-Content -LiteralPath $skillPath -Raw
    if ($content -match '(?ms)^---\s*(.*?)\s*---') { $frontmatter = $Matches[1] } else { $frontmatter = '' }
    $nameMatch = [regex]::Match($frontmatter, '(?m)^name:\s*(.+?)\s*$')
    $descriptionMatch = [regex]::Match($frontmatter, '(?m)^description:\s*(.+?)\s*$')

    if (-not $nameMatch.Success) { $errors.Add("Missing frontmatter name: $($skillDir.Name)") } else {
        $name = $nameMatch.Groups[1].Value.Trim('''" ')
        if ($name -ne $skillDir.Name) { $errors.Add("Folder/name mismatch: $($skillDir.Name) != $name") }
        if ($names.ContainsKey($name)) { $errors.Add("Duplicate skill name: $name") } else { $names[$name] = $skillPath }
    }
    if (-not $descriptionMatch.Success -or [string]::IsNullOrWhiteSpace($descriptionMatch.Groups[1].Value)) { $errors.Add("Missing description: $($skillDir.Name)") }
    if ($content -match '(?i)library/') { $errors.Add("Stale library/ reference: $skillPath") }
    foreach ($referenceType in @('refs','recipes')) {
        $referencePattern = [char]96 + $referenceType + '/([^' + [char]96 + ']*)' + [char]96
        [regex]::Matches($content, $referencePattern) | ForEach-Object {
            $referencePath = Join-Path $skillDir.FullName ($referenceType + '/' + $_.Groups[1].Value)
            if (-not (Test-Path -LiteralPath $referencePath)) { $errors.Add(('Dangling {0} reference: {1} -> {2}' -f $referenceType, $skillPath, $_.Groups[1].Value)) }
        }
        $markdownLinkPattern = '\(' + $referenceType + '/([^\)#]+)(?:#[^\)]*)?\)'
        [regex]::Matches($content, $markdownLinkPattern) | ForEach-Object {
            $referencePath = Join-Path $skillDir.FullName ($referenceType + '/' + $_.Groups[1].Value)
            if (-not (Test-Path -LiteralPath $referencePath)) { $errors.Add(('Dangling Markdown link: {0} -> {1}' -f $skillPath, $_.Groups[1].Value)) }
        }
    }
    if ($content.Length -gt 12000) { $warnings.Add("Long SKILL.md: $skillPath") }
    $fence = [char]96 + [char]96 + [char]96
    $longCsharpBlock = [regex]::Matches($content, '(?s)' + $fence + 'csharp.*?' + $fence) | Where-Object { $_.Value.Length -gt 800 }
    if ($longCsharpBlock) { $warnings.Add("Long C# block in SKILL.md (>800 chars): $skillPath") }
    if (-not ((Get-Content -LiteralPath $readmePath -Raw) -match [regex]::Escape("$($skillDir.Name)/"))) { $errors.Add("Skill folder missing from README: $($skillDir.Name)") }
}

if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ } }
$warnings | ForEach-Object { Write-Warning $_ }
Write-Output "template-lint: $($names.Count) skills checked; $($errors.Count) errors; $($warnings.Count) warnings"
if ($errors.Count -gt 0) { exit 1 }
