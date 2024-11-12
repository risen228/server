namespace Argon.Api.Grains;

using Interfaces;
using Orleans.Streams;

#if DEBUG

public class TestGrain([PersistentState("input", "RedisStorage")] IPersistentState<SomeInput> inputStore) : Grain, ITestGrain
{
#region Implementation of ITestGrain

    public async Task<SomeInput> CreateSomeInput(SomeInput input)
    {
        inputStore.State = input;
        await inputStore.WriteStateAsync();
        return inputStore.State;
    }

    public async Task<SomeInput> UpdateSomeInput(SomeInput input)
    {
        inputStore.State = input;
        await inputStore.WriteStateAsync();
        return inputStore.State;
    }

    public async Task<SomeInput> DeleteSomeInput()
    {
        var obj = inputStore.State;
        await inputStore.ClearStateAsync();
        return obj;
    }

    public async Task<SomeInput> GetSomeInput()
    {
        await inputStore.ReadStateAsync();
        return inputStore.State;
    }

#endregion
}

public class StreamProducer : Grain, IStreamProducer
{
#region Implementation of IStreamProducer

    [Obsolete("Obsolete")]
    public Task StartStream()
    {
        var streamProvider = this.GetStreamProvider("TestProvider");
        var streamId       = StreamId.Create("@", Guid.Parse("3dc47106-3ca0-47a0-9be1-4891885b3f0d"));
        var stream         = streamProvider.GetStream<SomeInput>(streamId);
        stream.OnNextAsync(new SomeInput(Random.Shared.Next(), "test"));
        return Task.CompletedTask;
    }

#endregion
}

[ImplicitStreamSubscription("@")]
public class StreamConsumer(ILogger<StreamConsumer> logger) : Grain, IStreamConsumer
{
#region Implementation of IStreamConsumer

    public async Task ConsumeStream()
    {
        var streamProvider = this.GetStreamProvider("TestProvider");

        var streamId = StreamId.Create("@", Guid.Parse("3dc47106-3ca0-47a0-9be1-4891885b3f0d"));
        var stream   = streamProvider.GetStream<SomeInput>(streamId);
        await stream.SubscribeAsync(async observer =>
        {
            foreach (var sequentialItem in observer) logger.LogInformation($"Received: {sequentialItem.Item}");
        });
    }

#endregion
}

#endif