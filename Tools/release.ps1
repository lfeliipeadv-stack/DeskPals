<#
.SYNOPSIS
    Build a DeskPals release and publish it on GitHub (the source of the in-app updater).

.DESCRIPTION
    0. Updates size and counts of every pet in Pets\pets.json (Tools\update-pets-json.ps1)
    1. Writes the version into src\Properties\AssemblyInfoPortable.cs
    2. Publishes the .NET 10 single-file exe
    3. Packs lang\*.json into lang.zip
    2b. Builds the Pet Editor and DeskPals Weather (with its snow engine, Snowhush), runs their tests and packs them
    4. Writes SHA256SUMS (the updater refuses any file whose hash does not match)
    5. Creates the GitHub release "v<Version>" with the files (and copies under the names before 1.2.0) (needs the GitHub CLI, "gh auth login")

    Commit the version change after the release.

.EXAMPLE
    .\Tools\release.ps1 -Version 2.1.0 -Notes "- New pet: Rex"
.EXAMPLE
    .\Tools\release.ps1 -Version 2.1.0 -NoUpload     # build and check only, nothing is sent to GitHub
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = "",
    [string]$Repo = "lfeliipeadv-stack/DeskPals",
    [switch]$NoUpload
)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 2.1.0" }

$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root 'src'
$out = Join-Path $root "release\$Version"
$utf8 = New-Object System.Text.UTF8Encoding($false)

# 0. gallery: size and counts of every pet in Pets\pets.json (shown before downloading)
& (Join-Path $PSScriptRoot 'update-pets-json.ps1')
$listed = (Get-Content (Join-Path $root 'Pets\pets.json') -Raw | ConvertFrom-Json).pets.folder
$onDisk = Get-ChildItem (Join-Path $root 'Pets') -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'animations.xml') } | ForEach-Object Name
foreach ($f in $onDisk | Where-Object { $listed -notcontains $_ }) { Write-Warning "Pets\$f is not in pets.json (it will not appear in the gallery)" }
foreach ($f in $listed | Where-Object { $onDisk -notcontains $_ }) { throw "pets.json lists '$f', but Pets\$f\animations.xml does not exist" }
if (git -C $root status --porcelain -- Pets/pets.json) { Write-Warning "Pets\pets.json changed: commit and push it, the gallery reads it from GitHub" }

# 1. version
$info = Join-Path $src 'Properties\AssemblyInfoPortable.cs'
$text = [IO.File]::ReadAllText($info)
$text = $text -replace 'AssemblyVersion\("[^"]*"\)', "AssemblyVersion(""$Version.0"")"
$text = $text -replace 'AssemblyFileVersion\("[^"]*"\)', "AssemblyFileVersion(""$Version.0"")"
[IO.File]::WriteAllText($info, $text, $utf8)
Write-Output "Version $Version written to AssemblyInfoPortable.cs"

# 2. exe
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
# built in a folder of its own: a DeskPals running from src\bin (the one used for development) locks its exe there
$binDir = Join-Path $env:TEMP 'deskpals-release-bin\'
dotnet publish (Join-Path $src 'DesktopPet.Net.csproj') -c Release -o $out --nologo "-p:BaseOutputPath=$binDir"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
$exe = Join-Path $out 'DeskPals.exe'
$built = (Get-Item $exe).VersionInfo.FileVersion
if ($built -ne "$Version.0") { throw "The exe reports version $built instead of $Version.0" }

# 2b. "full" exe: .NET inside, nothing to install (bigger). It updates itself to the full exe of the next release.
$fullDir = Join-Path $out 'full'
dotnet publish (Join-Path $src 'DesktopPet.Net.csproj') -c Release -o $fullDir --nologo -p:SelfContained=true "-p:BaseOutputPath=$binDir"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (full) failed" }
$fullExe = Join-Path $out 'DeskPals-completo.exe'
Copy-Item (Join-Path $fullDir 'DeskPals.exe') $fullExe -Force
Remove-Item $fullDir -Recurse -Force

