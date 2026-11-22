using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitGym.Infrastructure.Persistence;

public sealed class FitGymDbContextFactory : IDesignTimeDbContextFactory<FitGymDbContext>
{
    public FitGymDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FITGYM_CONNECTION_STRING")
            ?? "Server=.\\SQLEXPRESS;Database=FitGymDev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<FitGymDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new FitGymDbContext(options);
    }
}
