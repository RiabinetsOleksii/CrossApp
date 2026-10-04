using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class DeviceJsonImporter
{
    public static ImportResult<DeviceDomainDto> Load(string path)
    {
        var errors = new List<string>();
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            var items = JsonSerializer.Deserialize<List<DeviceDomainDto>>(json, options) ?? [];
            return new ImportResult<DeviceDomainDto>(items, errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"Помилка розбору JSON: {ex.Message}");
            return new ImportResult<DeviceDomainDto>([], errors);
        }
    }
}