# Звіт. Лабораторна робота №5

**Тема:** Сервісний шар: інтерфейси, два сховища, ручний DI  
**Проєкт:** CrossApp — система моніторингу IoT-пристроїв  
**Студент:** Рябінець Олексій (групу та дату актуалізувати перед здачею)

## 1. Предметна область і контракт

Головна сутність — `Sensor`. Вона має `Id`, `ControllerId`, `Model`, `PingIntervalMs`, `Location`, `Status` та методи зміни стану. Інтерфейс `ISensorStore` задає п'ять операцій:

- `List()` — перегляд усіх сенсорів; повертає `IReadOnlyList`, щоб не дозволяти змінювати колекцію через її API.
- `GetById(id)` — знаходить сенсор або повертає `null`.
- `Add(sensor)` — додає сенсор, відмовляючи при повторному ID.
- `Update(sensor)` — зберігає новий стан наявного сенсора.
- `Remove(id)` — видаляє сенсор і повертає результат.

## 2. Два сховища

- `InMemorySensorStore` — `Dictionary<string, Sensor>`, початкові 18 сенсорів із `SampleData`; після закінчення процесу зміни зникають.
- `FileSensorStore` — кеш у пам'яті, ліниве завантаження, `data/sensors.json`, запис JSON після кожної зміни. Для файлу застосовується `SensorStorageDto` з усіма полями сенсора. Старий `SensorDto` для імпорту не змінено.

## 3. Схема залежностей

`Cli (Program.cs) → SensorService → ISensorStore → InMemorySensorStore / FileSensorStore`

Опційно: `Cli → CachingSensorStore → ISensorStore`.

## 4. Composition root

```csharp
ISensorStore store = useFactory
    ? StoreFactory.Create(args)
    : useFile
        ? new FileSensorStore(dataPath)
        : new InMemorySensorStore(SampleData.Sensors());
if (useCache) store = new CachingSensorStore(store);
var service = new SensorService(store);
```

**Команди для скриншотів:**

```powershell
dotnet build src/Cli/Cli.csproj -f net8.0
dotnet src/Cli/bin/Debug/net8.0/Cli.dll
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --file
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --file
dotnet src/Cli/bin/Debug/net8.0/Cli.dll --file --cache --factory
```

> Вставити **реальний** вивід консолі та/або власні скриншоти обох режимів. Не підміняти фактичні результати прикладами.

## 5. Майбутній перехід на Entity Framework Core

Під час переходу до бази даних буде створено `EfSensorStore : ISensorStore`; у точці входу буде замінено лише конкретну реалізацію сховища. `SensorService`, його конструктор та предметні правила `Sensor` залишаться незалежними від механізму зберігання. Пізніше ручне створення об'єктів можна замінити DI-контейнером у `Api`.

## 6. Додаткові завдання

1. `CachingSensorStore` кешує результати `List` і `GetById`; `Add/Update/Remove` інвалідовують кеш.
2. `SensorService.Search(Func<Sensor,bool>)` фільтрує сенсори за довільним предикатом; приклад — лише активні сенсори.
3. `StoreFactory.Create(args)` вибирає реалізацію; `--factory` демонструє фабрику, без прапорця вибір залишається безпосередньо в `Program.cs`.
