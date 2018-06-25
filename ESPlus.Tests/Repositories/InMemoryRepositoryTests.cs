using ESPlus.Interfaces;
using ESPlus.Repositories;

namespace ESPlus.Tests.Repositories
{
    public class InMemoryRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            return new InMemoryRepository();
        }
    }
}