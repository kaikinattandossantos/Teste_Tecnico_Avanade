using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StockService.Data; // Adicione esta linha

namespace StockService
{
    public class StockConsumer : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private IConnection? _connection; // Marcar como nullable
        private IModel? _channel; // Marcar como nullable

        public StockConsumer(IServiceProvider services, IConfiguration configuration)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var portString = _configuration["RabbitMQ:Port"] ?? "5672";
            if (!int.TryParse(portString, out var port))
            {
                Console.WriteLine("Porta do RabbitMQ inválida. Usando padrão: 5672");
                port = 5672;
            }

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = port,
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.QueueDeclare(queue: "stock_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                    Console.WriteLine("Conectado ao RabbitMQ com sucesso");

                    var consumer = new EventingBasicConsumer(_channel);
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"Mensagem recebida: {message}");

                        try
                        {
                            using var scope = _services.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<StockContext>();
                            Console.WriteLine("Contexto do banco criado com sucesso");

                            var parts = message.Split("reduce stock for product ");
                            if (parts.Length > 1)
                            {
                                var productParts = parts[1].Split(" by ");
                                if (productParts.Length == 2 && int.TryParse(productParts[0], out var productId) && int.TryParse(productParts[1], out var quantity))
                                {
                                    Console.WriteLine($"Processando: ProductId={productId}, Quantity={quantity}");
                                    var product = await context.Products.FindAsync(productId);
                                    if (product != null)
                                    {
                                        Console.WriteLine($"Produto encontrado: Id={product.Id}, QuantityInStock={product.QuantityInStock}");
                                        product.QuantityInStock -= quantity;
                                        await context.SaveChangesAsync();
                                        Console.WriteLine($"Estoque atualizado: ProductId={productId}, QuantityInStock={product.QuantityInStock}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Produto não encontrado: ProductId={productId}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Formato de mensagem inválido: {message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Formato de mensagem inválido: {message}");
                            }

                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao processar mensagem: {ex.Message}\nStackTrace: {ex.StackTrace}");
                            _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                        }
                    };

                    _channel.BasicConsume(queue: "stock_queue", autoAck: false, consumer: consumer);
                    Console.WriteLine("Consumidor iniciado para a fila stock_queue");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao conectar ao RabbitMQ: {ex.Message}. Tentando novamente em 5 segundos...");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fechar conexão com RabbitMQ: {ex.Message}");
            }
            base.Dispose();
        }
    }
}