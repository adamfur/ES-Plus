using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.Wyrm;
using Xunit;

namespace ESPlus.IntegrationTests.Repositories.Implementations
{
    public class WyrmRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            var connection = CreateDriver();

            return new WyrmRepository(connection, new WyrmAggregateZeroRenamer());            
        }

        private static WyrmDriver CreateDriver()
        {
            return new WyrmDriver("localhost:8888", new EventJsonSerializer());
        }

        [Fact]
        public async Task SaveAsync_ReadFromTo_OutOfRange()
        {
            var driver = CreateDriver();
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString());

            await Repository.SaveAsync(aggregate);
            var result = await AsyncAny(driver.EnumerateAll(DateTime.Now.AddDays(1), DateTime.Now.AddDays(2),
                CancellationToken.None));

            Assert.False(result);
        }
        
        [Fact]
        public async Task SaveAsync_ReadFromTo_InRange()
        {
            var driver = CreateDriver();
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString());

            await Repository.SaveAsync(aggregate);
            var result = await AsyncAny(driver.EnumerateAll(DateTime.Now.AddDays(-1), DateTime.Now,
                CancellationToken.None));

            Assert.True(result);
        }

        private async Task<bool> AsyncAny(IAsyncEnumerable<WyrmEvent2> enumerateAll)
        {
            await foreach (var item in enumerateAll)
            {
                return true;
            }
            
            return false;
        }
    }        
}
