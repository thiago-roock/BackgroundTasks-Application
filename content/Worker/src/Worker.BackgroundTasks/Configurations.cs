using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using System;
using System.Diagnostics.CodeAnalysis;
using Worker.BackgroundTasks.Tasks;
using Worker.Domain.Infrastructure.ExternalServices;
using Worker.Domain.Infrastructure.Repository;
using Worker.Infrastructure;
using Worker.Infrastructure.Infrastructure.ExternalServices;
using Worker.Infrastructure.Infrastructure.Repository;
namespace Worker.BackgroundTasks
{
    [ExcludeFromCodeCoverage]
    public static class Configurations
    {
        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddInfrastructureServices(configuration);
            return services;
        }

        private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddLogging()
                .AddHealthChecks();

            services.AddScoped<DbConexao>();

            // Registra escopos de repositórios
            services.AddScoped<ILogsRepository, LogsRepository>();

            // Configura fábrica do RabbitMQ (vem do appsettings.json)
            services.AddSingleton(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMq:HostName"] ?? "localhost",
                    UserName = configuration["RabbitMq:UserName"] ?? "guest",
                    Password = configuration["RabbitMq:Password"] ?? "guest",
                    Port = int.Parse(configuration["RabbitMq:Port"] ?? "5672"),
                };
                return factory;
            });


            // Registra serviço
            services.AddSingleton<IRabbitMqService, RabbitMqService>();

            // OpenTelemetry + OTLP (Jaeger v2)
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("Worker")) // Define o nome do serviço
                .WithTracing(builder =>
                {
                    builder
                        // Instrumentação automática para ASP.NET Core
                        .AddAspNetCoreInstrumentation()
                        // Instrumentação automática para HttpClient
                        .AddHttpClientInstrumentation()
                        // Exporta spans para o console (opcional, útil para debug)
                        .AddConsoleExporter()
                        // Spans manuais via ActivitySource("Worker")
                        .AddSource("Worker")
                       // Exporta spans para Jaeger via OTLP/gRPC
                       .AddOtlpExporter(opt =>
                       {
                           var jaegerHost = configuration["Jaeger:Host"] ?? "localhost";
                           opt.Endpoint = new Uri($"http://{jaegerHost}:4317"); // gRPC
                       });

                });
            services.AddHostedService<WorkerTask>();
            return services;
        }
    }
}