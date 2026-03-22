using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IRouteRepository : IRepository<Models.Route>
    {
        Task<Models.Route?> GetByIdAsync(int id);
        Task<Models.Route?> GetBySourceDestinationAsync(string source, string destination);
        Task<(IEnumerable<Models.Route> Routes, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
        void Update(Models.Route route);

        Task<(IEnumerable<Models.Route> Routes, int TotalCount)>
            GetBySourceAsync(string source, int pageNumber, int pageSize);

        Task<(IEnumerable<Models.Route> Routes, int TotalCount)>
            GetByDestinationAsync(string destination, int pageNumber, int pageSize);

        Task<(IEnumerable<Models.Route> Routes, int TotalCount)> SearchAsync(
            string? source, string? destination, int pageNumber, int pageSize);
    }
}