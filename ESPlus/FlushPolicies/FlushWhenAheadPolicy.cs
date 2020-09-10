namespace ESPlus.FlushPolicies
{
    public class FlushWhenAheadPolicy : FlushOnThresholdPolicy
    {
        public override void FlushWhenAhead()
        {
            Flush();
        }
    }
}
