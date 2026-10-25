param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'Не знайдено dotnet. Встанови .NET SDK 10 (репозиторій має також target net10.0).'
    }
    if (-not $SkipBuild) {
        Write-Host '[1/5] Збірка .NET 8...' -ForegroundColor Cyan
        & dotnet build 'src/Cli/Cli.csproj' -f net8.0
        if ($LASTEXITCODE -ne 0) { throw 'Команда dotnet build завершилася з помилкою.' }
    }
    $dll = Join-Path $root 'src/Cli/bin/Debug/net8.0/Cli.dll'
    if (-not (Test-Path -LiteralPath $dll)) { throw "DLL не знайдено: $dll" }

    function Invoke-Cli([string[]]$Options) {
        $lines = & dotnet $dll @Options 2>&1
        $code = $LASTEXITCODE
        $out = $lines | Out-String
        if ($code -ne 0) { throw "Програма завершилася з кодом $code :`n$out" }
        return $out
    }
    function Ensure([bool]$Ok, [string]$Message) {
        if (-not $Ok) { throw "Перевірка провалена: $Message" }
        Write-Host "  [OK] $Message" -ForegroundColor Green
    }

    Write-Host '[2/5] Пам''ять...' -ForegroundColor Cyan
    $mem1 = Invoke-Cli -Options @()
    $mem2 = Invoke-Cli -Options @()
    Ensure ($mem1.Contains('InMemorySensorStore') -and $mem2.Contains('InMemorySensorStore')) 'сховище RAM'
    Ensure ($mem1.Contains('Записів до сценарію: 18') -and $mem2.Contains('Записів до сценарію: 18')) 'RAM скидається після перезапуску'
    Ensure ($mem1.Contains('[OK] Дубль ID') -and $mem1.Contains('[OK] Невідомий ID')) 'обробка двох помилок'

    Write-Host '[3/5] JSON-збереження (тимчасовий файл)...' -ForegroundColor Cyan
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ("crossapp_lab05_" + [guid]::NewGuid().ToString('N') + '.json')
    try {
        $file1 = Invoke-Cli -Options @('--file','--data',$temp)
        $file2 = Invoke-Cli -Options @('--file','--data',$temp)
        Ensure ($file1.Contains('Записів до сценарію: 18') -and $file1.Contains('Записів після сценарію: 19')) 'перший файловий запуск 18 -> 19'
        Ensure ($file2.Contains('Записів до сценарію: 19') -and $file2.Contains('Записів після сценарію: 20')) 'другий файловий запуск 19 -> 20'
        $saved = Get-Content -LiteralPath $temp -Raw -Encoding UTF8 | ConvertFrom-Json
        Ensure ($saved.Count -eq 20) 'JSON містить 20 записів'
        Ensure (($saved | Where-Object { $_.Status -eq 'Active' }).Count -ge 1) 'зберігається статус сенсора'
    }
    finally { Remove-Item -LiteralPath $temp -ErrorAction SilentlyContinue }

    Write-Host '[4/5] Додаткові завдання...' -ForegroundColor Cyan
    $decorated = Invoke-Cli -Options @('--cache','--factory')
    Ensure ($decorated.Contains('CachingSensorStore') -and $decorated.Contains('StoreFactory')) 'кеш + фабрика'
    Ensure ($decorated.Contains('Активних сенсорів:')) 'пошук за предикатом'

    Write-Host '[5/5] Попередня лабораторна...' -ForegroundColor Cyan
    $old = Invoke-Cli -Options @('--lab04')
    Ensure ($old.Contains('Сценарій 1:') -and $old.Contains('Сценарій 4:')) 'сценарії лабораторної №4 доступні'
    Write-Host 'ВСІ ПЕРЕВІРКИ ПРОЙДЕНО' -ForegroundColor Green
}
finally { Pop-Location }


