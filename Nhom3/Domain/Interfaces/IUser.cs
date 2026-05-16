using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nhom3.Domain.Entities;

namespace Nhom3.Domain.Interfaces
{
    public interface IUser
    {
        Task<User?> GetUserById(int id);
        Task<User?> GetUserByEmail(string email);
        Task<User?> GetUserByUserName(string userName);
        Task<List<User>> GetAllUsers();
        Task AddUser(User user);
        Task DeleteUser(int id);
        Task UpdateUser(User user);
    }
}