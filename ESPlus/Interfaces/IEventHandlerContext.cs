namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        Position Checkpoint { get; set; }
        long Offset { get; set; }
        long TotalOffset { get; set; }
        MetaData Metadata { get; set; }
        void Flush();
    }
}
