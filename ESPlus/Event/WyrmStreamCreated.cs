namespace Wyrm
{
    public abstract class StreamCreated
    {
    }
    
    public class StreamCreated<T> : StreamCreated
    {
        public string StreamName { get; }

        public StreamCreated(string streamName)
        {
            StreamName = streamName;
        }
    }    
}