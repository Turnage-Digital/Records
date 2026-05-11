using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.OpenAI;

namespace Records.Agents.Tests.Services;

public sealed class AgentThreadStreamTests
{
    [Test]
    public async Task SubscribeAsync_ShouldYieldPublishedEvents_ForMatchingThread()
    {
        var stream = new AgentThreadStream();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in stream.SubscribeAsync("thread-1", cancellationTokenSource.Token))
            {
                return item;
            }

            throw new InvalidOperationException("No event was received.");
        }, cancellationTokenSource.Token);

        await Task.Delay(50, cancellationTokenSource.Token);

        await stream.PublishAsync("thread-1", new AgentStreamEventDto
        {
            Type = "assistant_final_message",
            OccurredAt = DateTimeOffset.UtcNow,
            Message = "Done"
        }, cancellationTokenSource.Token);

        var received = await readTask;

        Assert.That(received.Type, Is.EqualTo("assistant_final_message"));
        Assert.That(received.Message, Is.EqualTo("Done"));
    }
}
