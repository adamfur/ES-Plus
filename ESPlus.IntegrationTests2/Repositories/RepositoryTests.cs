using System;
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

            foreach (var item in _connection.EnumerateAll("0000000000000000000000000000000000000000000000000000000000000000"))
            {
                Console.WriteLine($"{++count} {item.EventType}");
            }
        }        
    }
}