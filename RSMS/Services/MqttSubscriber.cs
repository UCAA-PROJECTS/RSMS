using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Packets;
using RSMS.Data;
using RSMS.DTO;
using RSMS.Hubs;
using RSMS.Models;
using System.Text;
using System.Text.Json;

//To run code as it was originally, comment everyline inside the //ADDED_LINES ....//END_OF_ADDED_LINES. then uncomment the commented code. 

namespace RSMS.Services
{
    /// <summary>
    /// The service class for running the MQTT client. It uses the BackgroundService class's ExecuteAsync Method to start code execution at application startup. 
    /// This method in dotnet 9  runs all the first lines of code in a synchronous way until it hits the first 'await' keyword, then it starts runnin asynchronously. 
    /// 
    /// </summary>
    public class MqttSubscriber  : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ShelterHub> _hub; 
        private readonly ILogger<MqttSubscriber> _logger;
        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttClientConnectOptions;
        private MqttTopicFilter _mqttClientSubscriptionOptions;
        private MqttClientDisconnectOptions _mqttClientDisconnectOptions;
        private string MqttServerAddress => "localhost"; //"172.29.100.10"; //this will be changed to the actual mqtt server 
        private int MqttServerPort => 1883;

        public MqttSubscriber(IServiceScopeFactory scopeFactory, IHubContext<ShelterHub> hub, ILogger<MqttSubscriber> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;

            //create client in constructor since they must exist from application startup and they are created once.
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            //create mqtt client connection options
            _mqttClientConnectOptions = new MqttClientOptionsBuilder()
                .WithClientId("RSMS-Dashboard")
                .WithTcpServer(MqttServerAddress, MqttServerPort)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .Build();

            //create mqtt client subscription options.
            _mqttClientSubscriptionOptions = new MqttTopicFilterBuilder()
                    .WithTopic("shelters/+")
                    //.WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

            //create mqtt client disconnect options.
            _mqttClientDisconnectOptions = new MqttClientDisconnectOptionsBuilder()
                    .WithReason(MqttClientDisconnectReason.NormalDisconnection)
                    .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ///-----------Register with MQTT event callbacks.------------
            //register with event callbacks
            _mqttClient.ConnectedAsync += args => OnConnectAsync(args, stoppingToken);
            _mqttClient.DisconnectedAsync += args => OnDisConnectAsync(args, stoppingToken);
            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            //connect to the MQTT server
            //We run this connection method as a seperate background service (i.e inside Task.Run()) because of the while loop it contains.
            //During the initial connection when ExecuteAsync is still synchronous, if the *MQTT SERVER IS OFFLINE*,  the ConnectAsync() method inside the HandleManualReconnectAsync()
            // throws an exception instantly, this trigger the catch block which starts a delay of 5 seconds, at this point the ExecuteAsync() is still in blocking state, so the
            //entire application waits for 5 seconds before startup. After 5 seconds however, we're still in the while loop which calls the method ConnectAsync() in the next loop. the compiler 
            //however the .NET runtime is still waiting on the first command to finish before it handles this new command. this causes the application to stall forever.
            _= Task.Run(() => HandleManualReconnectAsync(stoppingToken), stoppingToken) ;

            // Keep the service alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }   
        }

        private async Task OnConnectAsync(MqttClientConnectedEventArgs args, CancellationToken cancellationToken)
        {
            _logger.LogInformation("MQTT connected.");

            //subscribe to all shelter topics
            await _mqttClient.SubscribeAsync(_mqttClientSubscriptionOptions, cancellationToken);
            _logger.LogInformation("MQTT subscribed to shelters/+.");
        }

        private async Task OnDisConnectAsync(MqttClientDisconnectedEventArgs args, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            
            _logger.LogWarning("MQTT disconnected. Retrying in 2s...");
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            _= Task.Run(() =>HandleManualReconnectAsync(cancellationToken), cancellationToken);
        }

        private async Task HandleManualReconnectAsync( CancellationToken cancellationToken)
        {
            while (!_mqttClient.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Attempting to connect to the MQTT Server.");
                    await _mqttClient.ConnectAsync(_mqttClientConnectOptions, cancellationToken); 
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MQTT reconnection failed. Trying again in 5 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
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

                    // Get the Application DbContext service, this is used to acecess the Database table "Shelters", this is to make sure we first cofirm if the payload 
                    //obtained has a known shelter id, ensures entegrity of sensor data. 
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var shelterStatusService = scope.ServiceProvider.GetRequiredService<IShelterService>();

                    var shelter = await context.Shelters.FirstOrDefaultAsync(s => s.ShelterCode == dto.ShelterCode);

                    if (shelter == null)
                    {
                        _logger.LogWarning("Invalid shelter code: {ShelterCode}", dto.ShelterCode);
                        return;
                    }
                    
                    //extract only the relevant data for the "Readings" table
                    var reading = new SensorReading
                    {
                        ShelterCode = shelter.ShelterCode,
                        Temperature = dto.Temperature,
                        Humidity = dto.Humidity,
                        SmokeDetected = dto.SmokeDetected,
                        IntrusionDetected = dto.IntrusionDetected,
                        TimeStamp = dto.RecordedTime.UtcDateTime
                    };

                    //Add a new reading to the Database
                    context.Readings.Add(reading);
                    await context.SaveChangesAsync();
                    var statusResult = shelterStatusService.Evaluate(reading);
                    await _hub.Clients
                        .Group(shelter.ShelterCode)
                        .SendAsync("ShelterUpdated", new
                        {
                            reading.TimeStamp,
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

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Stopping MQTT client connection.");
            if (_mqttClient != null)
            {
                try
                {
                    await _mqttClient.DisconnectAsync(_mqttClientDisconnectOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting MQTT client.");
                }
            }
            await base.StopAsync(cancellationToken);
        }
    }
}