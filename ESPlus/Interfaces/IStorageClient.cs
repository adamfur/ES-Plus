using System;

namespace ESPlus.Storage
{
    public interface IStorageClient : IFlushable
    {
        T Get<T>(string path) where T : new();
        void Put<T>(string path, T graph);
        void Update<T>(string path, Action<T> action) where T : new();
        void Reset();
    }
}