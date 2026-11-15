# CrossApp — Практикум з крос-платформного програмування

**Студент:** Рябінець Олексій, група ФЕІ-35  
**Семестровий проєкт:** Наскрізна розробка системи моніторингу IoT-пристроїв (тижні 1–16).

---

## 1. Предметна область (Domain)

* **Назва:** Система моніторингу IoT-пристроїв.
* **Призначення:** Централізований збір, парсинг та обробка даних телеметрії з мікроконтролерів та підключених до них датчиків.
* **Ключові сутності:**
  * `Controller` — мікроконтролер (ESP32) з налаштуваннями мережі.
  * `Sensor` — апаратний датчик (HC-SR04, TM1637), прив'язаний до локації.
  * `Telemetry` — пакет даних із показниками від конкретного сенсора.

---

## 2. Архітектура та структура рішення (Solution)

Проєкт відокремлює консольний інтерфейс від основної логіки (Domain та Dto):

```text
CrossApp/
├── CrossApp.slnx
├── README.md
├── data/
│   ├── sample.csv
│   └── sample.json
└── src/
    ├── Core/
    │   ├── Domain/
    │   │   ├── Sensor.cs
    │   │   ├── SensorStatus.cs
    │   │   ├── SensorBuilder.cs
    │   │   └── ControllerCapacityPolicy.cs
    │   ├── Dto/
    │   │   ├── DeviceDomainDto.cs
    │   │   └── ImportResult.cs
    │   └── Import/
    │       ├── DeviceCsvImporter.cs
    │       └── DeviceJsonImporter.cs
    └── Cli/
        └── Program.cs
```

---

## 3. Інструкція зі збірки та запуску

### Збірка всієї системи
```bash
dotnet build
```

### Запуск програми (Обхід Application Control)
Оскільки локальна політика безпеки Windows (Smart App Control) блокує запуск нових `.exe` файлів, програму налаштовано на безпечний запуск напряму через скомпільовану бібліотеку для **.NET 8.0**.

**Імпорт CSV-файлу (за замовчуванням):**
```bash
dotnet src/Cli/bin/Debug/net8.0/Cli.dll
```

**Або з явним вказанням файлу:**
```bash
dotnet src/Cli/bin/Debug/net8.0/Cli.dll data/sample.csv
```

**Імпорт JSON-файлу:**
```bash
dotnet src/Cli/bin/Debug/net8.0/Cli.dll data/sample.json
```

**Перевірка відмовостійкості (неіснуючий файл):**
```bash
dotnet src/Cli/bin/Debug/net8.0/Cli.dll data/fake.csv
```

---

## 4. Виконані додаткові завдання (Лабораторні 1-3)

У межах попередніх лабораторних робіт успішно реалізовано:

1. **Розпізнавання різнорідних рядків (CSV):** `DeviceCsvImporter` читає різні сутності (Контролери, Сенсори, Телеметрію) з одного файлу, ідентифікуючи їх за префіксами `C`, `S`, `T` за допомогою `switch expression` та `pattern matching`.
2. **Підтримка формату JSON:** Реалізовано `DeviceJsonImporter`. Базовий рекорд `DeviceDomainDto` адаптовано для поліморфної десеріалізації за допомогою атрибутів `[JsonPolymorphic]` та `[JsonDerivedType]`.
3. **Динамічний вибір імпортера:** У `Program.cs` додано `switch expression`, який автоматично обирає потрібний клас для імпорту (`.csv` або `.json`) залежно від розширення вхідного файлу.
4. **Статистика імпорту:** Додано виведення загальної статистики одним рядком перед виведенням результатів (усього / прийнято / пропущено / % помилок).

---

## 5. Дослідження режимів публікації (.NET Publish)

Під час виконання лабораторних робіт досліджено способи публікації застосунку для `win-x64`:

| Режим публікації | Розмір каталогу | Потрібен .NET Runtime |
| :--- | ---: | :---: |
| **Framework-dependent** | ≈ 200 КБ | Так |
| **Self-contained** | ≈ 76 МБ | Ні |
| **Single-File** | ≈ 70 МБ | Ні |
| **Trimmed Single-File** | ≈ 12 МБ | Ні |

---

## 6. Multi-targeting та умовна компіляція

