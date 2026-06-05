using Finbridge.Api.Services;
using Polly;
using Polly.Registry;

namespace Finbridge.Api.Resilience;

internal sealed class ResilientKafkaProducer : IKafkaProducer
{
    private readonly IKafkaProducer _inner;
    private readonly ResiliencePipeline _pipeline;

    public ResilientKafkaProducer(
        IKafkaProducer inner,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _inner = inner;
        _pipeline = pipelineProvider.GetPipeline(ResiliencePipelines.KafkaProducer);
    }

    public async Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(
            async ct => await _inner.ProduceAsync(topic, payload, ct),
            cancellationToken);
    }
}
