using System.Threading.Tasks;
using Nhom3.Domain.Entities;

namespace Nhom3.Domain.Interfaces
{
    public interface ITokenBlacklist
    {
        Task<bool> IsBlacklistedAsync(string jti);
        Task AddAsync(BlacklistedToken token);
    }
}
