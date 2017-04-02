using System.Collections.Generic;

namespace ESPlus.Interfaces
{
    public interface IAggregate
    {
        int Version { get; }
        string Id { get; }
        void ApplyChange(object @event);
        IEnumerable<object> TakeUncommittedEvents();
    }
}
