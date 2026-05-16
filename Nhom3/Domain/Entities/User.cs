using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        // Constructor and Validation
        public User(string userName, string fullName, string email, string passwordHash,
        DateTime dateOfBirth, Gender sex, string address, bool skipValidation = false)
        {
            UserName = userName;
            FullName = fullName;
            Email = email;
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(passwordHash);
            PasswordHash = hashedPassword;
            DateOfBirth = dateOfBirth;
            Sex = sex;
            Address = address;
            CreatedAt = DateTime.Now;
            LastModified = null;
        }
        public User() { }
    }
}
