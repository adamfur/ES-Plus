using System.Threading.Tasks;
using ESPlus.Interfaces;

namespace ESPlus.Wyrm
{
    public interface IBatchRepository : IRepository
    {
        Task<byte[]> Commit();
    }
}