# 2b2. Pet Editor (a separate program): published next to the app, carried by the installer, downloaded by older installs
$editorDir = Join-Path $out 'editor'
dotnet publish (Join-Path $root 'editor\PetEditor.csproj') -c Release -o $editorDir --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (editor) failed" }
$editorExe = Join-Path $out 'DeskPals-Editor.exe'
Copy-Item (Join-Path $editorDir 'DeskPals-Editor.exe') $editorExe -Force
Remove-Item $editorDir -Recurse -Force

# 2b3. Pet Editor checked before it goes out: its own tests, then a picture of every page and window at 100% and
# 150% of zoom. A text or a button cut off (or an error while taking the pictures) stops the release.
function Wait-Editor([string[]]$editorArgs, [string]$what) {
    $p = Start-Process $editorExe -ArgumentList $editorArgs -PassThru
    if (-not $p.WaitForExit(240000)) { Stop-Process -Id $p.Id -Force; throw "Pet Editor: $what did not finish in 4 minutes" }
    return $p.ExitCode
}
$fails = Wait-Editor @("--devtests=`"$(Join-Path $root 'Pets')`"") 'the tests'
if ($fails -ne 0) { throw "Pet Editor: $fails test(s) failed (see $env:TEMP\PetEditor-devtests.txt)" }
$snapPet = Join-Path $root 'Pets\pingo\animations.xml'
foreach ($z in @(@{ Scale = '1'; Size = '1000x660' }, @{ Scale = '1.5'; Size = '1500x990' })) {
    $snap = Join-Path $env:TEMP ("PetEditor-snap-" + $z.Scale)
    if (Test-Path $snap) { Remove-Item $snap -Recurse -Force }
    Wait-Editor @("--snap=`"$snap`"", "--snapsize=$($z.Size)", "--scale=$($z.Scale)", "`"$snapPet`"") "the pictures (zoom $($z.Scale))" | Out-Null
    $cutsFile = Join-Path $snap 'cortes.txt'
    if (-not (Test-Path $cutsFile)) { throw "Pet Editor: the pictures at zoom $($z.Scale) did not finish (see $snap)" }
    $cuts = @(Get-Content $cutsFile -Encoding UTF8 | Where-Object { $_ -and $_ -ne 'nenhum corte' })
    if ($cuts.Count -gt 0) { throw "Pet Editor: texts cut off at zoom $($z.Scale) (pictures in $snap):`n$($cuts -join "`n")" }
    $errFile = Join-Path $snap 'erros.txt'
    if (Test-Path $errFile) { throw "Pet Editor: errors while taking the pictures at zoom $($z.Scale):`n$(Get-Content $errFile -Raw)" }
    Remove-Item $snap -Recurse -Force
}
Write-Output "Pet Editor: tests passed, no text cut off at 100% and 150%"

