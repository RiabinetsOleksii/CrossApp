using System.Text;
using Core.Domain;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("=== Сценарій 1: Успіх ===");
Sensor mySensor = Sensor.Create("S-001", "DHT22", 5000);
Console.WriteLine(mySensor);

mySensor.Mount("Серверна");
Console.WriteLine("Після монтажу: " + mySensor);

mySensor.Deactivate();
Console.WriteLine("Після деактивації: " + mySensor);
Console.WriteLine();

Console.WriteLine("=== Сценарій 2: Порушення інваріантів ===");
TryDo("Створення з порожньою моделлю", () => Sensor.Create("S-002", "", 1000));
TryDo("Від'ємний інтервал", () => Sensor.Create("S-003", "ESP32", -500));
TryDo("Монтаж деактивованого сенсора", () => mySensor.Mount("Новий цех"));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" [X] {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" [V] {title}: {ex.GetType().Name} - {ex.Message}");
    }
}
