param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
$project = [System.IO.Path]::GetFullPath($ProjectRoot)
$dll = Join-Path $project 'src/Cli/bin/Debug/net8.0/Cli.dll'
$log = Join-Path $project 'logs/app.log'
$sinkLog = Join-Path $project 'logs/sink.log'

Push-Location $project
try {
    Write-Host '[1/5] Build...'
    & dotnet build src/Cli/Cli.csproj -f net8.0 --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    function Invoke-Lab06([string[]]$Options) {
        $output = (& dotnet $dll --lab06 @Options 2>&1 | Out-String)
        if ($LASTEXITCODE -ne 0) { throw "Lab06 run failed: $output" }
        return $output
    }
    function Assert-Contains([string]$Text, [string]$Pattern) {
        if (-not $Text.Contains($Pattern)) { throw "Missing expected output: $Pattern" }
    }
    function Assert-LineCount([string]$FilePath, [int]$Count) {
        if (-not (Test-Path -LiteralPath $FilePath)) { throw "Missing log: $FilePath" }
        $actual = @(Get-Content -LiteralPath $FilePath -Encoding UTF8).Count
        if ($actual -ne $Count) { throw "Expected $Count lines, got $actual in $FilePath" }
    }

    Write-Host '[2/5] Events, unsubscribe, failure, Func, Action...'
    $a = Invoke-Lab06 -Options @()
    foreach ($s in @('LAB06: events before unsubscribe = 3',
                    'LAB06: failure did not raise event = True',
                    'LAB06: events after unsubscribe = 4',
                    'LAB06: logged lines after unsubscribe = 3',
                    'LAB06: imported = 2, skipped = 2',
                    'LAB06: sinks = disabled')) { Assert-Contains $a $s }
    Assert-LineCount $log 3
    Write-Host '  [OK] Events and app.log'

    Write-Host '[3/5] Optional notification sinks...'
    $b = Invoke-Lab06 -Options @('--sinks', '--cache', '--factory')
    Assert-Contains $b 'LAB06: sinks = enabled'
    Assert-Contains $b '[SINK/CONSOLE]'
    Assert-LineCount $log 3
    Assert-LineCount $sinkLog 4
    Write-Host '  [OK] ConsoleSink + FileSink, manual DI'

    Write-Host '[4/5] File storage survives restart...'
    $file = Join-Path $env:TEMP ('crossapp-lab06-check-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        $c = Invoke-Lab06 -Options @('--file', '--data', $file)
        Assert-Contains $c 'LAB06: events before unsubscribe = 3'
        $first = @((Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json)).Count
        if ($first -ne 19) { throw "First file run: expected 19 sensors, got $first" }
        $d = Invoke-Lab06 -Options @('--file', '--data', $file)
        $second = @((Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json)).Count
        if ($second -ne 20) { throw "Second file run: expected 20 sensors, got $second" }
        Write-Host '  [OK] 18 -> 19 -> 20 sensors'
    } finally {
        Remove-Item -LiteralPath $file -ErrorAction SilentlyContinue
    }

    Write-Host '[5/5] Lab 5 compatibility...'
    $old = (& dotnet $dll 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) { throw "Lab 5 run failed: $old" }
    Assert-Contains $old 'InMemorySensorStore'
    Write-Host '  [OK] Lab 5 still works'
    Write-Host 'ALL LAB06 CHECKS PASSED' -ForegroundColor Green
}
finally {
    Pop-Location
}