Бібліотеку `Core` та застосунок `Cli` налаштовано на збірку під дві версії платформи:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```
Це дозволяє компілювати проєкт під декілька версій .NET без дублювання кодової бази.

---

## 7. Доменна модель та інваріанти (Лабораторна 4)

У проєкті реалізовано багату доменну модель для сутності `Sensor`. Стан інкапсульовано за допомогою приватних полів та властивостей лише для читання. Створення об'єкта відбувається виключно через статичний фабричний метод `Create`.

**Реалізовані бізнес-правила (Інваріанти):**

| Правило | Тип винятку | Де перевіряється |
| :--- | :--- | :--- |
| Ідентифікатор та модель не можуть бути порожніми | `ArgumentException` | Фабричний метод `Create` |
| Нова локація при монтажі не може бути порожньою | `ArgumentException` | Метод `MountAndActivate` |
| Інтервал опитування (пінг) має бути > 0 | `ArgumentOutOfRangeException` | Фабричний метод `Create` |
| Неможливо виконати недопустимий перехід стану сенсора | `DomainException` (успадковує `InvalidOperationException`) | Перевірка переходів статусів |

**DTO vs Сутність:**
`SensorDto` (`record`) — це анемічна структура даних, яка використовується виключно для перенесення інформації (наприклад, імпорту з CSV). `Sensor` (`class`) — це доменна сутність із поведінкою, яка захищає бізнес-правила, не дозволяє перевести систему в некоректний стан і є основою для подальшого збереження у базу даних. Перетворення між ними відбувається через методи `ToDto()` та `FromDto()`.

---

## 8. Виконані додаткові завдання (Лабораторна 4)

1. **Міст із тижнем 3 (`SensorBuilder`):** Створено метод, який приймає `ImportResult<DeviceDomainDto>`, намагається перетворити DTO на доменні сутності через фабрику, і якщо спрацьовують інваріанти (винятки `ArgumentException`), записує їх у список помилок нового `ImportResult<Sensor>`.
2. **Правило двох сутностей (`ControllerCapacityPolicy`):** Реалізовано інваріант, що охоплює Контролер та Сенсори. Заборонено монтувати більше визначеного ліміту (3) активних сенсорів на один мікроконтролер. Правило винесено в окремий статичний клас, оскільки жоден окремий сенсор не "бачить" інші сенсори.
3. **Явний стан-перелічування (`SensorStatus`):** Замість булевого прапорця використано `enum` (New, Active, Maintenance, Broken). Налаштовано жорстку перевірку допустимих переходів між статусами через `switch expression` над кортежем `(Status, next)` у методі `TransitionTo`.


## 9. Лабораторна робота №5 — сервісний шар, сховища і ручний DI

### Мета

Відокремити бізнес-логіку моніторингу IoT-сенсорів від способу зберігання даних; забезпечити ручний Dependency Injection без стороннього DI-контейнера.

### Архітектура

`Cli (composition root) → SensorService → ISensorStore → InMemorySensorStore | FileSensorStore`

- `ISensorStore` (`src/Core/Abstractions`) — `List`, `GetById`, `Add`, `Update`, `Remove`.
- `InMemorySensorStore` (`src/Core/Storage`) — словник у RAM, стартові 18 сенсорів.
- `FileSensorStore` (`src/Core/Storage`) — JSON у `data/sensors.json`, ліниве завантаження, запис після змін.
- `SensorService` (`src/Core/Services`) — додавання, активація/монтаж, технічне обслуговування, позначення несправності, пошук, видалення.
- `SensorStorageDto` (`src/Core/Dto`) — повний знімок стану для файлу; `SensorDto` попередніх робіт залишено незмінним.

### Збірка і запуск із кореня CrossApp

Для multi-target `net8.0;net10.0` потрібні відповідні .NET SDK (для `net10.0` — SDK 10).

```powershell
dotnet --list-sdks
dotnet build src/Cli/Cli.csproj -f net8.0
# Варіант без запуску .exe (якщо Windows Smart App Control його блокує):
dotnet src/Cli/bin/Debug/net8.0/Cli.dll
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --file
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --file
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --cache
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --file --cache --factory
# Сценарій попередньої лабораторної №4:
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --lab04
# Усі перевірки з автоматичною перевіркою результату:
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab05-check.ps1 -SkipBuild
```

Альтернатива, якщо запуск застосунків через .NET CLI не блокується:

```powershell
dotnet run --project src/Cli --framework net8.0 -- --file
```

При запуску без `--file` зміни зникають після завершення процесу. Прапорець `--file` зберігає дані в `data/sensors.json`; повторний запуск додає новий запис, не затираючи старі.

### Обробка помилок

Демонструються дві контрольовані відмови: повторний `Id` і спроба змінити невідомий сенсор. Повідомлення виводяться через `try/catch` у `Cli`.

### Додаткові завдання

1. Декоратор `CachingSensorStore`: кешує `List`/`GetById` та інвалідовує кеш після змін (`--cache`).
2. Пошук за делегатом `Func<Sensor,bool>`: `service.Search(s => s.Status == SensorStatus.Active)`.
3. `StoreFactory.Create(args)` (`--factory`) для вибору сховища; базовий варіант тернарного вибору залишається в `Program.cs`, що дає змогу порівняти обидва підходи.

**DI-контейнер `Microsoft.Extensions.DependencyInjection` не використовується.**

Звіт із коротким описом, схемою залежностей та переліком демонстрацій: [`docs/lab05-report.md`](docs/lab05-report.md).

Для незалежного тестування роботи з JSON можна додати `--data "C:\Temp\sensors-test.json"` разом із `--file`. Скрипт `scripts/lab05-check.ps1` використовує тимчасовий JSON-файл і не чіпає звичайний `data/sensors.json`.

---

## 10. Лабораторна робота №6 — Доменні події, делегати Action/Func, лямбди

### Мета роботи

Реалізувати механізм сповіщення про зміни стану IoT-сенсорів за допомогою подій, делегатів і лямбда-виразів без залежності доменного шару від консолі та файлового логування.

### Реалізована функціональність

1. Створено `SensorChangeKind` та `SensorChangedEventArgs` для опису доменних подій.
2. У `SensorService` реалізовано подію `Changed` та метод `OnChanged`, який викликається після успішної зміни стану сенсора.
3. Реалізовано два підписники: запис повідомлень у консоль і `logs/app.log`, а також підрахунок подій через лямбда-вираз.
4. Додано можливість відписки від події за допомогою оператора `-=`.
5. Перевірено, що у випадку помилки подія не виникає.
6. Реалізовано пошук сенсорів за умовою через `Func<Sensor, bool>`.

### Додаткові завдання

**1. Інтерфейс INotificationSink**

Створено інтерфейс `INotificationSink` та його реалізації `ConsoleSink` і `FileSink`. Для передавання залежностей використано ручний Dependency Injection.

**2. Делегат Action<string>**

У класі `SensorImportReporter` реалізовано використання `Action<string>` для повідомлення про пропущені некоректні рядки під час імпорту даних.

### Збірка та запуск

Збірка проєкту:

```powershell
dotnet build src/Cli/Cli.csproj -f net8.0
```

Запуск лабораторної №6:

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab06
```

