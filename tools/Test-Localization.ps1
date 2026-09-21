<#
  Verifies the language dictionaries:
    1. Strings.English.xaml and Strings.Turkish.xaml contain exactly the same keys.
    2. Placeholders ({0}, {1}, ...) match per key.
    3. Every key used from code (Loc.T("...")) or XAML ({DynamicResource ...}) exists.
  Exit code 1 on any problem, so it can run in CI:  powershell -File tools\Test-Localization.ps1
#>
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

function Read-Dictionary([string]$file) {
    $map = @{}
    $xml = [xml](Get-Content -LiteralPath $file -Raw -Encoding utf8)
    $ns = New-Object System.Xml.XmlNamespaceManager $xml.NameTable
    $ns.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
    foreach ($node in $xml.DocumentElement.ChildNodes) {
        if ($node.NodeType -ne 'Element') { continue }
        $key = $node.GetAttribute('Key', 'http://schemas.microsoft.com/winfx/2006/xaml')
        if ($key) { $map[$key] = $node.InnerText }
    }
    return $map
}

$en = Read-Dictionary "$root\Languages\Strings.English.xaml"
$tr = Read-Dictionary "$root\Languages\Strings.Turkish.xaml"
$problems = @()

foreach ($k in $en.Keys | Where-Object { -not $tr.ContainsKey($_) }) { $problems += "Missing in Turkish: $k" }
foreach ($k in $tr.Keys | Where-Object { -not $en.ContainsKey($_) }) { $problems += "Missing in English: $k" }

foreach ($k in $en.Keys | Where-Object { $tr.ContainsKey($_) }) {
    $a = ([regex]::Matches($en[$k], '\{(\d+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique) -join ','
    $b = ([regex]::Matches($tr[$k], '\{(\d+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique) -join ','
    if ($a -ne $b) { $problems += "Placeholder mismatch in '$k': English {$a} vs Turkish {$b}" }
}

# Keys used by code and XAML. Dynamic keys built at run time are listed here explicitly.
$dynamic = @('Nav_Dashboard', 'Nav_System', 'Nav_Network', 'Nav_Processes', 'Nav_Storage', 'Nav_Utilities', 'Nav_Settings')
$used = New-Object System.Collections.Generic.HashSet[string]
$sources = Get-ChildItem -LiteralPath $root -Recurse -Include *.cs, *.xaml -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|dist|Languages)\\' }
foreach ($f in $sources) {
    $text = Get-Content -LiteralPath $f.FullName -Raw -Encoding utf8
    foreach ($m in [regex]::Matches($text, '(?:Loc\.T|LocText\.Of|SetResult)\(\s*"([A-Za-z]+_[A-Za-z0-9]+)"')) { [void]$used.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($text, '"(Mascot_[A-Za-z0-9]+)"')) { [void]$used.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($text, '\{DynamicResource ([A-Za-z]+_[A-Za-z0-9]+)\}')) { [void]$used.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($text, 'Loc\.T\(\s*_?\w+\s*\?\s*"([A-Za-z_]+)"\s*:\s*"([A-Za-z_]+)"')) { [void]$used.Add($m.Groups[1].Value); [void]$used.Add($m.Groups[2].Value) }
}
foreach ($d in $dynamic) { [void]$used.Add($d) }
foreach ($k in $used | Where-Object { -not $en.ContainsKey($_) }) { $problems += "Used but not defined: $k" }

$unused = $en.Keys | Where-Object { -not $used.Contains($_) -and $_ -notlike 'Guard_*' }
if ($unused) { Write-Host "Note: unused keys (harmless): $($unused -join ', ')" -ForegroundColor DarkYellow }

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host "Localization check FAILED ($($problems.Count) problem(s))." -ForegroundColor Red
    exit 1
}
Write-Host "Localization OK: $($en.Count) keys, English and Turkish match." -ForegroundColor Green
