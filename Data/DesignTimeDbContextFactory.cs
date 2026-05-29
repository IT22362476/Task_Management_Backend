using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Task_Manager_Backend.Data;

/// <summary>
/// Design-time factory for EF Core migrations (dotnet ef database update).
/// This bypasses the full application startup (Program.cs) so migrations
/// can run without JWT key, Google Auth, or other services being configured.
///
/// Connection string resolution order:
///   1. --connection argument (passed by dotnet ef database update --connection "...")
///   2. ConnectionStrings__DefaultConnection environment variable
///   3. ConnectionStrings:DefaultConnection environment variable
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // 1. Check --connection argument (passed by dotnet ef database update)
        var connectionString = GetConnectionStringFromArgs(args);

        // 2. Fall back to environment variables
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                            ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");
        }

        // 3. Fall back to local development default
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=taskmanagerdb;Username=taskuser;Password=taskpass";
            Console.WriteLine("WARNING: No connection string provided. Using local development default.");
        }

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string? GetConnectionStringFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--connection" && !string.IsNullOrEmpty(args[i + 1]))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