Запуск із додатковими реалізаціями сповіщень:

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab06 --sinks
```

Запуск із файловим сховищем, кешуванням і фабрикою:

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab06 --file --cache --factory --sinks
```

### Тестування

Для перевірки роботи використовується команда:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab06-check.ps1
```

**Результат:** `ALL LAB06 CHECKS PASSED`.

Усі основні та додаткові завдання виконано успішно. Перевірено роботу подій, делегатів, файлового збереження та сумісність із лабораторною №5.

### Висновок

У ході лабораторної роботи реалізовано механізм доменних подій, використано делегати `Action` і `Func`, лямбда-вирази, підписку та відписку від подій. Реалізовано логування змін стану сенсорів і виконано обидва додаткові завдання. Усі автоматичні перевірки пройдено успішно.

<!-- LAB07-SECTION-START -->
## 11. Лабораторна робота №7 — LINQ to Objects: звіти за сенсорами

**Мета:** побудувати аналітичні звіти над `IEnumerable<Sensor>`, відокремити логіку
від CLI через DTO, дослідити відкладене виконання та виконати всі додаткові завдання.

Приклад методички використовує товари і залишки, а в CrossApp доменом є IoT-сенсори.
Тому роль кількісного показника `Quantity` відіграє **`PingIntervalMs`**, а роль
категорії/одиниці — **`Model`**. `report-low` знаходить сенсори, які опитуються
надто часто (`PingIntervalMs < 1000`); `report-top` показує найдовші інтервали.
Групування за моделлю рахує кількість сенсорів і навчальну суму їхніх інтервалів,
`report-sum` виводить кількість, суму, середнє та максимум (усі інтервали в мс).

### Архітектура

- `src/Core/Reports/SensorReportLine.cs` — незмінний `record` для рядка звіту.
- `src/Core/Reports/SensorGroupSummary.cs` — `record` підсумку групи.
- `src/Core/Reports/SensorReportService.cs` — усі LINQ-запити, жодного `Console`.
- `src/Core/Reports/EnumerableReportExtensions.cs` — власний `TopBy<T>`.
- `src/Core/Reports/SensorReading*.cs` — DTO для демонстраційних вимірювань.
- `src/Cli/Lab07Demo.cs` — тільки вибір сценарію, отримання даних і виведення.
- `src/Cli/Lab07SelfTests.cs` — автоматична самоперевірка запитів.
- `src/Core/SampleData.cs` — 18 сенсорів, 4 моделі, 3 інтервали < 1000 мс.

### Чотири обов'язкові звіти

| Команда | Метод у Core | LINQ | Питання |
| --- | --- | --- | --- |
| `report-low` | `LowInterval` | `Where`, `OrderBy`, `ThenBy`, `Select`, `ToList` | Які сенсори мають надто малий інтервал? |
| `report-top` | `TopByInterval` | `OrderByDescending`, `ThenBy`, `Take`, `Select` | Які 5 сенсорів мають найбільший інтервал? |
| `report-groups` | `ByModel` | `GroupBy`, `Count`, `Sum`, `Select` | Скільки сенсорів кожної моделі і яка сума інтервалів? |
| `report-sum` | `Totals` | `ToList`, `Sum`, `Average`, `Max` | Яка кількість, сума, середнє й максимум? |

Запуск із кореня репозиторію:

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-top
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-low
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-groups
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-sum
```

