namespace MassTransitBatchRetryTest;

using MassTransit;
using Microsoft.Extensions.Logging;

public class TestConsumer : IConsumer<Batch<TestMessage>>
{
    private readonly ILogger<TestConsumer> _logger;

    public TestConsumer(ILogger<TestConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Batch<TestMessage>> context)
    {
        string ids = string.Join(",", context.Message.Select(m => m.Message.Id));

        _logger.LogDebug(
            "Working on a batch of {MessageCount} messages, Ids [{Ids}], retryAttempt [{RetryAttempt}], retryCount [{RetryCount}]",
            context.Message.Length,
            ids,
            context.GetRetryAttempt(),
            context.GetRetryCount());

        throw new Exception("something went wrong");

        // return Task.CompletedTask;
    }
}