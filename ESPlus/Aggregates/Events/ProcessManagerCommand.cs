namespace ESPlus.Aggregates
{
    public class ProcessManagerCommand
    {
        public string CorrolationId { get; }
        public string Type { get; }
        public object Payload { get; }

        public ProcessManagerCommand(string corrolationId, string type, object payload)
        {
            CorrolationId = corrolationId;
            Type = type;
            Payload = payload;
        }
    }
}