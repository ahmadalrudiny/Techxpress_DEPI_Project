using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetByIdWithDetailsAsync(int id);                  // للأدمن
    Task<Order?> GetByIdForUserAsync(int id, string userId);       // للعميل (يمنع رؤية طلب غيره)
    Task<IEnumerable<Order>> GetByUserIdAsync(string userId);      // سجل طلبات عميل
    Task<IEnumerable<Order>> GetAllWithUsersAsync();               // كل الطلبات للأدمن
}