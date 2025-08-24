using System.Threading.Tasks;

namespace Worker.Domain.Infrastructure.ExternalServices
{
    public interface IRabbitMqService
    {
        Task<string> LerMensagemAsync(string nomeFila);
    }
}
