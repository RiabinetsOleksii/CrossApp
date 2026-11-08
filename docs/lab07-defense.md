# Шпаргалка для захисту лабораторної №7

**Початок:** «У сьомій лабораторній я додав до CrossApp звіти LINQ to Objects.
У Core — окремий SensorReportService, у Cli — лише команди та вивід.
Виконав усі чотири додаткові завдання».

1. **DTO:** відкрити `src/Core/Reports/SensorReportLine.cs` та
   `SensorGroupSummary.cs`. «Це record-DTO: звіти не повертають внутрішні Sensor».
2. **GroupBy:** відкрити `src/Core/Reports/SensorReportService.cs`, знайти `ByModel`.
   «g.Key — модель, g.Count() — кількість, g.Sum(...) — сума інтервалів».
3. **Чотири звіти:** у терміналі виконати:

   ```powershell
   dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-top
   dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-low
   dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-groups
   dotnet run --project src/Cli/Cli.csproj -f net8.0 -- report-sum
   ```

4. **Стабільність топ-N:** повторити `report-top`. «ThenBy(Id) усуває залежність
   від порядку джерела при однакових значеннях».
5. **Deferred execution:** `report-deferred`. «До додавання — 3, після — 4;
   ToList створює знімок, після Clear у знімку залишається 4».
6. **Додаткове 1 — GroupJoin:** `report-join`. «З'єднує сенсори з
   демонстраційними вимірюваннями, показує і сенсори без вимірювань».
7. **Додаткове 2 — TopBy<T>:** відкрити `EnumerableReportExtensions.cs`;
   `TopByInterval` і `TopByReadings` у `SensorReportService.cs`, виконати
   `report-top-readings`. «Один універсальний extension використано двічі».
8. **Додаткове 3 — синтаксис запитів:** знайти `ByModelQuerySyntax`, виконати
   `report-query`. «Однакова семантика з GroupBy/Select у методному синтаксисі».
9. **Додаткове 4 — Stopwatch:** виконати `report-bench`. «100 тисяч сенсорів,
   три повтори, порівняння з проміжним ToList і без нього, результати однакові.
   Час залежить від комп'ютера».
10. **Перевірка:** запустити
   `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\lab07-check.ps1`.
   «Тести перевіряють лабораторну 7 та сумісність із лабораторними 5–6».

**Висновок:** «Логіка запитів відокремлена від інтерфейсу, оброблені порожні
колекції, застосовані DTO, групування, агрегати, сортування й усі додаткові».