# 2b4. DeskPals Weather, an extension (a program of its own): DeskPals downloads DeskPals-Weather.zip when the user
# turns it on, and brings its new versions. Its snow engine is Snowhush (PolyForm Noncommercial, extensions\snowhush-src),
# built here with Rust (GNU toolchain; MinGW-w64 in a folder without spaces, see extensions\README.md). Its own tests
# run before it is packed; a failure stops the release.
$weatherDir = Join-Path $out 'weather'
dotnet publish (Join-Path $root 'weather\DeskPalsWeather.csproj') -c Release -o $weatherDir --nologo "-p:Version=$Version"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (DeskPals Weather) failed" }
$snowSrc = Join-Path $root 'extensions\snowhush-src\windows'
foreach ($dir in @('C:\Users\Public\mingw64\bin', (Join-Path $env:USERPROFILE '.cargo\bin'))) { if ((Test-Path $dir) -and ($env:PATH -notlike "*$dir*")) { $env:PATH = "$dir;$env:PATH" } }
if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) { throw "Rust (cargo) not found: it builds the snow engine of DeskPals Weather (see extensions\README.md)" }
Push-Location $snowSrc
cargo build --release --bin Snowhush
$cargoCode = $LASTEXITCODE
Pop-Location
if ($cargoCode -ne 0) { throw "cargo build (Snowhush) failed" }
# Without its manifest (windres missing: see extensions\README.md) Windows refuses the window that keeps the snow and
# the rain behind the windows, and they fall in front of them: such a build is not published.
$snowText = [Text.Encoding]::GetEncoding(28591).GetString([IO.File]::ReadAllBytes((Join-Path $snowSrc 'target\release\Snowhush.exe')))
$manifests = ([regex]::Matches($snowText, '<assembly xmlns=')).Count
if ($snowText -notmatch '8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a' -or $snowText -notmatch 'PerMonitorV2') { throw "Snowhush.exe has no manifest (is windres, from MinGW, on the PATH?): the snow and the rain could not go behind the windows" }
if ($manifests -ne 1) { throw "Snowhush.exe has $manifests manifests (one expected): see extensions\snowhush-src\windows\build.rs" }
Copy-Item (Join-Path $snowSrc 'target\release\Snowhush.exe') (Join-Path $weatherDir 'DeskPals-Snowhush.exe') -Force
Copy-Item (Join-Path $root 'extensions\snowhush-src\LICENSE') (Join-Path $weatherDir 'Snowhush-LICENSE.txt') -Force
[IO.File]::WriteAllText((Join-Path $weatherDir 'NOTICE.txt'), "DeskPals Weather $Version`r`n`r`nSnow, autumn leaves, rain and cherry blossom: Snowhush (DeskPals-Snowhush.exe), changed to run inside DeskPals Weather (see extensions/README.md in the DeskPals repository).`r`nLicense: PolyForm Noncommercial 1.0.0 (Snowhush-LICENSE.txt). Free; it may not be sold.`r`n`r`nRequired Notice: Copyright 2026 Mykhailo Bezruchko (https://github.com/mbezruchko/Snowhush)`r`n`r`nThe rest of DeskPals Weather is part of DeskPals (MIT license). The weather comes from Open-Meteo (open-meteo.com).`r`n", $utf8)
$weatherReport = Join-Path $env:TEMP 'DeskPalsWeather-devtests.txt'
$wp = Start-Process (Join-Path $weatherDir 'DeskPalsWeather.exe') -ArgumentList "--devtests=`"$weatherReport`"" -PassThru
if (-not $wp.WaitForExit(300000)) { Stop-Process -Id $wp.Id -Force; throw "DeskPals Weather: the tests did not finish in 5 minutes" }
if ($wp.ExitCode -ne 0) { throw "DeskPals Weather: $($wp.ExitCode) test(s) failed (see $weatherReport)" }
$weatherZip = Join-Path $out 'DeskPals-Weather.zip'
Compress-Archive -Path (@('DeskPalsWeather.exe', 'DeskPals-Snowhush.exe', 'Snowhush-LICENSE.txt', 'NOTICE.txt') | ForEach-Object { Join-Path $weatherDir $_ }) -DestinationPath $weatherZip -Force
# the snow engine started by the tests may still be closing: it holds its exe in that folder
Get-Process | Where-Object { $_.Path -and $_.Path.StartsWith($weatherDir, [StringComparison]::OrdinalIgnoreCase) } | Stop-Process -Force -ErrorAction SilentlyContinue
for ($try = 0; $try -lt 10; $try++) { try { Remove-Item $weatherDir -Recurse -Force -ErrorAction Stop; break } catch { if ($try -eq 9) { throw }; Start-Sleep -Milliseconds 500 } }
Write-Output "DeskPals Weather: tests passed, packed in DeskPals-Weather.zip"

# 2c. installer (.NET Framework 4.8, comes with Windows): checks the .NET 10 Desktop Runtime, then installs DeskPals.exe (and the Pet Editor)
dotnet build (Join-Path $root 'Tools\Setup\Setup.csproj') -c Release --nologo "-p:AppExe=$exe" "-p:EditorExe=$editorExe" "-p:Version=$Version"
if ($LASTEXITCODE -ne 0) { throw "installer build failed" }
$setupExe = Join-Path $out 'DeskPals-Instalador.exe'
Copy-Item (Join-Path $root 'Tools\Setup\bin\Release\net48\DeskPals-Instalador.exe') $setupExe -Force

# 2d. the same files under their names before 1.2.0 (DesktopPet.exe, DesktopPet-completo.exe, PetEditor.exe,
# DesktopPet-Instalador.exe): an installed 1.0.x or 1.1.x looks for those names to update itself (and to download
# the editor), and old links point to them. Same bytes, in SHA256SUMS too.
$legacyDir = Join-Path $out 'nomes-antigos'
New-Item -ItemType Directory -Force $legacyDir | Out-Null
$legacy = foreach ($pair in @(@($exe, 'DesktopPet.exe'), @($fullExe, 'DesktopPet-completo.exe'), @($editorExe, 'PetEditor.exe'), @($setupExe, 'DesktopPet-Instalador.exe'))) {
    $copy = Join-Path $legacyDir $pair[1]; Copy-Item $pair[0] $copy -Force; $copy
}

# 3. languages
$langZip = Join-Path $out 'lang.zip'
Compress-Archive -Path (Join-Path $src 'lang\*.json') -DestinationPath $langZip -Force

# 4. hashes ("hash  name", LF, like sha256sum)
$sums = foreach ($f in @($exe, $fullExe, $setupExe, $editorExe, $weatherZip, $langZip) + $legacy) {
    (Get-FileHash $f -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + (Split-Path $f -Leaf)
}
$sumsFile = Join-Path $out 'SHA256SUMS'
[IO.File]::WriteAllText($sumsFile, ($sums -join "`n") + "`n", $utf8)
Write-Output ""
Write-Output ($sums -join "`n")
Write-Output ""
Write-Output ("DeskPals.exe: {0:N0} KB" -f ((Get-Item $exe).Length / 1KB))
Write-Output ("DeskPals-completo.exe: {0:N0} KB" -f ((Get-Item $fullExe).Length / 1KB))
Write-Output ("DeskPals-Editor.exe: {0:N0} KB" -f ((Get-Item $editorExe).Length / 1KB))
Write-Output ("DeskPals-Weather.zip: {0:N0} KB" -f ((Get-Item $weatherZip).Length / 1KB))

