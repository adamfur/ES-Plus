using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepositoryTransaction : IDisposable
    {
        IEnumerable<WyrmEvent> Events { get; }
        Task<WyrmResult> Commit();
        void Append(IEnumerable<WyrmEvent> events);
    }
}