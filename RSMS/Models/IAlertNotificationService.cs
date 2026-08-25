namespace RSMS.Models
{
    public interface IAlertNotificationService
    {
        Task NotifyAsync(string shelterCode, string shelterName, string sensorType, string severity, 
            IDictionary<string, string>details, DateTime timeStamp);
    }
}
