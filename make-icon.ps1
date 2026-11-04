# 양 모양 아이콘(sheep.ico) 생성. 크기별 PNG를 ICO 컨테이너에 담는다
Add-Type -AssemblyName System.Drawing
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sizes = 256, 48, 32, 16

function Draw-Sheep([int]$n) {
    $bmp = New-Object System.Drawing.Bitmap $n, $n
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $n / 64.0
    function E($brush, $x, $y, $w, $h) { $g.FillEllipse($brush, [float]($x*$s), [float]($y*$s), [float]($w*$s), [float]($h*$s)) }

    $dark  = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 45, 40, 48))
    $edge  = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 150, 150, 160))
    $wool  = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 250, 250, 252))
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $pink  = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 240, 160, 175))

    # 다리
    foreach ($x in 16, 24, 36, 44) { $g.FillRectangle($dark, [float]($x*$s), [float](44*$s), [float](5*$s), [float](14*$s)) }

    # 털 뭉치 몸통: 테두리용 원을 먼저 조금 크게, 그 위에 흰 원
    $puffs = @(@(10,22,18),@(18,14,20),@(30,12,20),@(40,18,18),@(42,28,16),@(30,30,20),@(16,30,20),@(8,30,14))
    $pad = [Math]::Max(1.2, 1.6)
    foreach ($p in $puffs) { E $edge ($p[0]-$pad/2) ($p[1]-$pad/2) ($p[2]+$pad) ($p[2]+$pad) }
    foreach ($p in $puffs) { E $wool $p[0] $p[1] $p[2] $p[2] }

    # 머리(왼쪽 앞), 귀, 눈
    E $dark 1 20 9 5       # 귀
    E $dark 2 22 16 20     # 얼굴
    E $wool 4 16 12 9      # 이마 털
    E $white 6 28 4 4; E $dark 7.3 29.3 2 2   # 눈
    E $pink 5 37 6 3       # 코·입
    $g.Dispose()
    return $bmp
}

$entries = foreach ($n in $sizes) {
    $bmp = Draw-Sheep $n
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($n -eq 256) { $bmp.Save("$here\sheep-preview.png", [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    [pscustomobject]@{ Size = $n; Data = $ms.ToArray() }
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$entries.Count)
$offset = 6 + 16 * $entries.Count
foreach ($e in $entries) {
    $d = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $w.Write([byte]$d); $w.Write([byte]$d); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$e.Data.Length); $w.Write([uint32]$offset)
    $offset += $e.Data.Length
}
foreach ($e in $entries) { $w.Write($e.Data) }
$w.Flush()
[System.IO.File]::WriteAllBytes("$here\sheep.ico", $out.ToArray())
"sheep.ico 생성: $($entries.Count)개 크기"
