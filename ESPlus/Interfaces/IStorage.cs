using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put(string path, object item);
        object Get(string path);        
        void Reset();
    }
}