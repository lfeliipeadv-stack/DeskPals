<#
.SYNOPSIS
    Draws the icon of DeskPals Weather (weather\icon.ico): a smiling cloud, like a pet, with the sun behind it and a
    raindrop and a snowflake falling, on a tile of sky. Every size is drawn on its own (the small ones simpler), and
    the .ico keeps them as PNG pictures.
.EXAMPLE
    .\Tools\make-weather-icon.ps1
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'weather\icon.ico'
$png = Join-Path $root 'weather\icon.png'

function New-Rounded([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure(); return $p
}

function New-WeatherIcon([int]$size) {
    $b = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($b)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'
    $s = $size / 256.0
    $small = $size -lt 32
    # the tile: sky from light to deep blue
    $tile = New-Rounded (6 * $s) (6 * $s) (244 * $s) (244 * $s) (56 * $s)
    $sky = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.PointF]::new(0, 0)), ([System.Drawing.PointF]::new(0, $size)), ([System.Drawing.Color]::FromArgb(255, 120, 200, 255)), ([System.Drawing.Color]::FromArgb(255, 52, 108, 214))
    $g.FillPath($sky, $tile)
    # the sun, up on the right, its rays turning
    $sunC = [System.Drawing.PointF]::new(176 * $s, 84 * $s)
    $ray = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 214, 92)), ([float](12 * $s))
    $ray.StartCap = 'Round'; $ray.EndCap = 'Round'
    if (-not $small) {
        for ($i = 0; $i -lt 8; $i++) {
            $a = $i * [Math]::PI / 4 + 0.2
            $g.DrawLine($ray, [float]($sunC.X + [Math]::Cos($a) * 52 * $s), [float]($sunC.Y + [Math]::Sin($a) * 52 * $s), [float]($sunC.X + [Math]::Cos($a) * 70 * $s), [float]($sunC.Y + [Math]::Sin($a) * 70 * $s))
        }
    }
    $sunR = 40 * $s
    $sunB = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.PointF]::new($sunC.X, $sunC.Y - $sunR)), ([System.Drawing.PointF]::new($sunC.X, $sunC.Y + $sunR)), ([System.Drawing.Color]::FromArgb(255, 255, 232, 120)), ([System.Drawing.Color]::FromArgb(255, 255, 168, 40))
    $g.FillEllipse($sunB, $sunC.X - $sunR, $sunC.Y - $sunR, $sunR * 2, $sunR * 2)
    # the cloud: three puffs and a flat bottom, with a soft shadow under it
    $cloud = New-Object System.Drawing.Drawing2D.GraphicsPath ([System.Drawing.Drawing2D.FillMode]::Winding)
    $cloud.AddEllipse(34 * $s, 104 * $s, 82 * $s, 76 * $s)
    $cloud.AddEllipse(72 * $s, 70 * $s, 104 * $s, 104 * $s)
    $cloud.AddEllipse(140 * $s, 108 * $s, 80 * $s, 72 * $s)
    $cloud.AddPath((New-Rounded (50 * $s) (130 * $s) (158 * $s) (52 * $s) (26 * $s)), $false)
    $region = New-Object System.Drawing.Region $cloud
    $shadow = $region.Clone(); $shadow.Translate([float](0), [float](8 * $s))
    $g.FillRegion((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 20, 50, 120))), $shadow)
    $white = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.PointF]::new(0, 70 * $s)), ([System.Drawing.PointF]::new(0, 184 * $s)), ([System.Drawing.Color]::White), ([System.Drawing.Color]::FromArgb(255, 218, 230, 248))
    $g.FillRegion($white, $region)
    # its face: eyes, blush and a little smile (like the pets)
    if (-not $small) {
        $ink = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 40, 52, 84))
        $g.FillEllipse($ink, 98 * $s, 126 * $s, 16 * $s, 20 * $s)
        $g.FillEllipse($ink, 146 * $s, 126 * $s, 16 * $s, 20 * $s)
        $shine = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
        $g.FillEllipse($shine, 102 * $s, 129 * $s, 6 * $s, 6 * $s)
        $g.FillEllipse($shine, 150 * $s, 129 * $s, 6 * $s, 6 * $s)
        $blush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(150, 255, 128, 150))
        $g.FillEllipse($blush, 80 * $s, 148 * $s, 20 * $s, 11 * $s)
        $g.FillEllipse($blush, 160 * $s, 148 * $s, 20 * $s, 11 * $s)
        $smile = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 40, 52, 84)), ([float](6 * $s))
        $smile.StartCap = 'Round'; $smile.EndCap = 'Round'
        $g.DrawArc($smile, 118 * $s, 140 * $s, 24 * $s, 18 * $s, 20, 140)
    }
    # what falls: a raindrop and a snowflake (and a second drop when there is room)
    $drop = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 220, 244, 255))
    function Add-Drop([float]$cx, [float]$cy, [float]$r) {
        $p = New-Object System.Drawing.Drawing2D.GraphicsPath
        $p.AddBezier($cx, $cy - $r * 2.1, $cx + $r * 0.4, $cy - $r * 1.1, $cx + $r, $cy - $r * 0.4, $cx + $r, $cy + $r * 0.1)
        $p.AddArc($cx - $r, $cy - $r * 0.9, $r * 2, $r * 2, 0, 180)
        $p.AddBezier($cx - $r, $cy + $r * 0.1, $cx - $r, $cy - $r * 0.4, $cx - $r * 0.4, $cy - $r * 1.1, $cx, $cy - $r * 2.1)
        $g.FillPath($drop, $p)
    }
    if ($small) { Add-Drop (96 * $s) (214 * $s) (14 * $s); Add-Drop (160 * $s) (214 * $s) (14 * $s) }
    else {
        Add-Drop (86 * $s) (212 * $s) (11 * $s)
        Add-Drop (176 * $s) (220 * $s) (11 * $s)
        $flake = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 240, 250, 255)), ([float](6 * $s))
        $flake.StartCap = 'Round'; $flake.EndCap = 'Round'
        $fc = [System.Drawing.PointF]::new(130 * $s, 214 * $s)
        for ($i = 0; $i -lt 3; $i++) {
            $a = $i * [Math]::PI / 3 + [Math]::PI / 2
            $dx = [float]([Math]::Cos($a) * 16 * $s); $dy = [float]([Math]::Sin($a) * 16 * $s)
            $g.DrawLine($flake, $fc.X - $dx, $fc.Y - $dy, $fc.X + $dx, $fc.Y + $dy)
        }
    }
    $g.Dispose()
    return $b
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 96, 128, 256
$pngs = foreach ($z in $sizes) {
    $bmp = New-WeatherIcon $z
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($z -eq 256) { $bmp.Save($png, [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    , $ms.ToArray()
}
# the .ico: a header, one entry per size, then the PNG pictures
$fs = [System.IO.File]::Create($out)
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $z = $sizes[$i]; $dim = if ($z -ge 256) { 0 } else { $z }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$pngs[$i].Length); $w.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $w.Write($p) }
$w.Dispose()
Write-Output "Icon written to $out (and a 256 px picture to $png)"
