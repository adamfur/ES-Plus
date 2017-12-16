namespace ESPlus.Aggregates
{
    public class ProcessManagerCommand
    {
        public string CorrolationId { get; set; }
        public string Type { get; set; }
        public object Payload { get; set; }
    }    
}