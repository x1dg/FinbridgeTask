using Confluent.Kafka;
using Finbridge.Core.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Tasks;

namespace Finbridge.Api.Services
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly ProducerConfig _config;
        private readonly string _topic;

        public KafkaProducer(IOptions<KafkaSettings> settings)
        {
            _config = new ProducerConfig
            {
                BootstrapServers = settings.Value.BootstrapServers
            };
            _topic = settings.Value.Topic;
        }

        public async Task ProduceUserEventAsync(User user)
        {
            using var producer = new ProducerBuilder<Null, string>(_config).Build();
            var userEvent = new
            {
                user.Id,
                user.FullName,
                user.DateOfBirth,
                user.PlaceOfBirth,
                user.Balance,
                EventTimestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(userEvent);
            await producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
            producer.Flush(TimeSpan.FromSeconds(10));
        }
    }
}