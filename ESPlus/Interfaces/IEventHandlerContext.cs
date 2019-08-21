namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        Position Checkpoint { get; set; }
        long Offset { get; set; }
        long TotalOffset { get; set; }
        Metadata Metadata { get; set; }
        void Flush();
    }
}
