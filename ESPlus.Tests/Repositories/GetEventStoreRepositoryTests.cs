using ESPlus.Interfaces;
using ESPlus.Repositories;
using EventStore.ClientAPI;

namespace ESPlus.Tests.Repositories
{
    public class GetEventStoreRepositoryTests : RepositoryTests
    {
        private static IEventStoreConnection _eventStoreConnection;

        static GetEventStoreRepositoryTests()
        {
            var connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113; HeartBeatTimeout=500";
            _eventStoreConnection = EventStoreConnection.Create(connectionString);
            _eventStoreConnection.ConnectAsync().Wait();
        }
        protected override IRepository Create()
        {
            var eventSerializer = new EventJsonSerializer();

            return new GetEventStoreRepository(_eventStoreConnection, eventSerializer);
        }
    }
}