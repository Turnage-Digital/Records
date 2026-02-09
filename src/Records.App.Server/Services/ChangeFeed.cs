using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace Records.App.Server.Services;

public sealed class ChangeFeed
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<Guid, Channel<string>> subscribers = new();

    public async IAsyncEnumerable<string> Subscribe(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        subscribers[subscriberId] = channel;

        try
        {
            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return message;
            }
        }
        finally
        {
            RemoveSubscriber(subscriberId);
        }
    }

    public ValueTask PublishAsync(object @event, CancellationToken cancellationToken = default)
    {
        if (subscribers.IsEmpty)
        {
            return ValueTask.CompletedTask;
        }

        var payload = new ChangeFeedMessage(
            @event.GetType().FullName ?? @event.GetType().Name,
            @event,
            DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(payload, SerializerOptions);

        foreach (var subscriber in subscribers.Values)
        {
            _ = subscriber.Writer.TryWrite(json);
        }

        return ValueTask.CompletedTask;
    }

    private void RemoveSubscriber(Guid subscriberId)
    {
        if (subscribers.TryRemove(subscriberId, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    private sealed record ChangeFeedMessage(
        string Type,
        object Data,
        DateTimeOffset OccurredOn
    );
}
