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
}