using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ESPlus.Wyrm;
using Xunit;

namespace ESPlus.IntegrationTests2.Repositories
{
    public class RepositoryTests
    {
        private WyrmDriver _connection;

        public RepositoryTests()
        {
            _connection = new WyrmDriver(Environment.GetEnvironmentVariable("EVENTSTORE") ?? "localhost:8888", new EventJsonSerializer());
        }

        [Fact]
        public async Task xyz_xyz_xyz()
        {
            var watch = Stopwatch.StartNew();

            foreach (var item in _connection.EnumerateAll(Position.Start))
            {
                Console.WriteLine($"{item.Offset} {item.EventType}");

                if (item.IsAhead)
                {
                    break;
                }
            }

            Console.WriteLine($"Elapsed: {watch.ElapsedMilliseconds}ms");
        }
    }
}