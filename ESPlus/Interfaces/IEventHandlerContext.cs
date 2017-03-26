namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        long Checkpoint { get; set; }
        void Flush();
    }
}
