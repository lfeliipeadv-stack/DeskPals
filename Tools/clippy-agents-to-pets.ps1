# Turns the Office assistants of ClippyJS (https://github.com/pithings/clippy, folder src/agents/<name> with
# agent.ts, map.png and sounds-mp3.ts) into pets of the gallery: Pets/<name>/animations.xml.
#
# The assistants only play in place: the pets are "floating" (no gravity, they stay wherever they are dropped).
# Every animation of the agent becomes one animation of the pet (its frames composed from the overlays, each frame
# held as long as in the agent, in steps of 100 ms) and its first sound plays with it. The pet rests on its "rest
# pose" and now and then plays one of the others. The characters, names, art and sounds belong to Microsoft (Bonzi:
# BONZI Software); the code of ClippyJS is MIT.
#
#   pwsh -File Tools\clippy-agents-to-pets.ps1 -Source <folder with the agent folders> [-Only clippy]
param([Parameter(Mandatory)] [string]$Source, [string]$Only = "")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot

# name shown, the owner of the character and a line about it (English; the README.pt-BR has the Portuguese)
$agents = [ordered]@{
  clippy = @{ name = 'Clippy'; owner = 'Microsoft'; what = 'the paperclip assistant of Microsoft Office 97–2003'; pt = 'o clipe assistente do Microsoft Office 97–2003' }
  merlin = @{ name = 'Merlin'; owner = 'Microsoft'; what = 'the wizard of Microsoft Agent'; pt = 'o mago do Microsoft Agent' }
  rover  = @{ name = 'Rover'; owner = 'Microsoft'; what = 'the dog of Windows XP search'; pt = 'o cachorro da pesquisa do Windows XP' }
  peedy  = @{ name = 'Peedy'; owner = 'Microsoft'; what = 'the parrot of Microsoft Agent'; pt = 'o papagaio do Microsoft Agent' }
  genie  = @{ name = 'Genie'; owner = 'Microsoft'; what = 'the genie of Microsoft Agent'; pt = 'o gênio da lâmpada do Microsoft Agent' }
  genius = @{ name = 'Genius'; owner = 'Microsoft'; what = 'the professor assistant of Microsoft Office'; pt = 'o professor assistente do Microsoft Office' }
  f1     = @{ name = 'F1'; owner = 'Microsoft'; what = 'the robot assistant of Microsoft Office'; pt = 'o robô assistente do Microsoft Office' }
  links  = @{ name = 'Links'; owner = 'Microsoft'; what = 'the cat assistant of Microsoft Office'; pt = 'o gato assistente do Microsoft Office' }
  rocky  = @{ name = 'Rocky'; owner = 'Microsoft'; what = 'the dog assistant of Microsoft Office'; pt = 'o cachorro assistente do Microsoft Office' }
  bonzi  = @{ name = 'Bonzi'; owner = 'BONZI Software'; what = 'the purple gorilla of BonziBuddy'; pt = 'o gorila roxo do BonziBuddy' }
}

# ------------------------------------------------------------------ reading the agent

