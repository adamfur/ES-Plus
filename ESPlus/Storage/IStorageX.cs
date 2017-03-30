using System;

namespace ESPlus.Storage
{
    public interface IStorageX : IFlushable
    {
        void Update<T>(string path, Action<T> action) where T : new();
        T Get<T>(string path) where T : new();
        void Put<T>(string path, T graph);
    }
}