using EventStore.ClientAPI;

namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        Position Checkpoint { get; set; }
        void Flush();
    }
}
