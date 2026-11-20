# .github/readme/banner.png: the banner at the top of the README (1280 × 400). The name, the line under it, the
# chips and a row of pets taken from their preview.gif (Pets/<folder>/preview.gif), standing on the ground.
# Run it again when the pets of the gallery change.
#
#   pwsh -File Tools/make-readme-banner.ps1      (PowerShell 7: the texts have accents)
param(
  [string[]]$Pets = @('esheep64', 'rex', 'pingo', 'boo', 'mago', 'nuvi', 'claude', 'clippy'),
  [string[]]$Chips = @('Windows 10 e 11', 'Grátis e código aberto', '26 pets', '23 idiomas', 'Editor de pets')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$BW = 1280; $BH = 400; $ground = 344

$bmp = New-Object Drawing.Bitmap $BW, $BH
$g = [Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'

# the background: green, a little lighter on the right, with two soft circles
$bg = New-Object Drawing.Drawing2D.LinearGradientBrush ([Drawing.Point]::new(0, 0)), ([Drawing.Point]::new($BW, $BH)), ([Drawing.Color]::FromArgb(255, 12, 64, 40)), ([Drawing.Color]::FromArgb(255, 26, 112, 70))
$g.FillRectangle($bg, 0, 0, $BW, $BH)
$soft = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(18, 255, 255, 255))
$g.FillEllipse($soft, 1010, -60, 360, 360)
$g.FillEllipse($soft, -90, 220, 300, 300)
$g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(10, 255, 255, 255))), 600, 250, 260, 260)
# the ground the pets stand on
$g.FillRectangle((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(70, 0, 30, 18))), 0, $ground, $BW, $BH - $ground)
$g.FillRectangle((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(40, 255, 255, 255))), 0, $ground, $BW, 1)

# the icon of the program
$ico = New-Object Drawing.Icon (Join-Path $root 'src\icon.ico'), 256, 256
$icon = $ico.ToBitmap()
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.DrawImage($icon, 70, 58, 116, 116)
$g.PixelOffsetMode = 'Default'

function Font([string[]]$names, [float]$size, [Drawing.FontStyle]$style) {
  $families = (New-Object Drawing.Text.InstalledFontCollection).Families.Name
  foreach ($n in $names) { if ($families -contains $n) { return New-Object Drawing.Font $n, $size, $style, ([Drawing.GraphicsUnit]::Pixel) } }
  return New-Object Drawing.Font 'Segoe UI', $size, $style, ([Drawing.GraphicsUnit]::Pixel)
}
$white = [Drawing.Brushes]::White
$g.DrawString('Meu Pet', (Font @('Segoe UI Variable Display', 'Segoe UI') 68 'Bold'), $white, 206, 54)
$g.DrawString('Bichinhos que vivem na sua área de trabalho do Windows', (Font @('Segoe UI Variable Text', 'Segoe UI') 25 'Regular'),
  (New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(225, 255, 255, 255))), 212, 140)

# the chips
$chipFont = Font @('Segoe UI Variable Text', 'Segoe UI') 16 'Regular'
$x = 214
foreach ($chip in $Chips) {
  $size = $g.MeasureString($chip, $chipFont)
  $w = [int]$size.Width + 26
  $path = New-Object Drawing.Drawing2D.GraphicsPath
  $r = 14; $y = 190; $h = 30
  $path.AddArc($x, $y, $r * 2, $h, 90, 180); $path.AddArc($x + $w - $r * 2, $y, $r * 2, $h, 270, 180); $path.CloseFigure()
  $g.FillPath((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(48, 255, 255, 255))), $path)
  $g.DrawString($chip, $chipFont, $white, $x + 13, $y + 4)
  $x += $w + 10
}

# the pets: the first frame of each preview, cropped, the same height, standing on the ground
function Crop([Drawing.Bitmap]$b) {
  $minX = $b.Width; $minY = $b.Height; $maxX = -1; $maxY = -1
  for ($yy = 0; $yy -lt $b.Height; $yy++) { for ($xx = 0; $xx -lt $b.Width; $xx++) {
    if ($b.GetPixel($xx, $yy).A -gt 40) { if ($xx -lt $minX) { $minX = $xx }; if ($xx -gt $maxX) { $maxX = $xx }; if ($yy -lt $minY) { $minY = $yy }; if ($yy -gt $maxY) { $maxY = $yy } } } }
  if ($maxX -lt 0) { return $b }
  return $b.Clone([Drawing.Rectangle]::new($minX, $minY, $maxX - $minX + 1, $maxY - $minY + 1), $b.PixelFormat)
}
$slot = ($BW - 140) / $Pets.Count
$i = 0
foreach ($pet in $Pets) {
  $gif = Join-Path $root "Pets\$pet\preview.gif"
  if (-not (Test-Path $gif)) { Write-Warning "no preview for $pet"; continue }
  $raw = [Drawing.Image]::FromFile($gif)
  $first = New-Object Drawing.Bitmap $raw.Width, $raw.Height, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
  ([Drawing.Graphics]::FromImage($first)).DrawImage($raw, 0, 0, $raw.Width, $raw.Height)
  $raw.Dispose()
  $img = Crop $first
  $targetH = if ($pet -eq 'clippy') { 92 } else { 78 }
  $k = $targetH / $img.Height
  if ($img.Width * $k -gt $slot - 14) { $k = ($slot - 14) / $img.Width }
  $w = [int]($img.Width * $k); $h = [int]($img.Height * $k)
  $cx = 70 + $slot * $i + $slot / 2
  $g.InterpolationMode = if ($pet -eq 'clippy') { 'HighQualityBicubic' } else { 'NearestNeighbor' }
  $g.PixelOffsetMode = 'Half'
  # a soft shadow on the ground
  $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(60, 0, 0, 0))), [float]($cx - $w * 0.42), [float]($ground - 5), [float]($w * 0.84), 10)
  $g.DrawImage($img, [int]($cx - $w / 2), $ground - $h + 2, $w, $h)
  $i++
}

$out = Join-Path $root '.github\readme\banner.png'
$bmp.Save($out, [Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
"banner: $out"
