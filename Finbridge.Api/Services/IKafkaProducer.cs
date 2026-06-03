using System.Threading.Tasks;
using Finbridge.Core.Models;

namespace Finbridge.Api.Services
{
    public interface IKafkaProducer
    {
        Task ProduceUserEventAsync(User user);
    }
}