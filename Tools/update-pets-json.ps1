# Writes "size", "animations" and "sounds" into every entry of Pets/pets.json, read from
# Pets/<folder>/animations.xml. The program shows them before the pet is downloaded
# (size on the download button, counts in the detail panel). Run it after changing a pet.
#
#   powershell -ExecutionPolicy Bypass -File Tools\update-pets-json.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$jsonPath = Join-Path $root 'Pets\pets.json'
$json = [IO.File]::ReadAllText($jsonPath)

$entry = [regex]'\{[^{}]*?"folder"\s*:\s*"([^"]+)"[^{}]*\}'
$updated = 0

# The gallery (GitHub master) sends a pet again only when its "lastupdate" changes. A pet whose files differ from
# the published ones (origin/master) but still has the published date gets today's date, so nobody has to remember.
$published = @{}
$pubJson = git -C $root show origin/master:Pets/pets.json 2>$null
if ($LASTEXITCODE -eq 0) {
    foreach ($m in $entry.Matches(($pubJson -join "`n"))) {
        $published[$m.Groups[1].Value] = [regex]::Match($m.Value, '"lastupdate"\s*:\s*"([^"]*)"').Groups[1].Value
    }
}
$now = (Get-Date).ToString('yyyy-MM-dd HH:mm')
$bumped = @()
$json = $entry.Replace($json, {
    param($m)
    $folder = $m.Groups[1].Value
    $file = Join-Path $root "Pets\$folder\animations.xml"
    $text = $m.Value
    if (-not (Test-Path $file)) { Write-Warning "missing $file"; return $text }

    # size as GitHub serves it, i.e. the size of the file stored in git:
    # - unchanged since the last commit: the stored size (some old pets are stored with CRLF)
    # - new or changed: what git will store, LF line endings (* text=auto)
    $rel = "Pets/$folder/animations.xml"
    git -C $root diff --quiet HEAD -- $rel 2>$null
    $unchanged = $LASTEXITCODE -eq 0 -and (git -C $root ls-files -- $rel)
    if ($unchanged) { $size = [long](git -C $root cat-file -s "HEAD:$rel") }
    else
    {
        $bytes = [IO.File]::ReadAllBytes($file)
        $crlf = 0
        for ($i = 1; $i -lt $bytes.Length; $i++) { if ($bytes[$i] -eq 10 -and $bytes[$i - 1] -eq 13) { $crlf++ } }
        $size = $bytes.Length - $crlf
    }

    # files changed since the published version, date not changed yet: today's date
    if ($published.ContainsKey($folder)) {
        $date = [regex]::Match($text, '"lastupdate"\s*:\s*"([^"]*)"').Groups[1].Value
        git -C $root diff --quiet origin/master -- "Pets/$folder" 2>$null
        $changed = $LASTEXITCODE -ne 0 -or (git -C $root ls-files --others --exclude-standard -- "Pets/$folder")
        if ($changed -and $date -eq $published[$folder]) {
            $text = [regex]::Replace($text, '("lastupdate"\s*:\s*")[^"]*(")', "`${1}$now`${2}")
            $script:bumped += $folder
        }
    }

    $xml = New-Object Xml.XmlDocument
    $xml.Load($file)
    $anims = @($xml.DocumentElement.SelectNodes("*[local-name()='animations']/*")).Count
    $sounds = @($xml.DocumentElement.SelectNodes("*[local-name()='sounds']/*")).Count

    # drop old values, then add the new ones after the last property
    $text = [regex]::Replace($text, ',?\s*"(size|animations|sounds)"\s*:\s*\d+', '')
    $indent = [regex]::Match($text, '\n([ \t]*)"folder"').Groups[1].Value
    $close = [regex]::Match($text, '\s*\}$')
    $body = $text.Substring(0, $close.Index)
    $nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $script:updated++
    return $body + ",$nl$indent""size"" : $size,$nl$indent""animations"" : $anims,$nl$indent""sounds"" : $sounds" + $close.Value
})

$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($jsonPath, $json, $utf8)
Write-Host "pets.json: $updated entries updated"
if ($bumped.Count -gt 0) { Write-Host ("new date ($now) for the changed pets: " + ($bumped -join ', ')) }
