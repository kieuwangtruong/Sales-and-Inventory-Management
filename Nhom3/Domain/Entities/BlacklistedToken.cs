using System;

namespace Nhom3.Domain.Entities
{
    public class BlacklistedToken
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Jti { get; set; } = string.Empty;
        public string? TokenType { get; set; }
        public string? DeviceId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
