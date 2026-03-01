$flagsDir = "C:\Users\zinal\Desktop\MiniCurrency_Final\assets\flags-source-svg"
$mapJson  = "C:\Users\zinal\Desktop\MiniCurrency_Final\data\flags-map.json"

# true = show planned actions only
# false = do rename
$dryRun = $false

if (!(Test-Path $flagsDir)) { throw "Flags folder not found: $flagsDir" }
if (!(Test-Path $mapJson))  { throw "Mapping json not found: $mapJson" }

$map = Get-Content $mapJson -Raw | ConvertFrom-Json
if ($null -eq $map) { throw "Failed to read JSON mapping (empty/null)" }

function Normalize-Name([string]$s) {
  if ($null -eq $s) { return "" }
  $s = $s.Trim().ToLowerInvariant()
  $s = ($s -replace '\s+', ' ')
  return $s
}

$files = Get-ChildItem -Path $flagsDir -Filter "*.svg" -File
$index = @{}
foreach ($f in $files) {
  $key = Normalize-Name($f.BaseName)
  if (-not $index.ContainsKey($key)) { $index[$key] = @() }
  $index[$key] += $f
}

$actions = @()

foreach ($row in $map) {
  $code = ($row.currency_code + "").Trim().ToUpperInvariant()
  $srcName = Normalize-Name($row.svg_filename + "")

  if ([string]::IsNullOrWhiteSpace($code) -or [string]::IsNullOrWhiteSpace($srcName)) {
    Write-Warning "Skip row with empty values: $($row | ConvertTo-Json -Compress)"
    continue
  }

  if (-not $index.ContainsKey($srcName)) {
    Write-Warning "Source SVG not found: '$($row.svg_filename).svg'"
    continue
  }

  foreach ($file in $index[$srcName]) {
    $targetPath = Join-Path $flagsDir ("$code.svg")

    if (Test-Path $targetPath) {
      Write-Warning "Target exists, skip: $targetPath (from $($file.Name))"
      continue
    }

    $actions += [PSCustomObject]@{
      From = $file.FullName
      To   = $targetPath
    }
  }
}

if ($actions.Count -eq 0) {
  Write-Host "Nothing to rename (0 actions)."
  exit
}

Write-Host "Planned renames: $($actions.Count)"
$actions | Format-Table -AutoSize

if ($dryRun) {
  Write-Host "`nDRY RUN = true. Set `$dryRun = `$false to apply."
  exit
}

foreach ($a in $actions) {
  Rename-Item -Path $a.From -NewName ([System.IO.Path]::GetFileName($a.To))
}

Write-Host "Done. Renamed: $($actions.Count)"