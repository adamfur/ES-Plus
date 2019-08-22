using System;

namespace ESPlus.Exceptions
{
    public class AggregateNotFoundException : AggregateException
    {
        public string Id { get; }
        public Type Type { get; }

        public AggregateNotFoundException(string id, Type type)
        {
            Id = id;
            Type = type;
        }
    }    
}