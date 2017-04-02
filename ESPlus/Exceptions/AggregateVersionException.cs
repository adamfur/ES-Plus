using System;

namespace ESPlus
{
    public class AggregateVersionException : AggregateException
    {
        public string Id { get; set; }
        public Type Type { get; set; }
        public long Version { get; set; }
        public long ExpectedVersion { get; set; }

        public AggregateVersionException(string id, Type type, long version, long expectedVersion)
        {
            Id = id;
            Type = type;
            Version = version;
            ExpectedVersion = expectedVersion;
        }
    }  
}