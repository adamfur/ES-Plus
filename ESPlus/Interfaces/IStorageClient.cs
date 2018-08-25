using System;

namespace ESPlus.Storage
{
    public interface IStorageClient : IFlushable
    {
        T Get<T>(string path) where T : HasObjectId, new();
        void Put<T>(string path, T graph) where T : HasObjectId;
        void Update<T>(string path, Action<T> action) where T : HasObjectId, new();
        void Reset();
    }
}