using RSMS.Models;

namespace RSMS.Services
{
    public interface IShelterStatusService
    {
        ShelterStatus Evaluate(SensorReading reading);
    }
}
