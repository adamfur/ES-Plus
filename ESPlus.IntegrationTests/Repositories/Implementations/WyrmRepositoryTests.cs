using ESPlus.Interfaces;
using ESPlus.Repositories;
using ESPlus.Wyrm;

namespace ESPlus.IntegrationTests.Repositories.Implementations
{
    public class WyrmRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            var connection = new WyrmConnection();
            var eventSerializer = new EventJsonSerializer();

            return new WyrmRepository(connection, eventSerializer);            
        }
    }        
}
