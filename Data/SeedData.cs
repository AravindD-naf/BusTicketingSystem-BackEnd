namespace BusTicketingSystem.Data
{
    /// <summary>
    /// Seed data initialization for the database.
    /// Can be used to populate initial data during development or testing.
    /// </summary>
    public class SeedData
    {
        /// <summary>
        /// Initializes seed data in the database context.
        /// </summary>
        /// <param name="context">The database context</param>
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // TODO: Add seed data initialization logic here
            // Example: Create default users, operators, sources, destinations, etc.
            await Task.CompletedTask;
        }
    }
}

