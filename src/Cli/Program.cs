using System.Text;
using Core.Domain;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("=== Сценарій 1: Дод. завдання 3 (Переходи статусів) ===");
Sensor mySensor = Sensor.Create("S-001", "C-100", "DHT22", 5000);
Console.WriteLine(mySensor);

mySensor.MountAndActivate("Серверна");
Console.WriteLine(mySensor);

mySensor.SendToMaintenance();
Console.WriteLine(mySensor);

TryDo("Активувати зламаний (заборонено)", () =>
{
    mySensor.MarkAsBroken();
    mySensor.MountAndActivate("Серверна");
});
Console.WriteLine();

Console.WriteLine("=== Сценарій 2: Дод. завдання 2 (Правило двох сутностей) ===");
var systemSensors = new List<Sensor>();
string controllerId = "ESP-32-MAIN";

for (int i = 1; i <= ControllerCapacityPolicy.MaxActiveSensorsPerController; i++)
{
    ControllerCapacityPolicy.EnsureCanMountSensor(controllerId, systemSensors);
    var s = Sensor.Create($"S-10{i}", controllerId, "TM1637", 1000);
    s.MountAndActivate($"Зона {i}");
    systemSensors.Add(s);
}
Console.WriteLine($"Підключено сенсорів до {controllerId}: {systemSensors.Count}");

TryDo("Перевищення ліміту портів контролера", () =>
    ControllerCapacityPolicy.EnsureCanMountSensor(controllerId, systemSensors));
Console.WriteLine();

Console.WriteLine("=== Сценарій 3: Дод. завдання 1 (Міст імпорту з доменними правилами) ===");
var importedDtos = new ImportResult<DeviceDomainDto>(
    [
        new SensorDto("S-201", "HC-SR04", "Цех 1"),
        new SensorDto("S-202", "", "Цех 2")
    ],
    ["Стара помилка читання рядка 5"]
);

var domainImport = SensorBuilder.FromImport(importedDtos);
Console.WriteLine($"Імпортовано валідних сутностей: {domainImport.Items.Count}");
Console.WriteLine("Помилки в процесі (старі + порушення домену):");
foreach (var err in domainImport.Errors) Console.WriteLine($" - {err}");

Console.WriteLine("\n=== Сценарій 4: Стандартні інваріанти ===");
TryDo("Порожня модель при створенні", () => Sensor.Create("S-002", "C-1", "", 1000));
TryDo("Від'ємний інтервал", () => Sensor.Create("S-003", "C-1", "ESP32", -500));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" [X] {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" [V] {title}: {ex.GetType().Name} - {ex.Message}");
    }
}
