using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.FlushPolicies
{
    public class FlushOnThresholdOrAheadPolicy : FlushOnThresholdPolicy
    {
        public override async Task FlushWhenAheadAsync(CancellationToken cancellationToken)
        {
            await FlushAsync(cancellationToken);
        }
    }
}