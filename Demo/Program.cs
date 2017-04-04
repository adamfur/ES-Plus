using System;
using System.Threading;
using ESPlus.Subscribers;
using EventStore.ClientAPI;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500";
            //var _eventStoreConnection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));
            var eventStoreConnection = EventStoreConnection.Create(connectionString);
            eventStoreConnection.ConnectAsync().Wait();
            IEventFetcher eventFetcher = new EventFetcher(eventStoreConnection);
            eventFetcher = new CachedEventFetcher(eventFetcher);
            var manager = new SubscriptionManager(eventFetcher, workerThreads: 1);

            var client1 = manager.Subscribe(-1L);
            var client2 = manager.Subscribe(8078189L);
            var client3 = manager.Subscribe(8110420L);
            new Thread(() => Client("Client1", client1)).Start();
            new Thread(() => Client("Client2", client2)).Start();
            new Thread(() => Client("Client3", client3)).Start();
            manager.Start();
            Console.ReadLine();
        }

        private static void Client(string clientName, ISubscriptionClient client)
        {
            foreach (var @event in client)
            {
                //Console.WriteLine($"{clientName}: {@event.Position}");
            }
        }
    }
}
