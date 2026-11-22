param([switch]$SkipBuild, [switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testFolder = Join-Path $project ('artifacts/lab09-validation-' + [Guid]::NewGuid().ToString('N'))
$oldSimplePath = [Environment]::GetEnvironmentVariable('CROSSAPP_DATA_PATH', 'Process')
$oldNestedPath = [Environment]::GetEnvironmentVariable('CROSSAPP_Storage__DataPath', 'Process')
$oldFileName = [Environment]::GetEnvironmentVariable('CROSSAPP_Storage__FileName', 'Process')
$oldEnvName = [Environment]::GetEnvironmentVariable('CROSSAPP_ENVIRONMENT', 'Process')
$oldSecret = [Environment]::GetEnvironmentVariable('CROSSAPP_Integration__ApiKey', 'Process')

function Check([bool]$ok, [string]$title) {
    if (-not $ok) { throw "[FAIL] $title" }
    Write-Host "[OK] $title" -ForegroundColor Green
}
function RunCli([string[]]$cliArgs, [string]$workDir) {
    Push-Location $workDir
    try {
        $global:LASTEXITCODE = 0
        $result = & dotnet run --project (Join-Path $project 'src/Cli/Cli.csproj') -f net8.0 -- @cliArgs 2>&1
        if ($LASTEXITCODE -ne 0) { throw "CLI exited $LASTEXITCODE : $($result -join [char]10)" }
        return ($result -join "`n")
    }
    finally { Pop-Location }
}

Write-Host '=== CrossApp LAB09 checks ===' -ForegroundColor Cyan
try {
    if (-not $SkipBuild) {
        Write-Host '[1/9] Compile Core and CLI'
        & dotnet build (Join-Path $project 'src/Cli/Cli.csproj') -f net8.0
        Check ($LASTEXITCODE -eq 0) 'Build net8.0 succeeded'
    }
    else { Write-Host '[1/9] Build skipped' }

    [Environment]::SetEnvironmentVariable('CROSSAPP_DATA_PATH', $null, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_Storage__DataPath', $null, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_Storage__FileName', $null, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_ENVIRONMENT', 'Development', 'Process')

    Write-Host '[2/9] Profiles and appsettings'
    $development = RunCli @('--lab09', 'config') $project
    Check ($development.Contains('LAB09: env=Development; log=Debug')) 'Development profile'
    [Environment]::SetEnvironmentVariable('CROSSAPP_ENVIRONMENT', 'Production', 'Process')
    $production = RunCli @('--lab09', 'config') $project
    Check ($production.Contains('LAB09: env=Production; log=Warning')) 'Production profile'
    [Environment]::SetEnvironmentVariable('CROSSAPP_ENVIRONMENT', 'Development', 'Process')

    Write-Host '[3/9] CROSSAPP_Storage__DataPath / path is stable across cwd'
    [Environment]::SetEnvironmentVariable('CROSSAPP_Storage__DataPath', $testFolder, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_Storage__FileName', 'sensors.json', 'Process')
    $rootView = RunCli @('--lab09', 'config') $project
    $cliView = RunCli @('--lab09', 'config') (Join-Path $project 'src/Cli')
    $rootPath = @($rootView -split "`n" | Where-Object { $_.StartsWith('LAB09: path=') })[0]
    $cliPath = @($cliView -split "`n" | Where-Object { $_.StartsWith('LAB09: path=') })[0]
    Check ($rootPath -eq $cliPath) 'Same data file from solution root and src/Cli'
    Check ($rootPath.Contains('lab09-validation-')) 'Prefixed nested environment variable overrides JSON'

    Write-Host '[3b] Optional CLI override has highest priority among regular providers'
    $cliOverride = Join-Path $testFolder 'cli-argument-override'
    $cliArgs = @('--lab09', 'config', ('--Storage:DataPath=' + $cliOverride), '--Storage:FileName=from-cli.json')
    $cliArgView = RunCli $cliArgs $project
    Check ($cliArgView.Contains('cli-argument-override') -and $cliArgView.Contains('from-cli.json')) 'AddCommandLine overrides JSON and prefixed env settings'
    Check ($cliArgView.Contains('LAB09: env=Development')) 'CLI override keeps the expected profile'

    Write-Host '[4/9] Simple CROSSAPP_DATA_PATH wins over nested setting'
    $legacyOverride = Join-Path $testFolder 'legacy-override'
    [Environment]::SetEnvironmentVariable('CROSSAPP_DATA_PATH', $legacyOverride, 'Process')
    $simpleView = RunCli @('--lab09', 'config') $project
    Check ($simpleView.Contains('legacy-override')) 'Simple environment variable overrides nested setting'
    [Environment]::SetEnvironmentVariable('CROSSAPP_DATA_PATH', $null, 'Process')

    Write-Host '[5/9] Export complete collection to atomic JSON'
    $export = RunCli @('--lab09', 'export') $project
    $dataFile = Join-Path $testFolder 'sensors.json'
    Check ($export.Contains('LAB09: exported=3')) 'Three demonstration sensors exported'
    Check (Test-Path -LiteralPath $dataFile) 'JSON file created in configured location'
    $json = [IO.File]::ReadAllText($dataFile, [Text.Encoding]::UTF8)
    Check ($json.Contains('"id"') -and $json.Contains('Зона 3')) 'camelCase and readable Cyrillic'
    Check (-not (Test-Path -LiteralPath ($dataFile + '.tmp'))) 'No leftover temporary file'
    $again = RunCli @('export') $project
    Check ($again.Contains('LAB09: exported=3')) 'Repeated export is idempotent'

    Write-Host '[6/9] Re-import exported JSON (Lab 8 importer reuse)'
    $import = RunCli @('import') (Join-Path $project 'src/Cli')
    Check ($import.Contains('LAB09: imported=3; stored=3')) 'All exported sensors are imported'
    Check ($import.Contains('LAB09: path=')) 'Bare import command runs from src/Cli with stable config path'

    Write-Host '[7/9] Technical failure for malformed JSON'
    [IO.File]::WriteAllText($dataFile, '{not json', [Text.Encoding]::UTF8)
    Push-Location $project
    try {
        $global:LASTEXITCODE = 0
        # PowerShell 5.1 treats native stderr as an error record when Stop is active.
        $ErrorActionPreference = 'Continue'
        $badResult = & dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 import 2>&1
        $badExit = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = 'Stop'
        Pop-Location
    }
    Check ($badExit -ne 0 -and (($badResult -join "`n").Contains('LAB09: invalid-json'))) 'Invalid JSON handled without unhandled stack trace'
    Remove-Item -LiteralPath $dataFile -Force

    Write-Host '[8/9] Secret presence without leaking its value'
    [Environment]::SetEnvironmentVariable('CROSSAPP_Integration__ApiKey', 'lab09-secret-for-check', 'Process')
    $secretView = RunCli @('--lab09', 'config') $project
    Check ($secretView.Contains('LAB09: ApiKey=present,length=22')) 'Secret presence is reported'
    Check (-not $secretView.Contains('lab09-secret-for-check')) 'Secret value is never printed'
    [Environment]::SetEnvironmentVariable('CROSSAPP_Integration__ApiKey', $null, 'Process')

    Write-Host '[9/9] Unit tests (Lab 8 and new Lab 9)'
    if (-not $SkipTests) {
        & dotnet test (Join-Path $project 'tests/Core.Tests/Core.Tests.csproj') -f net8.0
        Check ($LASTEXITCODE -eq 0) 'All xUnit tests passed'
    }
    else { Write-Host '[9/9] Unit tests skipped by -SkipTests' }

    Write-Host 'ALL LAB09 CHECKS PASSED' -ForegroundColor Green
}
finally {
    [Environment]::SetEnvironmentVariable('CROSSAPP_DATA_PATH', $oldSimplePath, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_Storage__DataPath', $oldNestedPath, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_Storage__FileName', $oldFileName, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_ENVIRONMENT', $oldEnvName, 'Process')
    [Environment]::SetEnvironmentVariable('CROSSAPP_Integration__ApiKey', $oldSecret, 'Process')
    if (Test-Path -LiteralPath $testFolder) { Remove-Item -LiteralPath $testFolder -Recurse -Force }
}
