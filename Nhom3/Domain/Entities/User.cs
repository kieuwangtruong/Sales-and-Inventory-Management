using System;
using System.ComponentModel.DataAnnotations;

namespace Nhom3.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public enum UserRole
        {
            User = 0,
            Admin = 1
        }
        public UserRole Role { get; set; } = UserRole.User;
        public enum Gender
        {
            [Display(Name = "Nam")]
            Male,
            [Display(Name = "Nữ")]
            Female,
            [Display(Name = "Khác")]
            Other
        }
        public Gender Sex { get; set; }
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }
        public void DeleteUser(int userId)
        {
            throw new NotImplementedException();
        }

        // Constructor
        public User(string userName, string fullName, string email, string passwordHash,
        DateTime dateOfBirth, Gender sex, string address)
        {
            UserName = userName;
            FullName = fullName;
            Email = email;
            PasswordHash = passwordHash;
            DateOfBirth = dateOfBirth;
            Sex = sex;
            Address = address;
            Role = UserRole.User;
            CreatedAt = DateTime.Now;
            LastModified = null;
        }
        public User() { }
    }
}
