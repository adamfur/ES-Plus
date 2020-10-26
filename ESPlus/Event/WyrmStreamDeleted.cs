namespace Wyrm
{
    public abstract class StreamDeleted
    {
    }
    
    public class StreamDeleted<T> : StreamDeleted
    {
        public string StreamName { get; }

        public StreamDeleted(string streamName)
        {
            StreamName = streamName;
        }
    }    
}