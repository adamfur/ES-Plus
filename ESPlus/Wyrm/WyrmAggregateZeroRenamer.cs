namespace ESPlus.Wyrm
{
    public class WyrmAggregateZeroRenamer : IWyrmAggregateRenamer
    {
        public string Name(string name)
        {
            return name;
        }
    }
}