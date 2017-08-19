namespace ESPlus.Misc
{
    public interface ICommandDispatcher
    {
         void Dispatch<T>(T command);
    }
}