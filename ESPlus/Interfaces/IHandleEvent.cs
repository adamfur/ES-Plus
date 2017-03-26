namespace ESPlus.EventHandlers
{
    public interface IHandleEvent<TEvent>
    {
        void Apply(TEvent @event);
    }
}
