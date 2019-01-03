namespace ESPlus.Aggregates
{
    public class ProcessManagerPoisonPill
    {
        public string CorrolationId { get; }

        public ProcessManagerPoisonPill(string corrolationId)
        {
            CorrolationId = corrolationId;
        }
    }
}