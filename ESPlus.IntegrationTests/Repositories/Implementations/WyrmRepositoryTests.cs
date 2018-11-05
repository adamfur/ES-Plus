using ESPlus.Interfaces;
using ESPlus.Repositories;
using ESPlus.Wyrm;

namespace ESPlus.IntegrationTests.Repositories.Implementations
{
    public class WyrmRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            var connection = new WyrmDriver("localhost:8888", new EventJsonSerializer());
            var eventSerializer = new EventJsonSerializer();

            return new WyrmRepository(connection);            
        }
    }        
}
