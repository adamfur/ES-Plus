using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public class RepositoryTransaction : IRepositoryTransaction
    {
        private readonly List<WyrmEvent> _events = new List<WyrmEvent>();
        private readonly IRepository _repository;
        private readonly Action _action;
        private bool _disposed = false;

        public RepositoryTransaction(IRepository repository, Action action)
        {
            _action = action;
            _repository = repository;
        }

        public IEnumerable<WyrmEvent> Events => _events;

        public async Task<WyrmResult> Commit()
        {
            return await _repository.Commit();
        }

        public void Append(IEnumerable<WyrmEvent> events)
        {
            _events.AddRange(events);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _action();
            _disposed = true;
        }
    }
}
