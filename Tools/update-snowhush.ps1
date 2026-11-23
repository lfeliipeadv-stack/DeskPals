<#
.SYNOPSIS
    Keeps Snowhush (the snow engine of DeskPals Weather, extensions\snowhush-src) up to date with its author's versions.

.DESCRIPTION
    The DeskPals changes to Snowhush (the "hosted" mode, its own settings key, the colour of what falls; all marked
    "DeskPals:" in the code) are kept in extensions\snowhush-deskpals.patch, so they can be put back on any new version.

    -Check       only says whether the author published a newer version than the one here.
    (nothing)    brings the newest version (or -Version x.y.z): downloads it, puts the DeskPals changes back on it, builds
                 it, and only then puts it in place of the old copy. When a change no longer fits (the author changed
                 the same lines), nothing here is touched: the new copy stays in a temporary folder with the parts that
                 did not fit in *.rej files, to be finished by hand.
    -MakePatch   writes the patch again from the copy here (after changing Snowhush by hand, or finishing an update).

    After an update: run the tests of the extension (weather\...\DeskPalsWeather.exe --devtests) and commit.
    Snowhush stays under its own license (PolyForm Noncommercial 1.0.0, extensions\snowhush-src\LICENSE).

.EXAMPLE
    .\Tools\update-snowhush.ps1 -Check
.EXAMPLE
    .\Tools\update-snowhush.ps1
.EXAMPLE
    .\Tools\update-snowhush.ps1 -MakePatch
#>
param(
    [string]$Version,
    [switch]$Check,
    [switch]$MakePatch
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$here = Join-Path $root 'extensions\snowhush-src'
$patch = Join-Path $root 'extensions\snowhush-deskpals.patch'
$readme = Join-Path $root 'extensions\README.md'
$repo = 'https://github.com/mbezruchko/Snowhush'
$utf8 = New-Object System.Text.UTF8Encoding($false)

$current = (Select-String -Path (Join-Path $here 'windows\Cargo.toml') -Pattern '^version\s*=\s*"([^"]+)"').Matches[0].Groups[1].Value

function Get-Upstream([string]$v, [string]$to) {
    if (Test-Path $to) { Remove-Item $to -Recurse -Force }
    # the same line endings as the copy here (Windows: CRLF), so the patch fits
    git -c core.autocrlf=true -c advice.detachedHead=false clone -q --depth 1 --branch "v$v" $repo $to
    if ($LASTEXITCODE -ne 0) { throw "Snowhush v$v could not be downloaded from $repo" }
    Remove-Item (Join-Path $to '.git') -Recurse -Force
}

# ---- the patch, written again from the copy here
if ($MakePatch) {
    $base = Join-Path $env:TEMP "snowhush-base"
    Get-Upstream $current (Join-Path $base 'a')
    $b = Join-Path $base 'b'
    if (Test-Path $b) { Remove-Item $b -Recurse -Force }
    robocopy $here $b /E /XD target .git /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy could not copy $here" }
    $global:LASTEXITCODE = 0   # robocopy says 1 when it copied files
    Push-Location $base
    $diff = git -c core.autocrlf=false diff --no-index a b
    Pop-Location
    # "a/a/windows/…" and "b/b/windows/…" → "a/windows/…" and "b/windows/…"
    $diff = $diff | ForEach-Object { $_ -replace '^diff --git a/[ab]/(\S+) b/[ab]/(\S+)', 'diff --git a/$1 b/$2' -replace '^--- a/a/', '--- a/' -replace '^\+\+\+ b/b/', '+++ b/' }
    [IO.File]::WriteAllText($patch, (($diff -join "`n") + "`n"), $utf8)
    Remove-Item $base -Recurse -Force
    $files = ($diff | Where-Object { $_ -like 'diff --git*' }).Count
    Write-Output "Patch written: $files file(s) changed from Snowhush $current ($patch)"
    return
}

# ---- the versions the author published
$tags = git ls-remote --tags $repo | ForEach-Object { if ($_ -match 'refs/tags/v(\d+\.\d+\.\d+)$') { [version]$Matches[1] } } | Sort-Object -Unique
if (-not $tags) { throw "Could not read the versions of $repo" }
$latest = $tags | Select-Object -Last 1
Write-Output "Snowhush here: $current. Newest: $latest."
if ($Check) {
    if ([version]$current -lt $latest) { Write-Output "There is a newer version: .\Tools\update-snowhush.ps1 brings it." } else { Write-Output "Up to date." }
    return
}
if (-not $Version) { $Version = "$latest" }
if ($tags -notcontains [version]$Version) { throw "Snowhush has no version $Version" }
if ([version]$Version -le [version]$current) { Write-Output "Already at ${current}: nothing to do."; return }

# ---- the new version, with the DeskPals changes
$work = Join-Path $env:TEMP "snowhush-$Version"
Get-Upstream $Version $work
Push-Location $work
git init -q .
git apply --reject --whitespace=nowarn $patch 2>&1 | Write-Output
$fits = $LASTEXITCODE -eq 0
Pop-Location
if (-not $fits) {
    $rejects = Get-ChildItem $work -Recurse -Filter *.rej | ForEach-Object { $_.FullName.Substring($work.Length + 1) }
    throw ("The DeskPals changes do not fit Snowhush $Version everywhere: " + ($rejects -join ', ') +
        ". The new copy is in $work (the parts that did not fit are in the .rej files; the old copy shows them marked 'DeskPals:'). " +
        "Finish them there, copy the folder over extensions\snowhush-src, build it, then run .\Tools\update-snowhush.ps1 -MakePatch. Nothing here was changed.")
}
Remove-Item (Join-Path $work '.git') -Recurse -Force

# ---- built before it takes the old one's place
foreach ($dir in @('C:\Users\Public\mingw64\bin', (Join-Path $env:USERPROFILE '.cargo\bin'))) { if ((Test-Path $dir) -and ($env:PATH -notlike "*$dir*")) { $env:PATH = "$dir;$env:PATH" } }
if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) { throw "Rust (cargo) not found: see extensions\README.md. The new copy is in $work." }
Push-Location (Join-Path $work 'windows')
cargo build --release --bin Snowhush
$built = $LASTEXITCODE -eq 0
Pop-Location
if (-not $built) { throw "Snowhush $Version with the DeskPals changes does not build (see above). The new copy is in $work; nothing here was changed." }

Remove-Item $here -Recurse -Force
Move-Item $work $here
$text = [IO.File]::ReadAllText($readme)
$text = $text -replace '(\[Snowhush\]\(https://github\.com/mbezruchko/Snowhush\) )v\d+\.\d+\.\d+', "`${1}v$Version"
[IO.File]::WriteAllText($readme, $text, $utf8)
& (Join-Path $PSScriptRoot 'update-snowhush.ps1') -MakePatch
Write-Output ""
Write-Output "Snowhush $Version is in extensions\snowhush-src with the DeskPals changes, and it builds."
Write-Output "Next: build DeskPals Weather, run its tests (DeskPalsWeather.exe --devtests), try it on the screen, and commit."
