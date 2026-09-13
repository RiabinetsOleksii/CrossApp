using System;
using System.Runtime.InteropServices;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var info = new
{
    Title = "CrossApp — практикум з крос-платформного програмування",
    Student = "Рябінець Олексій, група ФЕІ-22",
    OsDescription = RuntimeInformation.OSDescription,
    OsEnvironment = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    ClrVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Склад (товари, партії, залишки, переміщення)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    Console.WriteLine(JsonSerializer.Serialize(info, options));
}
else
{
    Console.WriteLine(info.Title);
    Console.WriteLine($"Студент: {info.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)     : {info.OsDescription}");
    Console.WriteLine($"ОС (Environment)       : {info.OsEnvironment}");
    Console.WriteLine($"Архітектура процесу    : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)      : {info.ClrVersion}");
    Console.WriteLine($"Runtime                : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку     : {info.AppDirectory}");
    Console.WriteLine($"Поточний каталог       : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область      : {info.Domain}");
}