
namespace RSMS.Models
{
    public interface IEmailService
    {
        Task SendEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody);
    }
}
