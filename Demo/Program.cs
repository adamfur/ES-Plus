using System;
using ESPlus;
using System.Threading;
using ESPlus.Subscribers;
using System.Net;
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
            //var eventFetcher = new CachedEventFetcher(new DummyEventFetcher());
            var eventFetcher = new CachedEventFetcher(new EventFetcher(eventStoreConnection));
            var manager = new SubscriptionManager(eventFetcher, 3);

            var client1 = manager.Subscribe(0L);
            var client2 = manager.Subscribe(13L);
            new Thread(() => Client("Client1", client1)).Start();
            new Thread(() => Client("Client2", client2)).Start();
            manager.Start();
        }

        private static void Client(string clientName, ISubscriptionClient client)
        {
            foreach (var @event in client)
            {
                //Console.WriteLine($"{clientName}: {@event}");
            }
        }
    }
}
