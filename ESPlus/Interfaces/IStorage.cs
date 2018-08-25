using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put(string path, HasObjectId item);
        T Get<T>(string path) where T : HasObjectId;        
        void Reset();
    }
}