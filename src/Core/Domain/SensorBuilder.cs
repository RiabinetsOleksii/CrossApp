using Core.Dto;
using Core.Import;

namespace Core.Domain;

public static class SensorBuilder
{
    public static ImportResult<Sensor> FromImport(ImportResult<DeviceDomainDto> source)
    {
        var sensors = new List<Sensor>();
        var errors = new List<string>(source.Errors);

        foreach (var dto in source.Items.OfType<SensorDto>())
        {
            try
            {
                sensors.Add(Sensor.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                errors.Add($"{dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<Sensor>(sensors, errors);
    }
}