# agent.ts is a JavaScript object: keys get quotes and trailing commas go, then it is JSON
function Read-JsObject([string]$text) {
  $text = $text -replace '^\s*export default\s*', '' -replace ';\s*$', ''
  $sb = New-Object Text.StringBuilder
  $inStr = $false
  for ($i = 0; $i -lt $text.Length; $i++) {
    $c = $text[$i]
    if ($inStr) { [void]$sb.Append($c); if ($c -eq '\') { $i++; [void]$sb.Append($text[$i]) } elseif ($c -eq '"') { $inStr = $false }; continue }
    if ($c -eq '"') { $inStr = $true; [void]$sb.Append($c); continue }
    if ([char]::IsLetterOrDigit($c) -or $c -eq '_') {
      $j = $i; while ($j -lt $text.Length -and ([char]::IsLetterOrDigit($text[$j]) -or $text[$j] -eq '_')) { $j++ }
      $word = $text.Substring($i, $j - $i); $k = $j; while ($k -lt $text.Length -and [char]::IsWhiteSpace($text[$k])) { $k++ }
      if ($k -lt $text.Length -and $text[$k] -eq ':') { [void]$sb.Append('"' + $word + '"') } else { [void]$sb.Append($word) }
      $i = $j - 1; continue
    }
    [void]$sb.Append($c)
  }
  $json = [regex]::Replace($sb.ToString(), ',(\s*[\]}])', '$1')
  return $json | ConvertFrom-Json -AsHashtable
}

function Read-Sounds([string]$file) {
  $s = @{}
  if (-not (Test-Path $file)) { return $s }
  foreach ($m in [regex]::Matches([IO.File]::ReadAllText($file), '"?(\w+)"?\s*:\s*"data:audio/mpeg;base64,([^"]+)"')) { $s[$m.Groups[1].Value] = $m.Groups[2].Value }
  return $s
}

# "LookRight" -> "look right", "Idle1_1" -> "idle 1 1"; a few get the names the program reacts with
$special = @{ Wave = 'wave'; Congratulate = 'happy'; Greeting = 'hello'; Greet = 'hello'; Thinking = 'think'; Sad = 'sad'; Pleased = 'happy 2'; Surprised = 'surprised' }
function Pet-Name([string]$n) {
  if ($special.ContainsKey($n)) { return $special[$n] }
  return (($n -creplace '([a-z])([A-Z0-9])', '$1 $2' -creplace '_', ' ').ToLowerInvariant())
}

# PNG inside an .ico (Windows Vista and later read it): the icon of the pet
function New-Ico([Drawing.Bitmap]$img) {
  $s = 64; $b = New-Object Drawing.Bitmap $s, $s, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [Drawing.Graphics]::FromImage($b); $g.InterpolationMode = 'HighQualityBicubic'
  $k = [Math]::Min($s / $img.Width, $s / $img.Height); $w = [int]($img.Width * $k); $h = [int]($img.Height * $k)
  $g.DrawImage($img, [Drawing.Rectangle]::new([int](($s - $w) / 2), [int](($s - $h) / 2), $w, $h)); $g.Dispose()
  $ms = New-Object IO.MemoryStream; $b.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $png = $ms.ToArray()
  $out = New-Object IO.MemoryStream; $bw = New-Object IO.BinaryWriter $out
  $bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]1)
  $bw.Write([byte]$s); $bw.Write([byte]$s); $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([uint16]1); $bw.Write([uint16]32)
  $bw.Write([uint32]$png.Length); $bw.Write([uint32]22); $bw.Write($png); $bw.Flush()
  return @{ ico = [Convert]::ToBase64String($out.ToArray()); png = $b }
}

function X([string]$s) { [Security.SecurityElement]::Escape($s) }

# one "next" (id, probability) in a list that PowerShell does not flatten
function NextTo([int]$id) { $l = New-Object Collections.Generic.List[object]; $l.Add(@($id, 100)); return ,$l }

# ------------------------------------------------------------------ one agent

