# Лабораторна робота №8

**Тема:** async/await, скасування, доменні винятки, unit-тести Core.

**Мета:** Реалізувати асинхронний ввід-вивід, кооперативне скасування, доменні винятки й модульне тестування бізнес-логіки системи моніторингу IoT-сенсорів.

## Хід роботи

### 1. Асинхронний імпорт

Створено інтерфейс `ISensorImporter` і клас `JsonSensorImporter`. Файли читаються за допомогою `FileStream(useAsync: true)` та `JsonSerializer.DeserializeAsync` із передачею `CancellationToken`. Перетворення DTO на `Sensor` перевіряється для кожного запису. Синхронні операції зі сховищем попередніх робіт збережено для сумісності.

**Скрін 1:** відкрий `src/Core/Import/JsonSensorImporter.cs`, покажи `LoadAsync`, `DeserializeAsync`, `ThrowIfCancellationRequested`. *Рисунок 1 — Асинхронне читання даних.*

**Скрін 2:** `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-async`. *Рисунок 2 — Імпорт та події Changed.*

### 2. Скасування та помилки

У `Lab08Demo.cs` використано `CancellationTokenSource`, обробник `Console.CancelKeyPress`, `await` і окремі блоки `catch` для `OperationCanceledException`, `DomainException`, `IOException` та `JsonException`. Порушення правил домену відокремлено від помилок файлу. `DomainException` наслідує `InvalidOperationException`, щоб зберегти сумісність із попередніми лабораторними.

**Скрін 3:** `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-cancel`. *Рисунок 3 — Кооперативне скасування імпорту.*

**Скрін 4:** `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-invalid`. *Рисунок 4 — Доменний виняток із номером запису.*

### 3. Модульне тестування

До solution додано `tests/Core.Tests` з посиланням тільки на `Core`. Написано тести для інваріантів, кількості подій, LINQ-звітів, ідемпотентності імпорту, скасування, помилкових даних, потокового режиму, відсотків прогресу та linked-token таймауту.

| Тест / група | Що перевіряє | Очікування |
| --- | --- | --- |
| InvalidTransition / BadInput | Захист стану та аргументів | Виняток, стан незмінний |
| Add / Failure | Подія лише за успішної операції | Рівно одна подія або жодної |
| LowInterval / TopN | Фільтр і визначений порядок | Правильні DTO |
| EmptyCollections | Порожні дані | Без помилки |
| ImportAsync / StreamingImport | Імпорт, дублікати | Події лише для нових |
| LinkedTokens | Таймаут і токен користувача | Скасований лише таймаут |
| JsonImporter / InvalidRecord | JSON і бізнес-правила | Правильний результат / DomainException |
| MalformedJson / MissingFile | Технічні збої | JsonException / FileNotFoundException |
| Progress | Хід імпорту | Досягає 100% |

**Скрін 5:** `dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0`. *Рисунок 5 — Успішний запуск xUnit-тестів.*

### 4. Додаткові завдання

**Додаткове №1 — потоковий імпорт.** Додано `IAsyncEnumerable<Sensor>` із `[EnumeratorCancellation]` та `await foreach`, який отримує дані поступово.

Команда: `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-stream`.

*Рисунок 6 — Результат потокового імпорту.*

**Додаткове №2 — зв'язані токени.** Використано `CancellationTokenSource.CreateLinkedTokenSource`; в окремому тесті перевіряється, що імпорт скасовано саме таймаутом.

Команда: `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-timeout`.

*Рисунок 7 — Скасування імпорту через таймаут.*

**Додаткове №3 — IProgress<int>.** Імпортер повідомляє відсотки оброблених записів, консольний підписник друкує прогрес.

Команда: `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-progress`.

*Рисунок 8 — Відображення прогресу імпорту.*

**Додаткове №4 — покриття тестами.** Запускається збір статистики `XPlat Code Coverage`, утворюється XML-звіт Cobertura.

Команда: `dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0 --collect:"XPlat Code Coverage" --results-directory artifacts/test-results`.

**Фактичне покриття рядків Core:** **_____ %** (підставити значення з результату перевірки на власному ПК).

Не всі старі гілки Core, пов'язані зі сховищем, імпортерами лабораторних №1–4 і кешем, включені до нових модульних тестів: пріоритетом лабораторної №8 є асинхронний імпорт, інваріанти, події та LINQ-звіти. Об'єктивний відсоток залежить від запущених тестів; охоплення не стверджується до фактичного вимірювання.

*Рисунок 9 — Збір результатів покриття тестами.*

### 5. Підсумкова перевірка

Команда: `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab08-check.ps1`.

*Рисунок 10 — Результат підсумкової перевірки (лише після фактичного успіху).* 

## Висновок

Реалізовано справжній асинхронний імпорт сенсорів, кооперативне скасування, доменні винятки та unit-тести Core. Виконано усі чотири додаткові завдання: потоковий імпорт, зв'язані токени, прогрес імпорту і збір покриття тестами.
