using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Infrastructure.Sql;

public sealed class AgentThreadStream : IAgentThreadStream
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<AgentStreamEventDto>>> _subscriptions =
        new(StringComparer.Ordinal);

    public async IAsyncEnumerable<AgentStreamEventDto> SubscribeAsync(
        string threadId,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var channel = Channel.CreateUnbounded<AgentStreamEventDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        var subscriptionId = Guid.NewGuid();
        var threadSubscriptions = _subscriptions.GetOrAdd(
            threadId,
            _ => new ConcurrentDictionary<Guid, Channel<AgentStreamEventDto>>());
        threadSubscriptions[subscriptionId] = channel;

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            if (_subscriptions.TryGetValue(threadId, out var subscribers))
            {
                subscribers.TryRemove(subscriptionId, out _);
                if (subscribers.IsEmpty)
                {
                    _subscriptions.TryRemove(threadId, out _);
                }
            }

            channel.Writer.TryComplete();
        }
    }

    public ValueTask PublishAsync(
        string threadId,
        AgentStreamEventDto streamEvent,
        CancellationToken cancellationToken = default
    )
    {
        if (_subscriptions.TryGetValue(threadId, out var subscribers))
        {
            foreach (var channel in subscribers.Values)
            {
                channel.Writer.TryWrite(streamEvent);
            }
        }

        return ValueTask.CompletedTask;
    }
}
