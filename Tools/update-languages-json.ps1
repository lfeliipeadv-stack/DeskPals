# Writes Languages/languages.json: the list of languages the program offers to download in its settings
# (Settings > Application > More languages). One entry per Languages/<code>.json with the language's own name, its
# English name, the file size and a version (a hash of the content: any change is offered as an update).
# Run it after adding or changing a language file.
#
#   powershell -ExecutionPolicy Bypass -File Tools\update-languages-json.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'Languages'
$list = foreach ($file in Get-ChildItem $dir -Filter *.json | Where-Object Name -ne 'languages.json' | Sort-Object Name) {
    $code = $file.BaseName
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $texts = [Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
    $name = $texts.'language.name'
    if ([string]::IsNullOrWhiteSpace($name)) { throw "$($file.Name): no language.name" }
    $english = try { ([Globalization.CultureInfo]::GetCultureInfo($code)).EnglishName -replace ' \(.*\)$', '' } catch { $code }
    if ($code -eq 'zh-CN') { $english = 'Chinese (Simplified)' }
    if ($code -eq 'bn') { $english = 'Bengali' }
    $sha = [Security.Cryptography.SHA256]::Create()
    $version = (($sha.ComputeHash($bytes) | Select-Object -First 6 | ForEach-Object { $_.ToString('x2') }) -join '')
    [ordered]@{ code = $code; name = $name; english = $english; updated = $version; size = $bytes.Length }
}
function Esc([string]$s) { $s.Replace('\', '\\').Replace('"', '\"') }
$lines = $list | ForEach-Object { "    { ""code"": ""$(Esc $_.code)"", ""name"": ""$(Esc $_.name)"", ""english"": ""$(Esc $_.english)"", ""updated"": ""$($_.updated)"", ""size"": $($_.size) }" }
$json = "{`n  ""languages"": [`n" + ($lines -join ",`n") + "`n  ]`n}`n"
[IO.File]::WriteAllText((Join-Path $dir 'languages.json'), $json, (New-Object Text.UTF8Encoding $false))
"languages.json: $(@($list).Count) languages"
