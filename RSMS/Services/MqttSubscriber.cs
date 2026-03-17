using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.DTO;
using RSMS.Hubs;
using RSMS.Models;
using RSMS.Services;
using System.Text;
using System.Text.Json;

namespace RSMS.Services
{
    public class MqttSubscriber : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ShelterHub> _hub;
        private readonly ILogger<MqttSubscriber> _logger;
        private IMqttClient _mqttClient;

        public MqttSubscriber(
            IServiceScopeFactory scopeFactory,
            IHubContext<ShelterHub> hub,
            ILogger<MqttSubscriber> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            var options = new MqttClientOptionsBuilder()
                .WithClientId("RSMS-Dashboard")
                .WithTcpServer("localhost", 1883)
                .WithCleanSession(false)
                .Build();

            _mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("✅ MQTT connected.");
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic("shelters/+")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build());
                _logger.LogInformation("✅ MQTT subscribed to shelters/+.");
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                _logger.LogWarning("⚠️ MQTT disconnected. Retrying in 5s...");
                await Task.Delay(5000, stoppingToken);
                try
                {
                    await _mqttClient.ConnectAsync(options, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MQTT reconnection failed.");
                }
            };

            try
            {
                await _mqttClient.ConnectAsync(options, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MQTT broker.");
            }

            // Keep the service alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            // Wrap the entire handler to catch all exceptions
            return Task.Run(async () =>
            {
                try
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    var dto = JsonSerializer.Deserialize<SensorInputDTO>(
                        payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto == null) return;

                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var shelterStatusService = scope.ServiceProvider.GetRequiredService<IShelterService>();

                    var shelter = await context.Shelters
                        .FirstOrDefaultAsync(s => s.ShelterCode == dto.ShelterCode);

                    if (shelter == null)
                    {
                        _logger.LogWarning("Invalid shelter code: {ShelterCode}", dto.ShelterCode);
                        return;
                    }

                    var reading = new SensorReading
                    {
                        ShelterCode = shelter.ShelterCode,
                        Temperature = dto.Temperature,
                        Humidity = dto.Humidity,
                        SmokeDetected = dto.SmokeDetected,
                        IntrusionDetected = dto.IntrusionDetected
                    };

                    context.Readings.Add(reading);
                    await context.SaveChangesAsync();
                    var statusResult = shelterStatusService.Evaluate(reading);
                    await _hub.Clients
                        .Group(shelter.ShelterCode)
                        .SendAsync("ShelterUpdated", new
                        {
                            shelter.ShelterCode,
                            shelter.ShelterName,
                            reading.Temperature,
                            reading.Humidity,
                            reading.SmokeDetected,
                            reading.IntrusionDetected,
                            temperatureStatus = statusResult.TemperatureStatus.ToString(),
                            humidityStatus = statusResult.HumidityStatus.ToString(),
                            smokeStatus = statusResult.smokeStatus.ToString(),
                            intrudeStatus = statusResult.intrudeStatus.ToString(),
                            overallStatus = statusResult.OverallStatus.ToString(),



                        });

                    _logger.LogInformation("Shelter {Shelter} updated via MQTT.", shelter.ShelterCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message.");
                }
            });
        }
    }
}