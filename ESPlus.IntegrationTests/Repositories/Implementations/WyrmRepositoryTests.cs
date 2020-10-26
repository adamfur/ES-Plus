using ESPlus.Interfaces;
using ESPlus.Repositories;
using ESPlus.Wyrm;

namespace ESPlus.IntegrationTests.Repositories.Implementations
{
    public class WyrmRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            var eventSerializer = new EventJsonSerializer();
            var connection = new WyrmDriver("localhost:8888", eventSerializer);

            return new WyrmRepository(connection);            
        }
    }        
}
