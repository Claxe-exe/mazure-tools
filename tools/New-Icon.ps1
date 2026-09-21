<#
  Renders Mazu (tools\mazu-logo.xaml) into Resources\mazure-logo.png and a multi-size Resources\mazure.ico.
  Run it after changing the drawing:   powershell -STA -ExecutionPolicy Bypass -File tools\New-Icon.ps1
  (WPF needs a single-threaded apartment, hence -STA.)
#>
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    & powershell -NoProfile -STA -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Path
    exit $LASTEXITCODE
}

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$res = Join-Path $PSScriptRoot '..\Resources'
$xamlPath = Join-Path $PSScriptRoot 'mazu-logo.xaml'

function New-Render([int]$size) {
    # Fresh copy per size (a visual can have only one parent).
    $art = [System.Windows.Markup.XamlReader]::Parse((Get-Content -LiteralPath $xamlPath -Raw -Encoding utf8))

    $card = New-Object System.Windows.Controls.Border
    $card.Width = $size; $card.Height = $size
    $card.CornerRadius = New-Object System.Windows.CornerRadius ($size * 0.22)
    $bg = New-Object System.Windows.Media.LinearGradientBrush ([System.Windows.Media.Color]::FromRgb(224, 242, 254)), ([System.Windows.Media.Color]::FromRgb(186, 230, 253)), 90
    $card.Background = $bg

    $box = New-Object System.Windows.Controls.Viewbox
    $box.Stretch = 'Uniform'
    $box.Margin = New-Object System.Windows.Thickness ($size * 0.10)
    $box.Child = $art
    $card.Child = $box

    $card.Measure((New-Object System.Windows.Size $size, $size))
    $card.Arrange((New-Object System.Windows.Rect 0, 0, $size, $size))
    $card.UpdateLayout()

    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($card)
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $ms = New-Object System.IO.MemoryStream
    $enc.Save($ms)
    return , $ms.ToArray()
}

# --- logo used in the window, sidebar and Settings
[System.IO.File]::WriteAllBytes((Join-Path $res 'mazure-logo.png'), (New-Render 256))

# --- multi-size ico (PNG-compressed entries)
$sizes = 16, 24, 32, 48, 64, 128, 256
$images = foreach ($s in $sizes) { , (New-Render $s) }
$out = Join-Path $res 'mazure.ico'
$fs = [System.IO.File]::Create($out); $bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$images[$i].Length); $bw.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $bw.Write($img) }
$bw.Dispose(); $fs.Dispose()
Write-Host "Wrote $out and mazure-logo.png"
