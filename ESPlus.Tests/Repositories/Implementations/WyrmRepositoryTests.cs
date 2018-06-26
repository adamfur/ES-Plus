using ESPlus.Interfaces;
using ESPlus.Repositories;

namespace ESPlus.Tests.Repositories.Implementations
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
