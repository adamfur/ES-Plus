using System;

namespace ESPlus.Exceptions
{
    public class AggregateNotFoundException : AggregateException
    {
        public Type Type { get; }

        public AggregateNotFoundException(string id, Type type)
        : base($"Aggregate with id '{id}' not found")
        {
            Type = type;
        }
    }    
}