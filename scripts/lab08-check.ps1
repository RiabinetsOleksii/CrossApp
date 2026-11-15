param([switch]$SkipCoverage, [switch]$SkipLegacy)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    function Check([bool]$yes, [string]$label) {
        if (-not $yes) { throw "LAB08 FAILED: $label" }
        Write-Host "  [OK] $label" -ForegroundColor Green
    }
    function Run-Lab08([string[]]$Options) {
        $output = (& dotnet 'src/Cli/bin/Debug/net8.0/Cli.dll' --lab08 @Options 2>&1 | Out-String)
        if ($LASTEXITCODE -ne 0) { throw "Lab08 command failed: $($Options -join ' ')`n$output" }
        return $output
    }
    Write-Host '[1/9] Build Core and Cli' -ForegroundColor Cyan
    & dotnet build src/Cli/Cli.csproj -f net8.0 --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Cli build failed' }
    Check (Test-Path 'src/Cli/bin/Debug/net8.0/Cli.dll') 'Cli.dll built'

    Write-Host '[2/9] Real async JSON import and event' -ForegroundColor Cyan
    $regular = Run-Lab08 -Options @('import-async')
    Check ($regular.Contains('LAB08: added=4, events=4, stored=4')) 'Four sensors added; four events'

    Write-Host '[3/9] Optional 1: streaming await foreach' -ForegroundColor Cyan
    $streamed = Run-Lab08 -Options @('import-stream')
    Check ($streamed.Contains('LAB08: added=4, events=4, stored=4')) 'Streaming import has same result'

    Write-Host '[4/9] Optional 2: linked tokens and timeout' -ForegroundColor Cyan
    $timed = Run-Lab08 -Options @('import-timeout')
    Check ($timed.Contains('LAB08: timeout-fired=True, user-cancelled=False')) 'Timeout (not user) cancelled import'
    $cancelled = Run-Lab08 -Options @('import-cancel')
    Check ($cancelled.Contains('LAB08: timeout-fired=False, user-cancelled=True')) 'Explicit cancellation is handled'

    Write-Host '[5/9] Optional 3: progress percentages' -ForegroundColor Cyan
    $progress = Run-Lab08 -Options @('import-progress')
    Check ($progress.Contains('LAB08: progress=100%')) 'IProgress<int> reaches 100 percent'

    Write-Host '[6/9] Domain failure vs infrastructure failure' -ForegroundColor Cyan
    $bad = Run-Lab08 -Options @('import-invalid')
    Check ($bad.Contains('LAB08: mode=import-invalid') -and $bad.Contains('2:') -and -not $bad.Contains('LAB08: added=')) 'Invalid domain row reports row number'

    Write-Host '[7/9] xUnit tests (repeatability)' -ForegroundColor Cyan
    & dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0 --nologo
    if ($LASTEXITCODE -ne 0) { throw 'xUnit test run #1 failed' }
    & dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0 --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw 'xUnit test run #2 failed' }
    Check $true 'xUnit passes twice'

    Write-Host '[8/9] Optional 4: XPlat Code Coverage' -ForegroundColor Cyan
    if (-not $SkipCoverage) {
        & dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0 --collect:'XPlat Code Coverage' `
            --results-directory artifacts/test-results --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Coverage collection failed' }
        $coverage = Get-ChildItem -Path 'artifacts/test-results' -Filter 'coverage.cobertura.xml' `
            -Recurse -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        Check ($null -ne $coverage) 'coverage.cobertura.xml produced'
        [xml]$doc = Get-Content -LiteralPath $coverage.FullName -Raw
        $percent = [math]::Round(([double]$doc.coverage.'line-rate') * 100, 2)
        Write-Host "  LAB08: overall measured line coverage = $percent%" -ForegroundColor Yellow
        Write-Host "  Coverage file: $($coverage.FullName)"
    } else { Write-Host '  [SKIPPED] Coverage' }

    Write-Host '[9/9] File store idempotency and legacy smoke' -ForegroundColor Cyan
    $temp = Join-Path ([IO.Path]::GetTempPath()) ('crossapp_lab08_' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        $first = Run-Lab08 -Options @('import-async', '--file', '--data', $temp)
        $second = Run-Lab08 -Options @('import-async', '--file', '--data', $temp)
        Check ($first.Contains('LAB08: added=4, events=4, stored=4')) 'First file import adds records'
        Check ($second.Contains('LAB08: added=0, events=0, stored=4')) 'Repeated import is idempotent'
    } finally { Remove-Item -LiteralPath $temp -ErrorAction SilentlyContinue }
    $report = (& dotnet 'src/Cli/bin/Debug/net8.0/Cli.dll' report-sum 2>&1 | Out-String)
    Check ($LASTEXITCODE -eq 0 -and $report.Contains('LAB07: totals items=18')) 'Lab07 command still works'
    if (-not $SkipLegacy) {
        Write-Host '  You can separately run the pre-existing lab05/06/07-check.ps1 scripts.'
    }
    Write-Host 'ALL LAB08 CHECKS PASSED' -ForegroundColor Green
}
finally { Pop-Location }
