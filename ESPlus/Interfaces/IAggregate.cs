﻿
using System;
using System.Collections.Generic;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IAggregate : IWyrmItemVisitor
    {
        long Version { get; set; }
        string Id { get; }
        Type InitialType { get; }
        void ApplyChange(object @event);
        IEnumerable<object> TakeUncommittedEvents();
        IEnumerable<Type> ApplyTypes();
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
