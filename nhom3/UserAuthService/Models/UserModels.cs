namespace UserAuthService.Models;

// ─── User ─────────────────────────────────────────────────────────────────────
public class User
{
    public int    Id           { get; set; }
    public string Username     { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;  // BCrypt hash
    public string FullName     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public bool   IsActive     { get; set; } = true;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Reference ID — KHÔNG dùng FK sang service khác
    public int RoleId { get; set; }

    // Navigation
    public Role Role { get; set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

// ─── Role ─────────────────────────────────────────────────────────────────────
// Chỉ có 3 role cố định: Admin, Sales, Warehouse
public class Role
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty; // "Admin" | "Sales" | "Warehouse"
    public string Description { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}

// ─── RefreshToken ─────────────────────────────────────────────────────────────
// Lưu refresh token để gia hạn access token mà không cần đăng nhập lại
public class RefreshToken
{
    public int      Id        { get; set; }
    public string   Token     { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool     IsRevoked { get; set; } = false;
    public string?  ReplacedByToken { get; set; } // token mới thay thế nó

    public int  UserId { get; set; }
    public User User   { get; set; } = null!;
}
