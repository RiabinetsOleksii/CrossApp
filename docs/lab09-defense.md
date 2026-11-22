# Лабораторна №9 — покроковий захист

## Початок

«Тема лабораторної №9 — шляхи, JSON, конфігурація та secrets. У CrossApp я переніс налаштування шляхів у appsettings, додав профілі Development і Production, env-перевизначення, атомарний JSON і user-secrets. Працював із сенсорами, а не з товарами з прикладу методички».

## 1. Показати файли конфігурації

Відкрити `src/Cli/appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`, `src/Cli/Cli.csproj`.

Сказати: «У базовому файлі зберігаються каталог, назва JSON і рівень логування. Development вмикає Debug, Production — Warning. Паролів у цих файлах немає. У csproj задане копіювання налаштувань під час збірки».

## 2. Показати шляхи та опції

Відкрити `src/Core/Configuration/AppOptions.cs`, знайти `ResolveFullPath`.

Сказати: «Шлях збирається через Path.Combine, а не конкатенацією. У Cli конфігурація читається від AppContext.BaseDirectory, тому поточна папка запуску не змінює шлях до даних».

```powershell
cd C:\Users\rabin\CrossApp
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 config
cd src/Cli
dotnet run --project Cli.csproj -f net8.0 -- --lab09 config
cd ../..
```

Порівняти `LAB09: path=`.

## 3. Експорт, імпорт, атомарний запис

Відкрити `src/Core/Storage/FileSensorStore.cs` (метод `Flush`) та `src/Cli/Lab09Demo.cs`.

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 export
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 import
```

Показати JSON з полями `id`, `controllerId`, `model`, `pingIntervalMs`, `location`, `status` і читабельною кирилицею (`Зона 3`).

Сказати: «Спершу формується тимчасовий файл .tmp, потім File.Move замінює основний. Якщо запис не завершиться, старі дані не повинні бути частково переписані. Імпорт використовує валідатор із лабораторної №8».

## 4. Перевизначити шлях через env

```powershell
$env:CROSSAPP_DATA_PATH = "$env:TEMP\lab09-defense"
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 export
Remove-Item Env:CROSSAPP_DATA_PATH
```

Показати нове `LAB09: path=...` та файл в теці `%TEMP%\lab09-defense`.

Сказати: «Зміна шляху не потребує редагування коду. CROSSAPP_DATA_PATH має пріоритет над Storage:DataPath».

## 5. Показати user-secrets

Відкрити `src/Cli/Cli.csproj`, знайти `UserSecretsId`. Показати `src/Cli/Lab09Configuration.cs`, де використано `AddUserSecrets`.

```powershell
dotnet user-secrets set "Integration:ApiKey" "YOUR_LOCAL_DEMO_VALUE" --project src/Cli
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 config
git status --short
```

Сказати: «Ключ записується поза репозиторієм у профілі ОС. Програма показує лише факт наявності та довжину, а не саме значення».

## 6. Запустити автоматичні перевірки

```powershell
dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab09-check.ps1
```

Сказати: «Нові тести перевіряють шляхи, JSON-схему, кирилицю, старий формат і неправильний JSON. Тести лабораторної №8 залишилися в рішенні».

**УВАГА:** якщо Windows Smart App Control блокує `Cli.dll` або `Core.Tests.dll` (0x800711C7), програма не запуститься, хоча збірка завершиться успішно. Для демонстрації можна використати дозволене Linux/.NET-середовище; не називати ці перевірки успішними, поки вони фактично не виконані.

## Висновок

«Я прибрав залежність від абсолютних Windows-шляхів, виніс налаштування в JSON, реалізував профілі, змінні середовища, secrets поза Git та надійний імпорт/експорт IoT-сенсорів».


## Опційна підтримка AddCommandLine — додаткова демонстрація

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- export --Storage:DataPath=lab09-cli-data --Storage:FileName=cli-export.json
```

Скажи: «Параметри з командного рядка перевизначають конфігурацію JSON та `CROSSAPP_`-змінні. Це опційний пункт у методичці №9. Працює також команда `export` без `--lab09`».
