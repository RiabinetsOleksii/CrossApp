# Лабораторна №9 — відповіді на 9 запитань для захисту

1. **CurrentDirectory vs BaseDirectory?** `Environment.CurrentDirectory` — папка запуску процесу, може змінюватися. `AppContext.BaseDirectory` — місце скомпільованого застосунку, стабільне для тієї ж збірки.
2. **Чому Path.Combine?** Підставляє правильні роздільники шляхів для Windows/Linux/macOS; ручне `"\\"` ламає переносимість.
3. **Коли LocalApplicationData?** Коли потрібно зберігати дані конкретного користувача поза текою програми (наприклад, кеш і локальні налаштування).
4. **Пріоритет джерел?** `appsettings.json` → профіль → user-secrets → env → аргументи. Пізніше джерело перекриває попереднє. У CrossApp простий `CROSSAPP_DATA_PATH` окремо має пріоритет для каталогу.
5. **Як env для Storage:DataPath?** `$env:CROSSAPP_Storage__DataPath = "..."` у PowerShell або `CROSSAPP_Storage__DataPath=/tmp/data` у Bash. `__` замінює `:`.
6. **Де user-secrets, чому не для Production?** У профілі користувача, поза Git: Windows `%APPDATA%\Microsoft\UserSecrets\...`, Linux `~/.microsoft/usersecrets/`. Це не зашифрований vault; у Production краще захищені env або менеджер секретів.
7. **CamelCase у JSON?** `PingIntervalMs` записується як `pingIntervalMs` та `ControllerId` як `controllerId`; при читанні працює `PropertyNameCaseInsensitive`.
8. **Чому кирилиця стає \\uXXXX?** Стандартний енкодер System.Text.Json екранує не-ASCII символи. `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` дозволяє читабельну кирилицю, але його не слід сліпо використовувати для JSON, який вставляється у HTML.
9. **Навіщо .tmp + File.Move?** Щоб не пошкодити основний JSON у разі збою під час запису: спочатку записується повний тимчасовий файл, а потім він атомарно замінює основний на тому ж диску.
