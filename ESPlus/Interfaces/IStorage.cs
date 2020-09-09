using System;
using System.Collections.Generic;
using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put(string path, HasObjectId item);
        void Delete(string path);
        T Get<T>(string path) where T : HasObjectId;        
        void Reset();
        IAsyncEnumerable<byte[]> SearchAsync(string database, long[] parameters);
    }
}