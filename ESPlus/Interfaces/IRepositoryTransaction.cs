using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepositoryTransaction : IDisposable
    {
        IEnumerable<WyrmEvent> Events { get; }
        Task<Position> Commit();
        void Append(IEnumerable<WyrmEvent> events);
    }
}