using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.Tests.Repositories;
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
            var aggregate = new ESPlus.Tests.Repositories.Aggregates.DummyAggregate(Guid.NewGuid().ToString(), 0);

            await Repository.SaveAsync(aggregate);

            Assert.Empty(await driver.EnumerateAll(DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), default).ToListAsync());
        }
        
        [Fact]
        public async Task SaveAsync_ReadFromTo_InRange()
        {
            var driver = CreateDriver();
            var aggregate = new ESPlus.Tests.Repositories.Aggregates.DummyAggregate(Guid.NewGuid().ToString(), 0);

            await Repository.SaveAsync(aggregate);

            Assert.NotEmpty(await driver.EnumerateAll(DateTime.Now.AddDays(-1), DateTime.Now, default).ToListAsync());
        }
    }        
}
