using System.Threading.Tasks;
using ESPlus.EventHandlers;

namespace ESPlus.Subscribers
{
    // Event, Batch, IncompleteBatch, Time
    public interface IFlushPolicy
    {
        Task FlushWhenAheadAsync();
        Task FlushOnEventAsync();
        IEventHandler EventHandler { get; set; }
    }
}