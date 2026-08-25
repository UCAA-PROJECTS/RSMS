using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NuGet.Packaging.Signing;
using RSMS.Models;
using System.Text;

namespace RSMS.Services
{
    public class AlertNotificationService : IAlertNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly EmailSettings _settings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AlertNotificationService> _logger;

        public AlertNotificationService(IEmailService emailService, IOptions<EmailSettings> settings, IMemoryCache cache, ILogger<AlertNotificationService> logger)
        {
            _emailService = emailService;
            _settings = settings.Value;
            _cache = cache;
            _logger = logger;
        }
        public async Task NotifyAsync(string shelterCode, string shelterName, string sensorType, string severity, IDictionary<string, string> details, DateTime timeStamp)
        {
            //"Warning"/"Critical"/"Alert" are worth an email - "Normal"/"Ok"/"Healthy" do not reach here

            var cacheKey = $"alert:{shelterCode}:{sensorType}:{severity}"; // Creates a unique key that ensures identical alerts are suppressed and not different ones.

            if(_cache.TryGetValue(cacheKey, out _))
            {
                _logger.LogInformation("Suppressed duplicate alert email ({Key}) - still within cooldown.", cacheKey);//if key exists, skip key and log it
                return;
            }

            var subject = $"[RSMS] {severity} - {shelterName} ({shelterCode}) - {sensorType}";
            var body = BuildEmailBody(shelterCode, shelterName, sensorType, severity, details, timeStamp);
            await _emailService.SendEmailAsync(_settings.Recipients, subject, body);
            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(_settings.AlertCooldownMinutes));//email alert of similar issue wont be sent again until 15min elapse.
        }

        private static string BuildEmailBody(string shelterCode, string shelterName, string sensorType, string severity, IDictionary<string, string> details, DateTime timeStamp)
        {
            var (bannerColor, badgeText) = severity switch
            {
                "Critical" or "Alert" => ("#dc2626", "CRITICAL"),
                "Warning" => ("#d97706", "WARNING"),
                _ => ("#6b7280", severity.ToUpperInvariant()),
            };

            var rows = new StringBuilder();
            var detailsList = details.ToList();
            for (int i =0; i < detailsList.Count; i++)
            {
                var detail = detailsList[i];
                var borderStyle = i < detailsList.Count - 1 ? "border-bottom: 1px solid #f0f0f0" : "";
                rows.Append($"""
                <tr>
                    <td style="padding:10px 0; {borderStyle} font-size: 14px; color:#6b7280">{detail.Key}</td>
                    <td style="padding: 10px 0;font-size: 14px; {borderStyle} color: #111827; font-weight: 600; text-align: right;">{detail.Value}</td>
                </tr>

                """);
            }

            return $"""
                <!DOCTYPE html>
                <html>
                <head>
                  <meta name="viewport" content="width=device-width, initial-scale=1.0">
                </head>
                <body style="margin:0;padding:0;background-color:#f4f5f7;font-family:'Segoe UI',Arial,sans-serif;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f5f7;">
                    <tr>
                      <td align="center" style="padding:24px 16px;">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%;max-width:900px;background-color:#ffffff;border-radius:8px;overflow:hidden;">
                          <tr>
                            <td style="background-color:{bannerColor};padding:24px 32px;">
                              <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                  <td style="color:#ffffff;font-size:12px;font-weight:700;letter-spacing:1px;">RSMS ALERT &middot; {badgeText}</td>
                                </tr>
                                <tr>
                                  <td style="color:#ffffff;font-size:22px;font-weight:700;padding-top:6px;">{sensorType} - {severity}</td>
                                </tr>
                              </table>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:32px;">
                              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:20px;">
                                <tr>
                                  <td style="font-size:13px;color:#6b7280;padding-bottom:4px;">SHELTER</td>
                                </tr>
                                <tr>
                                  <td style="font-size:19px;font-weight:700;color:#111827;">{shelterName} ({shelterCode})</td>
                                </tr>
                              </table>
                              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                                {rows}
                              </table>
                            </td>
                          </tr>
                          <tr>
                            <td style="background-color:#f9fafb;padding:18px 32px;">
                              <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                  <td style="font-size:12px;color:#9ca3af;">Recorded at {timeStamp:yyyy-MM-dd HH:mm:ss}</td>
                                </tr>
                                <tr>
                                  <td style="font-size:12px;color:#9ca3af;padding-top:4px; font-style: italic;">This is an auto-generated message from the Remote Shelter Monitoring System. <strong>Do not reply to this email. <br> &copy {timeStamp:yyyy} - DANS Research & Development </strong></td>
                                </tr>
                              </table>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>

                """;
        }
            

    }   
    
        
}
