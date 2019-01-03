using System;

namespace ESPlus
{
    public class AggregateVersionException : AggregateException
    {
        public string Id { get; }
        public Type Type { get; }
        public long Version { get; }
        public long ExpectedVersion { get; }

        public AggregateVersionException(string id, Type type, long version, long expectedVersion)
        {
            Id = id;
            Type = type;
            Version = version;
            ExpectedVersion = expectedVersion;
        }
    }  
}