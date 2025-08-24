using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Worker.Domain.Infrastructure.ExternalServices;

namespace Worker.Infrastructure.Infrastructure.ExternalServices
{
    public class RabbitMqService : IRabbitMqService, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMqService> _logger;
        private readonly IConfiguration _configuration;
        private static readonly ActivitySource ActivitySource = new("Worker.RabbitMq");

        public RabbitMqService(ConnectionFactory factory, ILogger<RabbitMqService> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
            _logger.LogInformation("Carregando configurações do RabbitMQ...");
            // Cria conexão e canal (aqui ainda síncrono no construtor, mas vindo da DI)
            _connection = factory.CreateConnectionAsync("WorkerRabbitMqService").GetAwaiter().GetResult();
            _logger.LogInformation("Conexão com RabbitMQ estabelecida com sucesso.");
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _logger.LogInformation("Canal com RabbitMQ criado com sucesso.");
            // Declara fila
            _channel.QueueDeclareAsync(
                queue: _configuration["RBMQ_NOME_FILA"],
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: CancellationToken.None
            ).GetAwaiter().GetResult();
            _logger.LogInformation("Fila declarada com sucesso.");
            _logger.LogInformation("Configurações carregadas com sucesso:");
        }

        public async Task<string> LerMensagemAsync(string nomeFila)
        {
            using var activity = ActivitySource.StartActivity("RabbitMQ LerMensagem", ActivityKind.Producer);
            string json = string.Empty;
            try
            {
                // Propagar o trace context (traceparent, tracestate)
                if (activity != null)
                {
                    var context = activity.Context;
                    // Propaga CodigoTracing
                    var codigoTracing = Activity.Current?.GetBaggageItem("CodigoTracing") ?? Guid.NewGuid().ToString();
                    activity?.SetTag("CodigoTracing", codigoTracing);
                }

                // Publica mensagem de forma assíncrona
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    json = Encoding.UTF8.GetString(ea.Body.ToArray());

                    // Desserializa de volta para objeto
                    _logger.LogInformation($"Mensagem: [{JsonSerializer.Deserialize<Object>(json)}] recebida com sucesso.");
                    await Task.Yield();
                };

                await _channel.BasicConsumeAsync(queue: _configuration["RBMQ_NOME_FILA"],
                                      autoAck: true,
                                      consumer: consumer);

                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exceção: {ex.GetType().FullName} | " +
                           $"Mensagem: {ex.Message}");
                return json;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }
}

