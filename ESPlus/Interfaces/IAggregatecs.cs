namespace ESPlus.Interfaces
{
    public interface IAggregate
    {
        int Version { get; }
        string Id { get; }
    }
}
