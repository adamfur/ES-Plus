namespace ESPlus.Interfaces
{
    public interface IRepository : IStore
    {
        IRepositoryTransaction BeginTransaction();
    }
}