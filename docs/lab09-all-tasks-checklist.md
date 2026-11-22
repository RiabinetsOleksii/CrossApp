# Лабораторна №9: перевірка вимог і опційних можливостей

Ця методичка **не має окремого розділу «Додаткові завдання»**. У розділі «Пакети і команди» явно позначено одну опційну можливість: `AddCommandLine`, яка дозволяє перевизначити параметри через `--Storage:DataPath=...`. У цій версії пакета вона є **і перевіряється автоматично**.

| Вимога методички | Реалізація | Як перевірити |
| --- | --- | --- |
| 1. Немає Windows/Linux абсолютних шляхів у `src` | `Path.Combine`, `Path.GetFullPath`, `ProjectPaths` | Пошук `C:\`, `/home/` у `src` |
| 2. `appsettings.json` і профілі | `src/Cli/appsettings*.json` | `--lab09 config`, `CROSSAPP_ENVIRONMENT=Production` |
| 3. `AppOptions` і `Get<AppOptions>()` | `Core/Configuration/AppOptions.cs`, `Lab09Configuration.cs` | Відкрити обидва файли |
| 4. `CROSSAPP_...` та `CROSSAPP_DATA_PATH` | `Lab09Configuration.Load()` | `lab09-check.ps1`, кроки 3–4 |
| 5. JSON повної колекції, `.tmp` + `File.Move` | `FileSensorStore.ExportSnapshot/Flush` | `lab09-check.ps1`, кроки 5–7 та xUnit |
| 6. Secrets поза Git | `UserSecretsId`, `AddUserSecrets`, вивід лише факту | `dotnet user-secrets list --project src/Cli` |
| 7. Однаковий файл для різних cwd | пошук кореня від каталогу збірки | `lab09-check.ps1`, крок 3 |
| 8. README із профілями, ключами, PowerShell/bash | кореневий `README.md` після інсталяції | розділ 13 |
| **Опційно: AddCommandLine** | `.AddCommandLine(configArgs)` | `lab09-check.ps1`, крок 3b |
| Приклади запуску `export`/`import` без `--lab09` | маршрутизація в `Program.cs` | `dotnet run ... -- export` |
| Перевірка кирилиці й старого JSON | серіалізатор і xUnit | `Lab09Tests.cs` |
| Відсутність залишкового `.tmp` | `FileSensorStore.Flush()` | `lab09-check.ps1` та xUnit |

**Примітка:** відповідність збірки і проходження тестів на пристрої користувача підтверджуються лише фактичним запуском `dotnet build`, `dotnet test` і `lab09-check.ps1`. Windows Smart App Control може блокувати DLL незалежно від правильності коду.
