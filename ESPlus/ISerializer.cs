namespace ESPlus
{
    public interface ISerializer
    {
        string Serialize<T>(T graph);
        T Deserialize<T>(string buffer);
    }
}