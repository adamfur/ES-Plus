using ESPlus.Interfaces;
using ESPlus.Repositories;

namespace ESPlus.Tests.Repositories.Implementations
{
    public class InMemoryRepositoryTests : RepositoryTests
    {
        protected override IWyrmRepository Create()
        {
            return new InMemoryRepository();
        }
    }
}