namespace Wyrm
{
    public class StreamDeleted
    {
        public string StreamName { get; }

        public StreamDeleted(string streamName)
        {
            StreamName = streamName;
        }
    }
    
    public class StreamDeleted<T>
    {
        public string StreamName { get; }

        public StreamDeleted(string streamName)
        {
            StreamName = streamName;
        }
    }    
}