<#
.SYNOPSIS
    Build a DesktopPet release and publish it on GitHub (the source of the in-app updater).

.DESCRIPTION
    0. Updates size and counts of every pet in Pets\pets.json (Tools\update-pets-json.ps1)
    1. Writes the version into src\Properties\AssemblyInfoPortable.cs
    2. Publishes the .NET 10 single-file exe
    3. Packs lang\*.json into lang.zip
    4. Writes SHA256SUMS (the updater refuses any file whose hash does not match)
    5. Creates the GitHub release "v<Version>" with the three files (needs the GitHub CLI, "gh auth login")

    Commit the version change after the release.

.EXAMPLE
    .\Tools\release.ps1 -Version 2.1.0 -Notes "- New pet: Rex"
.EXAMPLE
    .\Tools\release.ps1 -Version 2.1.0 -NoUpload     # build and check only, nothing is sent to GitHub
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = "",
    [string]$Repo = "lfeliipeadv-stack/Desktop-Pet",
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
dotnet publish (Join-Path $src 'DesktopPet.Net.csproj') -c Release -o $out --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
$exe = Join-Path $out 'DesktopPet.exe'
$built = (Get-Item $exe).VersionInfo.FileVersion
if ($built -ne "$Version.0") { throw "The exe reports version $built instead of $Version.0" }

# 2b. "full" exe: .NET inside, nothing to install (bigger). It updates itself to the full exe of the next release.
$fullDir = Join-Path $out 'full'
dotnet publish (Join-Path $src 'DesktopPet.Net.csproj') -c Release -o $fullDir --nologo -p:SelfContained=true
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (full) failed" }
$fullExe = Join-Path $out 'DesktopPet-completo.exe'
Copy-Item (Join-Path $fullDir 'DesktopPet.exe') $fullExe -Force
Remove-Item $fullDir -Recurse -Force

# 2b2. Pet Editor (a separate program): published next to the app, carried by the installer, downloaded by older installs
$editorDir = Join-Path $out 'editor'
dotnet publish (Join-Path $root 'editor\PetEditor.csproj') -c Release -o $editorDir --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (editor) failed" }
$editorExe = Join-Path $out 'PetEditor.exe'
Copy-Item (Join-Path $editorDir 'PetEditor.exe') $editorExe -Force
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

# 2c. installer (.NET Framework 4.8, comes with Windows): checks the .NET 10 Desktop Runtime, then installs DesktopPet.exe (and the Pet Editor)
dotnet build (Join-Path $root 'Tools\Setup\Setup.csproj') -c Release --nologo "-p:AppExe=$exe" "-p:EditorExe=$editorExe" "-p:Version=$Version"
if ($LASTEXITCODE -ne 0) { throw "installer build failed" }
$setupExe = Join-Path $out 'DesktopPet-Instalador.exe'
Copy-Item (Join-Path $root 'Tools\Setup\bin\Release\net48\DesktopPet-Instalador.exe') $setupExe -Force

# 3. languages
$langZip = Join-Path $out 'lang.zip'
Compress-Archive -Path (Join-Path $src 'lang\*.json') -DestinationPath $langZip -Force

# 4. hashes ("hash  name", LF, like sha256sum)
$sums = foreach ($f in @($exe, $fullExe, $setupExe, $editorExe, $langZip)) {
    (Get-FileHash $f -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + (Split-Path $f -Leaf)
}
$sumsFile = Join-Path $out 'SHA256SUMS'
[IO.File]::WriteAllText($sumsFile, ($sums -join "`n") + "`n", $utf8)
Write-Output ""
Write-Output ($sums -join "`n")
Write-Output ""
Write-Output ("DesktopPet.exe: {0:N0} KB" -f ((Get-Item $exe).Length / 1KB))
Write-Output ("DesktopPet-completo.exe: {0:N0} KB" -f ((Get-Item $fullExe).Length / 1KB))
Write-Output ("PetEditor.exe: {0:N0} KB" -f ((Get-Item $editorExe).Length / 1KB))

if ($NoUpload) { Write-Output "Built in $out (not uploaded)."; return }

# 5. GitHub release
if (-not $Notes) { $Notes = "DesktopPet $Version" }
$Notes += "`n`n**Qual baixar?**`n- ``DesktopPet-Instalador.exe`` (recomendado): verifica se o .NET 10 está no computador, instala se faltar e cria os atalhos.`n- ``DesktopPet-completo.exe``: funciona direto, sem instalar nada (arquivo maior).`n- ``DesktopPet.exe``: só o programa, precisa do [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).`n- ``PetEditor.exe``: o Editor de Pets (o instalador já traz; o DesktopPet também baixa sozinho na primeira vez que for aberto)."
# notes go through a file: quotes or line breaks in the text would break the command line
$notesFile = Join-Path $out 'NOTES.md'
[IO.File]::WriteAllText($notesFile, $Notes, $utf8)
gh release create "v$Version" $setupExe $fullExe $exe $editorExe $langZip $sumsFile (Join-Path $root 'LICENSE.md') --repo $Repo --title "Desktop-Pet $Version" --notes-file $notesFile --target master
if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }
Write-Output "Release v$Version published on https://github.com/$Repo/releases"
