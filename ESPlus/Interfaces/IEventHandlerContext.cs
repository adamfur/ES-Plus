namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        string Checkpoint { get; set; }
        void Flush();
    }
}
