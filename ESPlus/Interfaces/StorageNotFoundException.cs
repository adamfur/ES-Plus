#nullable enable
using System;
using System.Runtime.Serialization;

namespace ESPlus.Interfaces
{
    public class StorageNotFoundException : Exception
    {
        public StorageNotFoundException()
        {
        }

        protected StorageNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public StorageNotFoundException(string? message)
            : base(message)
        {
        }

        public StorageNotFoundException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}