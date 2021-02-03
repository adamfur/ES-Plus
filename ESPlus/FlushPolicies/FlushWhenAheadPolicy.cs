using System.Threading.Tasks;

namespace ESPlus.FlushPolicies
{
    public class FlushWhenAheadPolicy : FlushOnThresholdPolicy
    {
        public override async Task FlushWhenAheadAsync()
        {
            await FlushAsync();
        }
    }
}
