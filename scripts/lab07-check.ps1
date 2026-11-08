param([switch]$SkipBuild, [switch]$SkipLegacy)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll = Join-Path $root 'src/Cli/bin/Debug/net8.0/Cli.dll'
Push-Location $root
try {
    function Check([bool]$condition, [string]$message) {
        if (-not $condition) { throw "LAB07 FAILED: $message" }
        Write-Host "  [OK] $message" -ForegroundColor Green
    }
    function Invoke-Report([string[]]$Options) {
        $output = (& dotnet $dll @Options 2>&1 | Out-String)
        if ($LASTEXITCODE -ne 0) { throw "Lab 7 command failed: $($Options -join ' ')`n$output" }
        return $output
    }
    Write-Host '[1/9] Build' -ForegroundColor Cyan
    if (-not $SkipBuild) {
        & dotnet build src/Cli/Cli.csproj -f net8.0 --nologo
        if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
    }
    Check (Test-Path -LiteralPath $dll) 'Cli.dll exists'

    Write-Host '[2/9] Four required reports + stable Top-N' -ForegroundColor Cyan
    $top1 = Invoke-Report -Options @('report-top')
    $top2 = Invoke-Report -Options @('report-top')
    Check ($top1 -ceq $top2) 'report-top stable between runs'
    Check ($top1.Contains('LAB07: rows=5')) 'report-top prints 5 DTO rows'
    $low = Invoke-Report -Options @('report-low')
    Check ($low.Contains('LAB07: rows=3')) 'report-low prints 3 matching sensors'
    $groups = Invoke-Report -Options @('report-groups')
    Check ($groups.Contains('LAB07: groups=4')) 'report-groups prints 4 groups'
    $sum = Invoke-Report -Options @('report-sum')
    Check ($sum.Contains('LAB07: totals items=18')) 'report-sum prints totals'

    Write-Host '[3/9] Deferred execution and snapshot' -ForegroundColor Cyan
    $deferred = Invoke-Report -Options @('report-deferred')
    Check ($deferred.Contains('LAB07: deferred before=3 after=4 snapshot=4 after-clear=4')) `
        'Count sees mutation, ToList snapshot is stable'

    Write-Host '[4/9] Optional 1: GroupJoin' -ForegroundColor Cyan
    $join = Invoke-Report -Options @('report-join')
    Check ($join.Contains('LAB07: join rows=18')) 'GroupJoin preserves unmatched sensors'
    Check ($join.Contains('without readings=')) 'Unmatched sensor count printed'

    Write-Host '[5/9] Optional 2 and 3: TopBy + query syntax' -ForegroundColor Cyan
    $topReadings = Invoke-Report -Options @('report-top-readings')
    Check ($topReadings.Contains('S-001') -and $topReadings.Contains('S-002')) 'Second generic TopBy<T> use'
    $query = Invoke-Report -Options @('report-query')
    Check ($query.Contains('LAB07: query syntax equals method syntax = True')) `
        'from/group/into equals method syntax'

    Write-Host '[6/9] Optional 4: Stopwatch on 100000 objects' -ForegroundColor Cyan
    $bench = Invoke-Report -Options @('report-bench')
    Check ($bench.Contains('LAB07: benchmark records=100000')) '100000 generated records'
    Check ($bench.Contains('Without middle ToList:') -and
           $bench.Contains('With middle ToList:')) 'Both timings measured'
    Check ($bench.Contains('LAB07: benchmark same-results=True')) 'Both variants return same rows'

    Write-Host '[7/9] Core correctness, DTO and empty-source assertions' -ForegroundColor Cyan
    $self = Invoke-Report -Options @('report-selftest')
    Check ($self.Contains('LAB07: all assertions passed')) 'Core self-tests pass'

    Write-Host '[8/9] --file option does not become a report command' -ForegroundColor Cyan
    $tempFile = Join-Path ([IO.Path]::GetTempPath()) ('crossapp_lab07_' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        $fileReport = Invoke-Report -Options @('--file', '--data', $tempFile, 'report-groups')
        Check ($fileReport.Contains('LAB07: groups=4')) 'Reports work with file-backed store'
        Check (Test-Path -LiteralPath $tempFile) 'JSON file created'
        $json = Get-Content -LiteralPath $tempFile -Raw -Encoding UTF8 | ConvertFrom-Json
        Check ($json.Count -eq 18) 'File store keeps 18 records'
    }
    finally { Remove-Item -LiteralPath $tempFile -ErrorAction SilentlyContinue }

    Write-Host '[9/9] Compatibility with previous labs' -ForegroundColor Cyan
    if (-not $SkipLegacy) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab05-check.ps1 -SkipBuild
        if ($LASTEXITCODE -ne 0) { throw 'Lab05 compatibility failed' }
        & powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab06-check.ps1
        if ($LASTEXITCODE -ne 0) { throw 'Lab06 compatibility failed' }
    } else {
        Write-Host '  [SKIPPED] Previous lab checks (-SkipLegacy)'
    }
    Write-Host 'ALL LAB07 CHECKS PASSED' -ForegroundColor Green
}
finally { Pop-Location }
