using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepositoryTransaction : IStore, IDisposable
    {
        Task<WyrmResult> CommitAsync(CommitPolicy policy = CommitPolicy.All);
    }
}