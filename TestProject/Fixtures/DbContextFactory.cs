using BusTicketingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Tests.Fixtures;

public static class DbContextFactory
{
    public static ApplicationDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
