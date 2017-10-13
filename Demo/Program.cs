using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Misc;
using ESPlus.Scheduling;
using ESPlus.Subscribers;
using EventStore.ClientAPI;
// using ESPlus.Scheduling;

namespace Demo
{
    public interface IFoo
    {
        int GetNum();
        string GetDay();
        int Buz { get; set; }
    }

    // public class Foo : IFoo
    // {
    //     public int GetNum()
    //     {
    //         return -1;
    //     }

    //     public string GetDay()
    //     {
    //         return "abc";
    //     }

    //     public int Buz { get; set; }
    // }

    [EventIdentifier(EventId = "Apa")]
    public class Food
    {

    }

    class Program
    {
        static void Main(string[] args)
        {
            var scheduler = new Scheduler();
            var id = Guid.NewGuid();

            scheduler.AddTrigger(id, "0 0 0 * * *", () => { });
            scheduler.AddTrigger(Guid.NewGuid(), "0 0 0 * * *", () => { });
            scheduler.Fire(id);

            foreach (var job in scheduler.Jobs())
            {
                Console.WriteLine($"Id: {job.Id}, Next: {job.Next:yyyy-MM-dd HH:mm:ss}, Left: {job.Left}, Last: {job.Last:yyyy-MM-dd HH:mm:ss}, Passed: {job.Passed:yyyy-MM-dd HH:mm:ss}");
            }

            return;
            var foo = new EventTypeResolver();

            foo.RegisterTypes(Assembly.GetExecutingAssembly().GetTypes());

            var hej = foo.ResolveType("Demo.Foodx", "Foodx", "Apa");
            Console.WriteLine(":" + hej);

            // var connectionString = "ConnectTo=tcp://admin:changeit@192.168.1.142:1113; HeartBeatTimeout=500";
            // var eventStoreConnection = EventStoreConnection.Create(connectionString);
            // eventStoreConnection.ConnectAsync().Wait();
            // IEventFetcher eventFetcher = new EventFetcher(eventStoreConnection);
            // // eventFetcher = new CachedEventFetcher(eventFetcher);
            // var manager = new SubscriptionManager(eventFetcher, workerThreads: 2);

            // var client1 = manager.Subscribe(Position.Start);
            // var client2 = manager.Subscribe(Position.Start);
            // // var client3 = manager.Subscribe(Position.Start);
            // // var client4 = manager.Subscribe(Position.Start);
            // new Thread(() => Client("Client1", client1)).Start();
            // new Thread(() => Client("Client2", client2)).Start();
            // // new Thread(() => Client("Client3", client3)).Start();
            // // new Thread(() => Client("Client4", client4)).Start();
            // manager.Start();
            // Console.ReadLine();
            /*            
                var connectionString = "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500";
                //var _eventStoreConnection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));
                var eventStoreConnection = EventStoreConnection.Create(connectionString);
                eventStoreConnection.ConnectAsync().Wait();
                IEventFetcher eventFetcher = new EventFetcher(eventStoreConnection);
                eventFetcher = new CachedEventFetcher(eventFetcher);
                var manager = new SubscriptionManager(eventFetcher, workerThreads: 2);

                var client1 = manager.Subscribe(Position.Start);
                var client2 = manager.Subscribe(Position.Start);
                //var client3 = manager.Subscribe(116508733L.ToPosition());
                new Thread(() => Client("Client1", client1)).Start();
                new Thread(() => Client("Client2", client2)).Start();
                // new Thread(() => Client("Client3", client3)).Start();
                manager.Start();
                Console.ReadLine();
            */
            // var sched = new Scheduler();

            // sched.Start();
            // sched.AddTrigger("0 0 * ? * *", () => Console.WriteLine(DateTime.Now));
            // //await sched.AddTrigger("1/2 * * ? * *", () => Console.WriteLine("Hello2"));
            // Console.ReadLine();
            // //sched.AddTrigger("/0.125 * * ? * *", () => Console.WriteLine("pulse"));
        }

        private static void Client(string clientName, ISubscriptionClient client)
        {
            var start = DateTime.Now;
            var count = 0L;

            foreach (var @event in client)
            {
                count++;
                //if (count % 500 == 0)
                Console.WriteLine($"{DateTime.Now:HH:mm:ss}: (e: {count:### ### ###}, eps: {count / (DateTime.Now.Subtract(start).Seconds + 1)}) {clientName}: {@event.Position}");
            }
        }
    }
}
