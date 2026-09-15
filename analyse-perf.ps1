param(
    [Parameter(Mandatory = $true)]
    [string]$LogPath,

    [string]$CsvPath
)

if (-not (Test-Path $LogPath)) {
    Write-Error "Log file not found: $LogPath"
    exit 1
}

$samples = Get-Content $LogPath |
    Select-String -Pattern 'PERF\|' |
    ForEach-Object {
        $payload = ($_.Line -split 'PERF\|')[1]
        $parts = $payload -split '\|'
        if ($parts.Count -ge 4) {
            [PSCustomObject]@{
                Method = $parts[0].Trim()
                Path   = $parts[1].Trim()
                Status = $parts[2].Trim()
                Ms     = [int]$parts[3].Trim()
            }
        }
    }

if (-not $samples) {
    Write-Warning "No PERF| lines found. Confirm the deployed build includes the timing middleware and that traffic was generated after deployment."
    exit 0
}

Write-Host ""
Write-Host ("Parsed {0} timed requests from {1}" -f $samples.Count, (Split-Path $LogPath -Leaf))
Write-Host ""

$results = $samples |
    Group-Object Path |
    ForEach-Object {
        $times = $_.Group.Ms | Sort-Object
        $p95Index = [math]::Min([int][math]::Ceiling($times.Count * 0.95) - 1, $times.Count - 1)
        if ($p95Index -lt 0) { $p95Index = 0 }

        [PSCustomObject]@{
            Path   = $_.Name
            Count  = $_.Count
            AvgMs  = [math]::Round(($times | Measure-Object -Average).Average, 1)
            MinMs  = $times[0]
            P95Ms  = $times[$p95Index]
            MaxMs  = $times[-1]
        }
    } |
    Sort-Object AvgMs -Descending

$results | Format-Table -AutoSize

$overall = $samples.Ms | Measure-Object -Average -Minimum -Maximum
Write-Host ("Overall: {0} requests, avg {1} ms, min {2} ms, max {3} ms" -f `
    $samples.Count,
    [math]::Round($overall.Average, 1),
    $overall.Minimum,
    $overall.Maximum)
Write-Host ""

if ($CsvPath) {
    $results | Export-Csv -Path $CsvPath -NoTypeInformation -Encoding utf8
    Write-Host "Written to $CsvPath"
}
