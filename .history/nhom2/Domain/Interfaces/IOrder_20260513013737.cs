using nhom2.Domain.Entities;

namespace DemoProject.Domain.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderById(int id);
        Task<List<Order>> GetAllOrdersByUserId(int userId);
        Task<List<Order>> GetAllOrders();
        Task AddOrder(Order order);
        Task UpdateOrder(Order order);
        Task DeleteOrder(int orderId);
    }
}