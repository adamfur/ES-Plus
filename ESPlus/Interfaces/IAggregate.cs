
using System;
using System.Collections.Generic;

namespace ESPlus.Interfaces
{
    public interface IAggregate
    {
        Type InitialType();
        long Version { get; set; }
        string Id { get; }
        void ApplyChange(object @event);
        IEnumerable<object> TakeUncommittedEvents();
    }

    public interface IAggregate<T> where T : IIdObject
    {
        Type InitialType();
        long Version { get; set; }
        T Id { get; }
        void ApplyChange(object @event);
        IEnumerable<object> TakeUncommittedEvents();
    }

    public interface IIdObject
    {
        string Value { get; }
    }

    // public interface ICopyable<T>
    // {
    //     T Copy();
    // }

    // public interface ISnapshot
    // {
    //     int Version { get; }
    // }

    // public interface ISnapshotable<T>
    //     where T : ISnapshot
    // {
    //     T Snapshot();
    // }
}
