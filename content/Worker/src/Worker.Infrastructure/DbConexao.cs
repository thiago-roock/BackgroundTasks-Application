using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Data;


namespace Worker.Infrastructure
{
    public sealed class DbConexao : IDisposable
    {
        public IDbConnection Connection { get; }
        public IDbTransaction? Transaction { get; set; }

        public DbConexao(IConfiguration configuration, IHostEnvironment env)
        {
            // Sempre pega de ConnectionStrings (appsettings)
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string 'DefaultConnection' não encontrada para o ambiente {env.EnvironmentName}."
                );
            }

            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        public void Dispose() => Connection?.Dispose();
    }
}
