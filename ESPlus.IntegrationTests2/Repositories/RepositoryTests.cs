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
        private WyrmConnection _connection;

        public RepositoryTests()
        {
            _connection = new WyrmConnection();
        }

        [Fact]
        public async Task xyz_xyz_xyz()
        {
            int count = 0;
            var watch = Stopwatch.StartNew();

            foreach (var item in _connection.EnumerateAll(Position.Start))
            {
                if (item.Offset >= 1339250)
                {
                    //break;
                }

                // if (item.Offset % 1000 == 0)
                {
                    Console.WriteLine($"{item.Offset} {item.EventType}");
                }
                // ++count;
            }

            Console.WriteLine($"Elapsed: {watch.ElapsedMilliseconds}ms");
        }
    }
}