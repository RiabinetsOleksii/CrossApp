# Лабораторна №8 — план захисту

Відкрити проєкт CrossApp у VS Code. Термінал PowerShell у корені проєкту.

1. **Представитися.** «Виконав лабораторну №8. Додав async/await, CancellationToken, доменні винятки та unit-тести Core. Усі 4 додаткові реалізовані».
2. **Тести.** `dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0`. «Тести незалежні від консолі; використовують сховище в пам'яті».
3. **Async LoadAsync.** Відкрити `src/Core/Import/JsonSensorImporter.cs`. Знайти `LoadAsync`, `useAsync: true`, `DeserializeAsync`, `ct.ThrowIfCancellationRequested`. «Справжній async I/O; Task.Run немає».
4. **Токен на всіх рівнях.** Відкрити `src/Cli/Lab08Demo.cs`, знайти `linked.Token`; потім `src/Core/Services/SensorService.cs`, `ImportAsync`; нарешті `JsonSensorImporter.LoadAsync`. «Cli → Service → Importer».
5. **Звичайний імпорт і події.** `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-async`. «Кожен успішний новий запис породжує Changed».
6. **Скасування.** `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-cancel`; додатково `... --lab08 import-async --delay-ms 10000`, під час запуску Ctrl+C.
7. **Зіпсований файл.** `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-invalid`. «DomainException: Запис 2».
8. **Тест події.** Відкрити `tests/Core.Tests/SensorServiceTests.cs`, метод `Add_RaisesChangedExactlyOnce`. «Subscribe, Act, Assert.Single, жодних Console/диска».
9. **Додаткове 1: streaming.** `dotnet run --project src/Cli/Cli.csproj -f net8.0 -- --lab08 import-stream`; показати `StreamAsync` і `[EnumeratorCancellation]`.
10. **Додаткове 2: linked cancellation.** `... --lab08 import-timeout`; показати тест `LinkedTokens_TimeoutCancelsAndUserTokenIsNotCancelled`.
11. **Додаткове 3: progress.** `... --lab08 import-progress`; показати IProgress<int> і 100%.
12. **Додаткове 4: coverage.** `dotnet test tests/Core.Tests/Core.Tests.csproj -f net8.0 --collect:"XPlat Code Coverage"`; вказати фактичне покриття з XML.
13. **Підсумок.** `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab08-check.ps1`; показати повідомлення про успіх тільки якщо всі перевірки справді завершилися без помилок.
