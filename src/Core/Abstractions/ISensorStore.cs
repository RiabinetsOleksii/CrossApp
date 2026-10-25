using Core.Domain;

namespace Core.Abstractions;

public interface ISensorStore
{
    IReadOnlyList<Sensor> List();
    Sensor? GetById(string id);
    void Add(Sensor item);
    void Update(Sensor item);
    bool Remove(string id);
}
