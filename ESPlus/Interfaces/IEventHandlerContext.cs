namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        byte[] Checkpoint { get; set; }
        void Flush();
    }
}
