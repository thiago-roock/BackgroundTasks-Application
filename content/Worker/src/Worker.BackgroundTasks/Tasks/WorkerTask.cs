using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Worker.Domain.Infrastructure.ExternalServices;
using Worker.Domain.Infrastructure.Repository;
using Worker.Domain.Infrastructure.Repository.Models;

namespace Worker.BackgroundTasks.Tasks
{
    public class WorkerTask : BackgroundService
    {
        private readonly ILogger<WorkerTask> _logger;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        public WorkerTask(ILogger<WorkerTask> logger, IRabbitMqService rabbitMqService, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _rabbitMqService = rabbitMqService;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            if (string.IsNullOrEmpty(configuration["RBMQ_NOME_FILA"]))
                throw new ArgumentException("O parametro RBMQ_NOME_FILA não pode ser null ou empty.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker iniciado às: {time}", DateTimeOffset.Now);

            string mensagem = await _rabbitMqService.LerMensagemAsync(_configuration["RBMQ_NOME_FILA"]);
            if (string.IsNullOrEmpty(mensagem))
            {
                _logger.LogError($"Falha ao iniciar consumo da fila {_configuration["RBMQ_NOME_FILA"]}.");
                using (var scope = _scopeFactory.CreateScope())
                {
                    var logsRepository = scope.ServiceProvider.GetRequiredService<ILogsRepository>();

                    // Usa o repositório
                    logsRepository.SalvarLog(new Log
                    {
                        DataHora = DateTime.Now,
                        Maquina = Environment.MachineName,
                        Mensagem = $"Falha ao iniciar consumo da fila {_configuration["RBMQ_NOME_FILA"]}.",
                        Tipo = "ERRO",
                        Usuario = "SYSTEM"
                    });
                }
                
            }

            // Mantém o worker rodando até o cancelamento
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
