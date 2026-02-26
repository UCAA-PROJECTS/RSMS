namespace RSMS.Models
{
    public interface IShelterService
    {
        ShelterStatusResult Evaluate(SensorReading reading);
    }
}
