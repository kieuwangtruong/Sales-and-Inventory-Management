using nhom2.Domain;

namespace nhom2.Domain.Interfaces
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