За потреби можна додати `--file`, `--data <шлях>` чи `--cache`:

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --file report-groups
```

### Відкладене виконання LINQ (обов'язкове демо)

Команда `report-deferred` створює запит `Where` до локального списку, але не
виконує його до першого `Count()`. Після `source.Add(...)` другий виклик `Count()`
перераховує елементи, тому результат збільшується на один. `ToList()` виконує
запит і зберігає окремий знімок. Після `source.Clear()` кількість елементів у
знімку не змінюється, оскільки список уже матеріалізований.

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-deferred
```

### Усі чотири додаткові завдання

1. **`GroupJoin` сенсорів із вимірюваннями:** `ReadingsBySensor` повертає `SensorReadingSummary`, включно із сенсорами без вимірювань. Демодані згенеровані лише для прикладу, не є справжньою телеметрією. Команда `report-join`.
2. **Власний `TopBy<T>`:** extension зі вторинним сортуванням за унікальним ID використовується у **двох** звітах: `TopByInterval` і `TopByReadings`. Друга демонстрація — `report-top-readings`.
3. **Query syntax:** `ByModelQuerySyntax` використовує `from`, `group`, `into`, `orderby`, `select`. `report-query` перевіряє його збіг із `ByModel`. Синтаксис методів простіший для послідовних `Where`/`ThenBy`/`Take`, а query syntax зручний для складного групування; обидва дають той самий DTO.
4. **`Stopwatch` на 100 000 записів:** `report-bench` порівнює тотожні запити з додатковим `ToList()` між `Where` і `OrderBy` та без нього. Обидва виконуються після прогріву по три рази; виводиться середній час та перевіряється однаковість результатів. Час **потрібно записати зі свого ПК**, він змінюється залежно від навантаження. Проміжна матеріалізація створює додатковий список і потенційні витрати пам'яті; швидший варіант визначають лише за результатами вимірювань.

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-join
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-top-readings
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-query
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-bench
```

### Самоперевірка і тести

- `report-top` має той самий порядок за повторного запуску завдяки `ThenBy(Id)`.
- На порожньому джерелі `LowInterval` повертає порожній DTO-список, а `Totals` — чотири нулі.
- CLI не містить реалізації `Where`/`GroupBy` для звітів: вони всі зосереджені у Core.
- Методний синтаксис і query syntax повертають однакові результати.
- Використано LINQ to Objects, без PLINQ і без `IQueryable`.

```powershell
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-selftest
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab07-check.ps1
```

Команда повного тестування перевіряє також сумісність із лабораторними 5–6.

### Підсумок

Реалізовано чотири основні аналітичні звіти, демонстрацію deferred execution,
DTO, чотири додаткові завдання, автоматизовану перевірку та сценарій захисту.
Усі звіти знаходяться в Core, а Cli відповідає за запуск і відображення.

<!-- LAB08-SECTION-START -->
## 12. Лабораторна робота №8 — async/await, скасування, доменні винятки, unit-тести Core

**Мета:** Реалізувати справжнє асинхронне читання JSON, кооперативне скасування, відокремити доменні помилки від технічних і протестувати Core за допомогою xUnit.

### Реалізація

- `Core/Abstractions/ISensorImporter.cs` — контракт `Task<IReadOnlyList<Sensor>> LoadAsync(...)`.
- `Core/Import/JsonSensorImporter.cs` — `FileStream(useAsync: true)`, `JsonSerializer.DeserializeAsync`, `CancellationToken`, `ThrowIfCancellationRequested` і повідомлення про номер некоректного запису.
- `Core/DomainException.cs` — виняток бізнес-правила; успадковує `InvalidOperationException` для сумісності з обробниками попередніх лабораторних. Некоректні аргументи залишаються `ArgumentException`, а `IOException`/`JsonException` — технічними винятками.
- `SensorService.ImportAsync` — ручне впровадження імпортера, пропуск повторних ID, подія `Changed` лише для успішно доданих сенсорів.
- `Cli/Lab08Demo.cs` — асинхронний запуск, Ctrl+C і окремі `catch` для скасування, доменних і технічних помилок.
- `tests/Core.Tests` — ізольовані xUnit-тести лише з посиланням на Core (`[Fact]`, `[Theory]`, `[InlineData]`, Arrange–Act–Assert).

### Асинхронні та синхронні операції

Асинхронним зроблено **читання JSON-файлу**, тому що це операція введення-виведення, під час якої не потрібно блокувати потік в очікуванні диска. Для цього `JsonSensorImporter.LoadAsync` використовує `FileStream(useAsync: true)` та `JsonSerializer.DeserializeAsync` з передаванням `CancellationToken`. Перевірка бізнес-правил сенсорів, робота зі сховищем у пам’яті та LINQ-звіти залишилися синхронними: вони виконуються над уже завантаженими об’єктами, тому штучне застосування `Task.Run` не потрібне.

**Фрагмент асинхронного читання** (`src/Core/Import/JsonSensorImporter.cs`):

```csharp
ct.ThrowIfCancellationRequested();
await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
    FileShare.Read, 4096, useAsync: true);
