using Microsoft.Extensions.Options;
using MimeKit;
using RSMS.Models;
using MailKit.Security;
using MailKit.Net.Smtp;

namespace RSMS.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            
        }
        public async Task SendEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody)
        {
            var recipientList = recipients.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            if (recipientList.Count == 0)
            {
                _logger.LogWarning("There are no recipients to recieve mail notifications");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));

            foreach(var recipient in recipientList)
            {
                message.To.Add(MailboxAddress.Parse(recipient));

                message.Subject = subject;
                message.Body = new TextPart("html")
                {
                    Text = htmlBody
                };

                using var client = new SmtpClient();
                try
                {
                    await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, _settings.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None);
                    await client.AuthenticateAsync(_settings.Username, _settings.Password);
                    await client.SendAsync(message);
                    _logger.LogInformation("Alert email sent to {Recipients}. Subject: {Subject}", string.Join(", ", recipientList), subject);
                }
                catch (Exception ex) 
                {

                    _logger.LogError(ex, "Failed to send alert email. Subject: {Subject}", subject);
                }
                finally
                {
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true);
                    }
                }
            }
        }
    }
}
