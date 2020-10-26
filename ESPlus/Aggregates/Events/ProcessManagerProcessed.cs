namespace ESPlus.Aggregates
{
    public class ProcessManagerProcessed
    {
        public string CorrolationId { get; }
        public string EventId { get; }

        public ProcessManagerProcessed(string corrolationId, string eventId)
        {
            CorrolationId = corrolationId;
            EventId = eventId;
        }
    }
}