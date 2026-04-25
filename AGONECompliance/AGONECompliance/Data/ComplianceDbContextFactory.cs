using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AGONECompliance.Data;

public sealed class ComplianceDbContextFactory : IDesignTimeDbContextFactory<ComplianceDbContext>
{
    public ComplianceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ComplianceDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
            ?? Environment.GetEnvironmentVariable("AGONE_SQLSERVER_CONNECTION")
            ?? "Server=localhost,1433;Database=AGONEComplianceDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.EnableRetryOnFailure(5);
            sql.CommandTimeout(60);
        });

        return new ComplianceDbContext(optionsBuilder.Options);
    }
}
