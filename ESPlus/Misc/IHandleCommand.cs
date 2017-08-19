namespace ESPlus.Misc
{
    public interface IHandleCommand<T>
    {
        void Handle(T command);
    }
}