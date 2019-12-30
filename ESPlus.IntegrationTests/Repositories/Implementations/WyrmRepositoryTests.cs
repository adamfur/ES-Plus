using ESPlus.Interfaces;
using ESPlus.Misc;
using ESPlus.Repositories;
using ESPlus.Wyrm;

namespace ESPlus.IntegrationTests.Repositories.Implementations
{
    public class WyrmRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            var eventTypeResolver = EventTypeResolver.Default();

            eventTypeResolver.RegisterType(typeof(FileAddedEvent));
            var connection = new WyrmDriver("192.168.1.2:9999", new EventJsonSerializer(eventTypeResolver), "key");

            return new WyrmRepository(new WyrmStore(connection), connection);            
        }
    }        
}