if ($NoUpload) { Write-Output "Built in $out (not uploaded)."; return }

# 5. GitHub release
if (-not $Notes) { $Notes = "DeskPals $Version" }
$Notes += "`n`n**Qual baixar?**`n- ``DeskPals-Instalador.exe`` (recomendado): verifica se o .NET 10 está no computador, instala se faltar e cria os atalhos.`n- ``DeskPals-completo.exe``: funciona direto, sem instalar nada (arquivo maior).`n- ``DeskPals.exe``: só o programa, precisa do [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).`n- ``DeskPals-Editor.exe``: o Editor de Pets (o instalador já traz; o DeskPals também baixa sozinho na primeira vez que for aberto).`n- ``DeskPals-Weather.zip``: a extensão DeskPals Weather (neve, chuva, neblina, sol, cenas…). Não precisa baixar à mão: o DeskPals baixa quando ela é ligada na aba Extensões.`n`nOs arquivos ``DesktopPet*.exe`` e ``PetEditor.exe`` são os mesmos programas com os nomes de antes: ficam aqui para quem tem uma versão antiga receber a atualização sozinho. Não precisa baixá-los."
# notes go through a file: quotes or line breaks in the text would break the command line
$notesFile = Join-Path $out 'NOTES.md'
[IO.File]::WriteAllText($notesFile, $Notes, $utf8)
gh release create "v$Version" $setupExe $fullExe $exe $editorExe $weatherZip $langZip $sumsFile (Join-Path $root 'LICENSE.md') @legacy --repo $Repo --title "DeskPals $Version" --notes-file $notesFile --target master
if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }
Write-Output "Release v$Version published on https://github.com/$Repo/releases"
