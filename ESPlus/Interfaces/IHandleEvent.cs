using System.Threading.Tasks;

namespace ESPlus.EventHandlers
{
    public interface IHandleEvent<TEvent>
    {
        Task Apply(TEvent @event);
    }
}
