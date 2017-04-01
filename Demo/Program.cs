using System;
using ESPlus;
using System.Threading;
using ESPlus.Subscribers;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var eventFetcher = new EventFetcher();
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
