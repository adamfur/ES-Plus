namespace ESPlus
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T graph);
        T Deserialize<T>(byte[] buffer);
    }
}