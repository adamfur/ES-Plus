using ESPlus.Interfaces;
using ESPlus;
using EventStore.ClientAPI;
using Xunit;
using ESPlus.Aggregates;
using System.Text;
using System.Linq;
using System;

namespace ESPlus.IntegrationTests
{
    // public class HashEvent
    // {
    //     public string Checksum { get; set; }
    // }

    // public class ChecksumAggregate : AggregateBase
    // {
    //     public string Checksum { get; set; }

    //     public ChecksumAggregate(string id)
    //         : base(id)
    //     {
    //         Checksum = Hash(id);
    //     }

    //     public void TriggerEvent()
    //     {
    //         ApplyChange(new HashEvent
    //         {
    //             Checksum = Hash(Checksum)
    //         });
    //     }

    //     protected void Apply(HashEvent @event)
    //     {
    //         Checksum = @event.Checksum;
    //     }

    //     private static string Hash(string input)
    //     {
    //         using (var provider = System.Security.Cryptography.SHA1.Create())
    //         {
    //             var hash = provider.ComputeHash(Encoding.UTF8.GetBytes(input));
    //             return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
    //         }
    //     }
    // }

    // public class RepostoryTests
    // {
    //     private IRepository _repository;
    //     private ChecksumAggregate _aggregate;
    //     private string _id;

    //     public RepostoryTests()
    //     {
    //         var connectionString = "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500";
    //         var eventStoreConnection = EventStoreConnection.Create(connectionString);
    //         eventStoreConnection.ConnectAsync().Wait();
    //         _repository = new GetEventStoreRepository(eventStoreConnection, new EventJsonSerializer());
    //         _id = Guid.NewGuid().ToString();
    //         _aggregate = new ChecksumAggregate(_id);

    //         GetEventStoreRepository.Register<HashEvent>();
    //     }

    //     [Fact]
    //     public void Save_NoEvents_Pass()
    //     {
    //         _repository.SaveAsync(_aggregate);
    //     }

    //     [Fact]
    //     public void Save_OneEvent_Pass()
    //     {
    //         _aggregate.TriggerEvent();
    //         _repository.SaveAsync(_aggregate);
    //     }

    //     [Fact(Skip = "Fails for some reason")]
    //     public void Save_AppendEvents_Pass()
    //     {
    //         _aggregate.TriggerEvent();
    //         _repository.SaveAsync(_aggregate);
    //         _aggregate.TriggerEvent();
    //         _repository.SaveAsync(_aggregate);
    //     }

    //     [Fact]
    //     public void GetById_LoadSaveAggregate_ReplayedToPreviousState()
    //     {
    //         Repeat(() => _aggregate.TriggerEvent(), times: 1024);
    //         var checksum = _aggregate.Checksum;
    //         var version = _aggregate.Version;

    //         _repository.SaveAsync(_aggregate);
    //         var result = _repository.GetByIdAsync<ChecksumAggregate>(_id);

    //         Assert.Equal(checksum, result.Checksum);
    //         Assert.Equal(version, result.Version);
    //     }

    //     [Fact]
    //     public void Save_ReloadAggregateAndResave_Pass()
    //     {
    //         _aggregate.TriggerEvent();
    //         _repository.SaveAsync(_aggregate);

    //         var aggregate = _repository.GetByIdAsync<ChecksumAggregate>(_id);
    //         aggregate.TriggerEvent();
    //         _repository.SaveAsync(aggregate);
    //     }

    //     private void Repeat(Action action, int times)
    //     {
    //         for (var i = 0; i < times; ++i)
    //         {
    //             action();
    //         }
    //     }
    // }
}