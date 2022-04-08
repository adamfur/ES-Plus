using System;
using System.Collections.Generic;

namespace ESPlus.Interfaces
{
    public interface IAggregate<T>
    {
        Type InitialType();
        long Version { get; set; }
        T Id { get; }
        void ApplyChange(object @event);
        IEnumerable<object> TakeUncommittedEvents();
    }

    public interface IAggregate : IAggregate<string>
    {
    }
}
