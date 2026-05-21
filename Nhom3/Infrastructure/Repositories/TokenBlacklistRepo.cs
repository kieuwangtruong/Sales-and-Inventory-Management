using Microsoft.EntityFrameworkCore;
using Nhom3.Domain.Entities;
using Nhom3.Domain.Interfaces;
using Nhom3.Infrastructure.Data;

namespace Nhom3.Infrastructure.Repositories
{
    public class TokenBlacklistRepo : ITokenBlacklist
    {
        private readonly ApplicationDbContext _context;

        public TokenBlacklistRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsBlacklistedAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return false;

            return await _context.BlacklistedTokens.AnyAsync(t => t.Jti == jti);
        }

        public async Task AddAsync(BlacklistedToken token)
        {
            if (string.IsNullOrWhiteSpace(token.Jti))
                return;

            var exists = await _context.BlacklistedTokens.AnyAsync(t => t.Jti == token.Jti);
            if (exists)
                return;

            _context.BlacklistedTokens.Add(token);
            await _context.SaveChangesAsync();
        }
    }
}
