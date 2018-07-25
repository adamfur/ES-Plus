namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        ESPlus.Position Checkpoint { get; set; }
        void Flush();
    }
}
