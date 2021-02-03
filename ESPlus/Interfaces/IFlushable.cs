using System.Threading.Tasks;

namespace ESPlus.Storage
{
    public interface IFlushable
    {
        Task FlushAsync();
    }
}