using ESPlus.Interfaces;
using ESPlus.Repositories;
using EventStore.ClientAPI;

namespace ESPlus.Tests.Repositories
{
    public class GetEventStoreRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            var eventSerializer = new EventJsonSerializer();
            var connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113; HeartBeatTimeout=500";
            var eventStoreConnection = EventStoreConnection.Create(connectionString);
            eventStoreConnection.ConnectAsync().Wait();

            return new GetEventStoreRepository(eventStoreConnection, eventSerializer);
        }
    }
}