using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vigie24.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core tooling (`dotnet ef migrations add/update`).
/// Uses a throwaway local connection string — the real one comes from the app
/// (the Web host cannot be built at design time: missing secrets, DI validation).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<Vigie24DbContext>
{
    public Vigie24DbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<Vigie24DbContext>()
            .UseNpgsql("Host=localhost;Database=vigie24_design;Username=postgres;Password=design")
            .Options;
        return new Vigie24DbContext(options);
    }
}
