# Лабораторна робота №9

**Тема:** Шляхи, JSON, конфігурація, Secrets.

**Мета:** Забезпечити кросплатформність системи моніторингу IoT-сенсорів, усунути залежність шляхів від поточного каталогу та ОС, запровадити конфігурацію з JSON, профілі та змінні середовища, реалізувати безпечне зберігання secret і атомарний імпорт/експорт колекції.

**Предметна область:** CrossApp, IoT-сенсори. `SensorStorageDto` замість навчального `ProductDto`, `FileSensorStore` замість `FileCatalogStore`.

## Хід роботи

### 1. Переносимі шляхи

Використано `Path.Combine`, `Path.GetFullPath` та `Directory.CreateDirectory`. Шлях до `data/sensors.json` розраховується від кореня застосунку, який визначається за `AppContext.BaseDirectory`, а не за `Environment.CurrentDirectory`. Для опублікованого застосунку без solution передбачено fallback до каталогу збірки.

**Код:** `src/Core/Configuration/AppOptions.cs`, `src/Cli/Program.cs` (`ProjectPaths`).

**Скріншот 1:** метод `StorageOptions.ResolveFullPath` із `Path.Combine`.

### 2. Конфігурація та профілі

Створено `src/Cli/appsettings.json` з `Storage:DataPath`, `Storage:FileName`, `Logging:MinimumLevel`, `EnvironmentName`, профілі Development і Production. Файли копіюються до збірки та публікації через `Cli.csproj`.

**Скріншот 2:** три файли конфігурації та їхні значення.

### 3. Зв'язування опцій і пріоритет джерел

Створено POCO `AppOptions`, `StorageOptions`, `LoggingOptions` у Core. CLI завантажує конфігурацію в порядку: базовий JSON, JSON профілю, user-secrets, змінні середовища `CROSSAPP_`, конфігураційні аргументи CLI. Далі викликається `configuration.Get<AppOptions>()`.

Окремо підтримано `CROSSAPP_DATA_PATH`, яка змінює каталог без редагування програми.

**Скріншот 3:** `src/Cli/Lab09Configuration.cs`: `ConfigurationBuilder`, `AddEnvironmentVariables` та `Get<AppOptions>()`.

### 4. Імпорт/експорт повної колекції сенсорів

Наявний `FileSensorStore` зберігає повний стан `SensorStorageDto` (ID, контролер, модель, інтервал, локація, статус). Запис відбувається через `.tmp` та `File.Move(..., overwrite: true)` у тому самому каталозі. Збережено сумісність зі старим PascalCase JSON. Для читабельності застосовано `JsonNamingPolicy.CamelCase`, `WriteIndented` і `UnsafeRelaxedJsonEscaping`.

**Команди:**

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 export
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 import
```

**Скріншот 4:** `FileSensorStore.Flush` з `.tmp` + `File.Move`.

**Скріншот 5:** вивід `LAB09: exported=...` та `LAB09: imported=...` і відкритий JSON із кирилицею.

### 5. Переносимість та env-перевизначення

Запуск із кореня рішення та каталогу `src/Cli` показує однакове `LAB09: path=...`.

```powershell
$env:CROSSAPP_DATA_PATH = "$env:TEMP\lab09-check"
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 export
Remove-Item Env:CROSSAPP_DATA_PATH
```

**Скріншот 6:** значення `LAB09: path=...` до й після env override та створений файл у новому каталозі.

### 6. User-secrets

Секрет задається локально через `dotnet user-secrets set`, а у Git потрапляє лише `UserSecretsId`. Програма виводить факт наявності та довжину ключа, не показуючи вміст.

```powershell
dotnet user-secrets set "Integration:ApiKey" "YOUR_LOCAL_DEMO_VALUE" --project src/Cli
dotnet user-secrets list --project src/Cli
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab09 config
```

**Скріншот 7:** вивід `ApiKey=present,length=N` і `git status` без secrets.json у списку змін.

### 7. Тести й підсумкова перевірка

До xUnit-проєкту Core.Tests додані тести для формування шляхів, захисту імені файлу, атомарного JSON, читабельної кирилиці, зворотного імпорту та пошкодженого JSON. Скрипт `lab09-check.ps1` перевіряє основні сценарії, профілі, env та запуск з різних каталогів. Існуючі тести лабораторної №8 збережені.

```powershell
dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab09-check.ps1
```

**Скріншот 8:** успішні тести та кінцевий напис `ALL LAB09 CHECKS PASSED` (лише після фактичного запуску).

## Профілі та розташування даних

| Профіль | Рівень логування | Дані |
| --- | --- | --- |
| Development | Debug | `data/sensors.json` від кореня CrossApp або шлях із env |
| Production | Warning | `data/sensors.json` від кореня CrossApp або шлях із env |

**Чому Path.Combine?** Він використовує системні роздільники каталогів і дозволяє не писати жорстко `\\` або `/` в коді. Файл конфігурації читається від директорії збірки, а файл даних — від стабільного кореня проєкту.

**Де user-secrets?** На Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`; на Linux/macOS: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`.


### 8. Додатково: перевірка параметрів із командного рядка

Використано `Microsoft.Extensions.Configuration.CommandLine`. Ключі `--Storage:DataPath=...` і `--Storage:FileName=...` перевизначають `appsettings.json` і звичайні змінні `CROSSAPP_`. Скрипт `lab09-check.ps1` перевіряє це в підкроці 3b. Реалізовано також запуск `export` та `import` без службового прапорця `--lab09`, як у прикладах методички.

**Скріншот 9 (за бажанням):** виклик `export --Storage:DataPath=lab09-cli-data` та створений файл у новому каталозі.

## Висновок

Реалізовано незалежні від ОС шляхи, JSON-профілі, перемикання параметрів через змінні середовища, локальне зберігання секретів поза Git та атомарне JSON-збереження повної колекції IoT-сенсорів. Існуючі контракти Core і функції лабораторної №8 збережено. *Підтвердження результатів тестів вписати лише після запуску на своєму комп'ютері.*
