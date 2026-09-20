# CrossApp — Лабораторна робота №2

Наскрізний проєкт з дисципліни «Крос-платформне програмування».  
**Виконав:** студент групи ФЕІ-35 Рябінець Олексій  
**Львівський національний університет імені Івана Франка**  
Факультет електроніки та комп’ютерних технологій  

---

## 📌 Мета роботи

Відокремити спільну бізнес-логіку від точки входу через створення бібліотеки класів (`Core`), налаштувати multi-targeting, підключити її до консольного застосунку (`Cli`), а також опанувати різні режими публікації (self-contained та framework-dependent) із аналізом їхніх розмірів і залежностей.

---

## 🏢 Предметна область

* **Обраний домен:** Склад.
* **Основні сутності:**
  * `Product` (товар)
  * `StockBatch` (партія товару)
  * `Warehouse` (склад)
  * `Movement` (переміщення товару)
* **Призначення застосунку:** облік залишків товарів по партіях на складах та фіксація історії переміщень товару між об'єктами.

---

## 💻 Технічне середовище

* **Операційна система:** Windows 11 (x64)
* **Платформа:** .NET SDK 10.0 (TFM: `net10.0`, з підтримкою multi-targeting `net8.0;net10.0` у бібліотеці `Core`)
* **Середовище розробки:** VS Code / Git Bash / PowerShell

---

## 📂 Структура проєкту

```text
CrossApp/
│
├── CrossApp.sln
├── README.md
├── .gitignore
│
└── src/
    ├── Core/                # Бібліотека класів (збір системної інформації)
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    │
    └── Cli/                 # Консольний інтерфейс
        ├── Cli.csproj       # Містить ProjectReference на Core
        └── Program.cs

🚀 Інструкція зі збірки, запуску та публікації
1. Клонування репозиторію та збірка
Bash
git clone [https://github.com/RiabinetsOleksii/CrossApp.git](https://github.com/RiabinetsOleksii/CrossApp.git)
cd CrossApp
dotnet build

2. Запуск консольного застосунку
Bash
dotnet run --project src/Cli

3. Публікація застосунку
Self-contained (самодостатній, з убудованим рантаймом ~78 МБ):

Bash
dotnet publish src/Cli -c Release -f net10.0 -r win-x64 --self-contained true
Framework-dependent (залежний від встановленого .NET ~229 КБ):

Bash
dotnet publish src/Cli -c Release -f net10.0 -r win-x64 --self-contained false

4. Прямий запуск зібраного бінарника (Windows 11)
Якщо виникають системні обмеження політики безпеки, попередньо розблокуйте файл через PowerShell (Unblock-File), після чого запустіть його:

PowerShell
.\src\Cli\bin\Release\net10.0\win-x64\publish\Cli.exe

📊 Порівняння розмірів self-contained публікаційЦільова платформа (RID)Розмір каталогу publishОсобливостіwin-x64~76.66 МБВключає рантайм Windows, працює без встановленого .NET на ПКlinux-x64~78.81 МБКрос-платформна збірка під Linux x64
