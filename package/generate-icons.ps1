# Generates the Lenovo Ripple app icon at the sizes Windows + MSIX expect.
# Design: dark navy tile, a cyan glowing keyboard "key" centered, with two
# concentric ripple rings expanding from it.
#
# Outputs:
#   ..\Assets\AppIcon.png             (256x256, embedded as the tray + window icon)
#   .\Assets\StoreLogo.png            (50x50,   MSIX manifest)
#   .\Assets\Square44x44Logo.png      (44x44,   MSIX manifest)
#   .\Assets\Square150x150Logo.png    (150x150, MSIX manifest)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedPath {
    param(
        [System.Drawing.RectangleF] $rect,
        [single] $radius
    )
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X,                $rect.Y,                $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d,       $rect.Y,                $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d,       $rect.Bottom - $d,      $d, $d,   0, 90)
    $path.AddArc($rect.X,                $rect.Bottom - $d,      $d, $d,  90, 90)
    $path.CloseFigure()
    return $path
}

function Render-RippleIcon {
    param([int] $size, [string] $outPath)

    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Background — deep navy.
    $g.Clear([System.Drawing.Color]::FromArgb(255, 6, 12, 36))

    # Subtle vignette: a darker bottom-right gradient to add depth.
    $vignette = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF 0, 0),
        (New-Object System.Drawing.PointF $size, $size),
        [System.Drawing.Color]::FromArgb(0,   0,   0,   0),
        [System.Drawing.Color]::FromArgb(120, 0,   0,   0))
    $g.FillRectangle($vignette, 0, 0, $size, $size)
    $vignette.Dispose()

    $cx = $size / 2.0
    $cy = $size / 2.0

    # Ripple rings: outer faintest, inner brightest. Drawn before the key so
    # the key sits on top.
    $rings = @(
        @{ rPct = 0.46; alpha =  45; width = 0.030 },
        @{ rPct = 0.34; alpha = 110; width = 0.034 },
        @{ rPct = 0.24; alpha = 200; width = 0.040 }
    )
    foreach ($ring in $rings) {
        $r = $size * $ring.rPct
        $color = [System.Drawing.Color]::FromArgb($ring.alpha, 0, 220, 255)
        $w = [Math]::Max(1, $size * $ring.width)
        $pen = New-Object System.Drawing.Pen($color, $w)
        $g.DrawEllipse($pen, ($cx - $r), ($cy - $r), $r * 2, $r * 2)
        $pen.Dispose()
    }

    # Soft glow halo behind the key.
    $glowR = $size * 0.20
    $glowBrush = New-Object System.Drawing.SolidBrush(
        [System.Drawing.Color]::FromArgb(110, 0, 220, 255))
    $g.FillEllipse($glowBrush, ($cx - $glowR), ($cy - $glowR), $glowR * 2, $glowR * 2)
    $glowBrush.Dispose()

    # The keyboard "key" — a rounded cyan square, dead center.
    $keyW = $size * 0.30
    $keyH = $size * 0.30
    $keyRadius = $size * 0.06
    $keyRect = New-Object System.Drawing.RectangleF(
        ($cx - $keyW / 2), ($cy - $keyH / 2), $keyW, $keyH)
    $keyPath = New-RoundedPath $keyRect $keyRadius

    $keyFill = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $keyRect,
        [System.Drawing.Color]::FromArgb(255,  90, 240, 255),
        [System.Drawing.Color]::FromArgb(255,   0, 180, 235),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    $g.FillPath($keyFill, $keyPath)
    $keyFill.Dispose()

    # Top highlight on the key (glassy effect).
    $hlInset = $keyW * 0.12
    $hlRect = New-Object System.Drawing.RectangleF(
        ($keyRect.X + $hlInset),
        ($keyRect.Y + $hlInset),
        ($keyW - $hlInset * 2),
        ($keyH * 0.32))
    $hlPath = New-RoundedPath $hlRect ($keyRadius * 0.7)
    $hlBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $hlRect,
        [System.Drawing.Color]::FromArgb(180, 240, 252, 255),
        [System.Drawing.Color]::FromArgb( 30, 200, 240, 255),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    $g.FillPath($hlBrush, $hlPath)
    $hlBrush.Dispose()
    $hlPath.Dispose()

    # Subtle 1px stroke on the key for crispness.
    $stroke = New-Object System.Drawing.Pen(
        [System.Drawing.Color]::FromArgb(220, 200, 245, 255),
        [Math]::Max(1, $size * 0.005))
    $g.DrawPath($stroke, $keyPath)
    $stroke.Dispose()
    $keyPath.Dispose()

    $g.Dispose()

    $dir = Split-Path -Parent $outPath
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "wrote $outPath ($size x $size)"
}

$here = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repo = Split-Path -Parent $here

Render-RippleIcon -size 256 -outPath (Join-Path $repo    "Assets\AppIcon.png")
Render-RippleIcon -size 150 -outPath (Join-Path $here    "Assets\Square150x150Logo.png")
Render-RippleIcon -size  50 -outPath (Join-Path $here    "Assets\StoreLogo.png")
Render-RippleIcon -size  44 -outPath (Join-Path $here    "Assets\Square44x44Logo.png")
