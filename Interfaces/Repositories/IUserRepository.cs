using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByEmailWithRoleAsync(string email);
        Task<User?> GetByIdWithRoleAsync(int userId);
    }
}