using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared
{
    public class RabbitMQProducer : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQProducer> _logger;

        public RabbitMQProducer(IConfiguration configuration, ILogger<RabbitMQProducer> logger)
        {
            _logger = logger;
            try
            {
                var hostName = configuration["RabbitMQ:HostName"] ?? "192.168.1.5";
                var userName = configuration["RabbitMQ:UserName"] ?? "guest";
                var password = configuration["RabbitMQ:Password"] ?? "guest";
                var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");

                _logger.LogInformation("Connecting to RabbitMQ at {HostName}:{Port} with user {UserName}", hostName, port, userName);

                var factory = new ConnectionFactory
                {
                    HostName = hostName,
                    UserName = userName,
                    Password = password,
                    Port = port
                };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(queue: "stock_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                _logger.LogInformation("Successfully connected to RabbitMQ and declared queue 'stock_queue'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                throw new Exception("Falha ao conectar ao RabbitMQ", ex);
            }
        }

        public void PublishMessage(string routingKey, string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message), "Message cannot be null or empty.");
            }
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "", routingKey: routingKey, basicProperties: null, body: body);
            _logger.LogInformation("Published message to {RoutingKey}: {Message}", routingKey, message);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQProducer disposed");
        }
    }
}