foreach ($key in $agents.Keys) {
  if ($Only -and $key -ne $Only) { continue }
  $meta = $agents[$key]
  $dir = Join-Path $Source $key
  if (-not (Test-Path (Join-Path $dir 'agent.ts'))) { Write-Warning "${key}: no agent.ts"; continue }
  $agent = Read-JsObject ([IO.File]::ReadAllText((Join-Path $dir 'agent.ts')))
  $sounds = Read-Sounds (Join-Path $dir 'sounds-mp3.ts')
  $map = [Drawing.Bitmap]::FromFile((Join-Path $dir 'map.png'))
  $fw = [int]$agent.framesize[0]; $fh = [int]$agent.framesize[1]

  # every frame drawn once (the overlays one over the other); the same picture is kept once
  $tiles = New-Object Collections.Generic.List[Drawing.Bitmap]
  $seen = @{}
  $sha = [Security.Cryptography.SHA1]::Create()
  function Tile($frame) {
    $b = New-Object Drawing.Bitmap $fw, $fh, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($b); $g.Clear([Drawing.Color]::Transparent)
    foreach ($im in @($frame.images)) {
      if ($null -eq $im -or $im.Count -lt 2) { continue }
      $g.DrawImage($map, [Drawing.Rectangle]::new(0, 0, $fw, $fh), [Drawing.Rectangle]::new([int]$im[0], [int]$im[1], $fw, $fh), 'Pixel')
    }
    $g.Dispose()
    $ms = New-Object IO.MemoryStream; $b.Save($ms, [Drawing.Imaging.ImageFormat]::Bmp)
    $h = [Convert]::ToHexString($sha.ComputeHash($ms.ToArray()))
    if ($seen.ContainsKey($h)) { $b.Dispose(); return $seen[$h] }
    $seen[$h] = $tiles.Count; $tiles.Add($b); return $seen[$h]
  }

  $names = @($agent.animations.Keys | Sort-Object)
  $rest = if ($names -contains 'RestPose') { 'RestPose' } elseif ($names -contains 'Idle1_1') { 'Idle1_1' } else { $names[0] }
  $restTile = Tile $agent.animations[$rest].frames[0]

  # the animations of the agent: frames (100 ms steps) and the first sound when it starts with the animation
  $anims = New-Object Collections.Generic.List[object]
  foreach ($n in $names) {
    $seq = New-Object Collections.Generic.List[int]
    $sound = $null; $t = 0
    foreach ($f in $agent.animations[$n].frames) {
      $tile = Tile $f
      $d = [int]($f.duration ?? 100)
      $reps = [Math]::Min(30, [Math]::Max(1, [int][Math]::Round($d / 100.0)))
      for ($r = 0; $r -lt $reps; $r++) { $seq.Add($tile) }
      if (-not $sound -and $f.sound -and $t -le 400 -and $sounds.ContainsKey([string]$f.sound)) { $sound = $sounds[[string]$f.sound] }
      $t += $d
    }
    if ($seq.Count -eq 0) { continue }
    $anims.Add(@{ src = $n; name = (Pet-Name $n); frames = $seq; sound = $sound })
  }
  $map.Dispose()

  # the sheet: every picture in a grid (about square)
  $cols = [Math]::Max(1, [int][Math]::Ceiling([Math]::Sqrt($tiles.Count * $fh / [double]$fw)))
  $rows = [int][Math]::Ceiling($tiles.Count / [double]$cols)
  $sheet = New-Object Drawing.Bitmap ($cols * $fw), ($rows * $fh), ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [Drawing.Graphics]::FromImage($sheet); $g.Clear([Drawing.Color]::Transparent)
  for ($i = 0; $i -lt $tiles.Count; $i++) { $g.DrawImageUnscaled($tiles[$i], ($i % $cols) * $fw, [Math]::Floor($i / $cols) * $fh) }
  $g.Dispose()
  $ms = New-Object IO.MemoryStream; $sheet.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $sheet.Dispose()
  $png64 = [Convert]::ToBase64String($ms.ToArray())
  $icon = New-Ico $tiles[$restTile]

  # ids: 1 idle, 2 fall (the still pose after a drop), 3 drag, then the agent's animations
  $used = @{ idle = 1; fall = 1; drag = 1 }
  $id = 4; $byName = @{}
  foreach ($a in $anims) {
    $nm = $a.name; $k = 2; while ($used.ContainsKey($nm)) { $nm = "$($a.name) $k"; $k++ }
    $used[$nm] = 1; $a.name = $nm; $a.id = $id; $byName[$a.src] = $a; $id++
  }
  $show = $byName['Show']; $hide = $byName['Hide']
  if ($hide) { $hide.name = 'kill' }

  $sb = New-Object Text.StringBuilder
  function L([string]$s) { [void]$sb.AppendLine($s) }
  function Anim($aid, $name, $frames, [string]$repeat, $next, [bool]$hidden) {
    L "    <animation id=""$aid""$(if ($hidden) { ' picker="no"' })>"
    L "      <name>$(X $name)</name>"
    foreach ($p in 'start', 'end') { L "      <$p>"; L "        <x>0</x>"; L "        <y>0</y>"; L "        <offsety>0</offsety>"; L "        <opacity>1</opacity>"; L "        <interval>$(if ($name -eq 'idle') { 1000 } else { 100 })</interval>"; L "      </$p>" }
    L "      <sequence repeatfrom=""0"" repeat=""$repeat"">"
    foreach ($f in $frames) { L "        <frame>$f</frame>" }
    foreach ($nx in $next) { L "        <next probability=""$($nx[1])"" only=""none"">$($nx[0])</next>" }
    L "      </sequence>"
    L "    </animation>"
  }

  $info = "$($meta.name), $($meta.what), on your desktop.[br]It stays wherever you drop it (no gravity) and now and then plays one of its $($anims.Count) animations.[br]Character, art and sounds: © $($meta.owner). Converted from ClippyJS (github.com/pithings/clippy). Unofficial, not affiliated with $($meta.owner)."
  L '<?xml version="1.0" encoding="utf-8"?>'
  L '<animations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="https://esheep.petrucci.ch/ https://adrianotiger.github.io/desktopPet/Resources/animations.xsd" xmlns="https://esheep.petrucci.ch/">'
  L '  <header>'
  L "    <author>$(X $meta.owner)</author>"
  L "    <title>$(X $meta.name)</title>"
  L "    <petname>$(X $meta.name)</petname>"
  L '    <version>1.0</version>'
  L "    <info>$(X $info)</info>"
  L '    <application>1</application>'
  L '    <floating>yes</floating>'
  L "    <icon><![CDATA[$($icon.ico)]]></icon>"
  L '  </header>'
  L '  <image>'
  L "    <tilesx>$cols</tilesx>"
  L "    <tilesy>$rows</tilesy>"
  L "    <png><![CDATA[$png64]]></png>"
  L '    <transparency>Magenta</transparency>'
  L '  </image>'
  L '  <spawns>'
  L "    <spawn id=""1"" probability=""100"">"
  L '      <x>random*(screenW-imageW-80)/100+40</x>'
  L '      <y>random*(areaH-imageH-120)/100+60</y>'
  L "      <next>$(if ($show) { $show.id } else { 1 })</next>"
  L '    </spawn>'
  L '  </spawns>'
  L '  <animations>'
  # idle: the rest pose for a few seconds, then another animation (the idle ones more often)
  $next = New-Object Collections.Generic.List[object]
  $next.Add(@(1, 30))
  foreach ($a in $anims) {
    if ($a.name -eq 'kill' -or $a -eq $show) { continue }
    $w = if ($a.src -match '^(Idle|Look|Blink|RestPose)') { 6 } else { 2 }
    $next.Add(@($a.id, $w))
  }
  Anim 1 'idle' @($restTile) 'random/10+3' $next $false
  Anim 2 'fall' @($restTile) '0' (NextTo 1) $true
  Anim 3 'drag' @($restTile) '40' (NextTo 3) $true
  foreach ($a in $anims) {
    $nx = if ($a.name -eq 'kill') { New-Object Collections.Generic.List[object] } else { NextTo 1 }   # the goodbye has no next: the pet fades and closes
    Anim $a.id $a.name $a.frames '0' $nx ($a.name -eq 'kill' -or $a -eq $show)
  }
  L '  </animations>'
  L '  <childs />'
  L '  <sounds>'
  foreach ($a in $anims) {
    if (-not $a.sound) { continue }
    L "    <sound animationid=""$($a.id)"">"
    L '      <probability>100</probability>'
    L "      <base64>$($a.sound)</base64>"
    L '    </sound>'
  }
  L '  </sounds>'
  L '</animations>'

  $out = Join-Path $root "Pets\$key"
  New-Item -ItemType Directory -Force $out | Out-Null
  $utf8 = New-Object Text.UTF8Encoding $false
  [IO.File]::WriteAllText((Join-Path $out 'animations.xml'), $sb.ToString().Replace("`r`n", "`n"), $utf8)
  $icon.png.Save((Join-Path $out 'icon.png'), [Drawing.Imaging.ImageFormat]::Png); $icon.png.Dispose()
  $credit = "Character, art and sounds © $($meta.owner). Converted from [ClippyJS](https://github.com/pithings/clippy) (its code is MIT; the agents are not covered by it). Unofficial: not affiliated with or endorsed by $($meta.owner)."
  [IO.File]::WriteAllText((Join-Path $out 'README.md'), "# $($meta.name)`n`n## Description`n`n$($meta.name), $($meta.what), on your desktop. It stays wherever you drop it (no gravity) and now and then plays one of its $($anims.Count) animations, with the original sounds.`n`n## Credits`n`n$credit`n", $utf8)
  $creditPt = "Personagem, arte e sons © $($meta.owner). Convertido do [ClippyJS](https://github.com/pithings/clippy) (o código dele é MIT; os personagens não entram nessa licença). Não oficial: sem vínculo com a $($meta.owner)."
  [IO.File]::WriteAllText((Join-Path $out 'README.pt-BR.md'), "# $($meta.name)`n`n## Descrição`n`n$($meta.name), $($meta.pt), na sua área de trabalho. Ele fica onde você soltar (sem gravidade) e de vez em quando faz uma das suas $($anims.Count) animações, com os sons originais.`n`n## Créditos`n`n$creditPt`n", $utf8)
  foreach ($t in $tiles) { $t.Dispose() }
  "{0,-7} {1} animations, {2} pictures ({3}x{4}), {5} sounds, {6:N0} KB" -f $key, ($anims.Count + 3), $tiles.Count, $cols, $rows, @($anims | Where-Object { $_.sound }).Count, ((Get-Item (Join-Path $out 'animations.xml')).Length / 1KB)
}