var dtos = await JsonSerializer.DeserializeAsync<List<SensorStorageDto>>(
    stream, cancellationToken: ct);
```

Токен передається за ланцюжком **Cli → `SensorService.ImportAsync` → `JsonSensorImporter.LoadAsync`**. Імпортер перевіряє `ct.ThrowIfCancellationRequested()` під час обробки записів. У Cli `OperationCanceledException` обробляється окремо від помилок: користувач бачить повідомлення «Імпорт скасовано.», а не стек викликів. Для перевірки є команди `import-cancel` та `import-timeout`.

### Розмежування винятків

| Виняток | Коли використовується | Приклад у CrossApp |
| --- | --- | --- |
| `DomainException` | Порушено бізнес-правило | Недопустимий перехід стану сенсора; невалідний запис під час імпорту із зазначенням його номера |
| `ArgumentException` / `ArgumentOutOfRangeException` | Метод отримав некоректний аргумент | Порожній ідентифікатор або інтервал опитування, який не більший за нуль |
| `IOException` / `JsonException` | Сталася технічна помилка читання чи розбору файлу | Файл недоступний або JSON має неправильний синтаксис |
| `OperationCanceledException` | Користувач або тайм-аут скасував операцію | `import-cancel` та `import-timeout` |

`DomainException` не використовується для маскування всіх помилок: технічні винятки й скасування обробляються окремо. Якщо один виняток обгортається іншим, початкову причину корисно зберігати в `InnerException`.

### Додаткові завдання (усі 4)

1. **Потоковий імпорт:** `IStreamingSensorImporter`, `JsonSensorImporter.StreamAsync`, `IAsyncEnumerable<Sensor>`, `[EnumeratorCancellation]`, `await foreach`.
2. **Зв'язані токени:** `CancellationTokenSource.CreateLinkedTokenSource(user.Token, timeout.Token)`; xUnit-тест доводить, що спрацював тайм-аут, а не токен користувача.
3. **Прогрес:** `IProgress<int>` з відсотками по кількості оброблених JSON-записів, обробник у Cli.
4. **Покриття:** `dotnet test --collect:"XPlat Code Coverage"`; Cobertura-XML записується в `artifacts/test-results/`. Реальний відсоток друкується скриптом і залежить від виконаних тестів. Не всі гілки старих імпортерів, CLI та файлового кешу покриваються тестами — цей набір зосереджений на Core, імпорті, доменних правилах, звітах і скасуванні. Cli не входить до тестового проєкту.

### Запуск із кореня CrossApp (PowerShell)

```powershell
# Збірка
dotnet build src/Cli/Cli.csproj -f net8.0
# Успішний імпорт і події
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-async
# Потоковий імпорт (додаткове №1)
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-stream
# Прогрес (додаткове №3)
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-progress
# Скасування і зв'язаний таймаут (додаткове №2)
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-cancel
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-timeout
# Некоректний доменний запис із номером рядка
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-invalid
# Скасування вручну: натиснути Ctrl+C під час асинхронної затримки
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-async --delay-ms 10000
# Імпорт у окреме файлове сховище (повторний запуск має додати 0)
dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-async --file --data data/lab08-imported.json
```

Не використовуються `.Result`, `.Wait()` або `Task.Run` для обгортання синхронного I/O.

### Модульне тестування та покриття

Створено окремий проєкт `tests/Core.Tests`, який посилається на **Core**, але не на Cli. Тестування побудоване за принципом Arrange–Act–Assert. Для ізоляції переважно використовується `InMemorySensorStore`; тести роботи з файлами застосовують тимчасові файли та видаляють їх після виконання.

| Група тестів | Що перевіряється | Очікуваний результат |
| --- | --- | --- |
| Інваріанти сенсора | Некоректні аргументи та зміни стану | Конкретний виняток; некоректна зміна не застосовується |
| Доменні події | Подія `Changed` після додавання | Рівно одна подія для успішної операції |
| LINQ-звіти | Відбір та стабільний порядок ТОП-N | Очікувана кількість і порядок рядків |
| JSON-імпорт | Асинхронне завантаження коректних даних | Сенсори додані, повторний імпорт не створює дублікатів |
| Скасування і тайм-аут | Реакція на `CancellationToken` | `OperationCanceledException`; можна відрізнити тайм-аут від ручного скасування |
| Некоректні дані | Помилковий запис у JSON | `DomainException` із номером запису |

**Фактичний результат перевірки від 15.11.2026:**

- xUnit: **20 тестів із 20 пройшли**, помилок — **0**; два послідовні запуски завершилися успішно.
- Повна перевірка `scripts/lab08-check.ps1`: **9 із 9 етапів** (`ALL LAB08 CHECKS PASSED`).
- `XPlat Code Coverage`: **38,07% виміряного покриття рядків коду**; звіт створений у `artifacts/test-results/.../coverage.cobertura.xml`.

Покриття не дорівнює 100%, тому що тести цієї лабораторної зосереджені на логіці Core, асинхронному імпорті, подіях, звітах і скасуванні. Частина коду попередніх лабораторних і допоміжних сценаріїв не виконується цим набором тестів. Це прийнятно для навчального етапу, але покриття можна поступово збільшувати.

Для повторної перевірки:

```powershell
dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0
dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0 --collect:"XPlat Code Coverage" --results-directory artifacts/test-results
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab08-check.ps1
```

### Висновок

Додано асинхронний імпорт з `CancellationToken`, винятки бізнес-правил, потокове читання, індикацію прогресу та ізольовані тести. Залишено сумісні команди лабораторних 5–7.

Документи: [`docs/lab08-report.md`](docs/lab08-report.md), [`docs/lab08-defense.md`](docs/lab08-defense.md), [`docs/lab08-questions.md`](docs/lab08-questions.md).