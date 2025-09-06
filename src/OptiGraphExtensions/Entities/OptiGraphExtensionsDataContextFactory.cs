using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace OptiGraphExtensions.Entities
{
    public class OptiGraphExtensionsDataContextFactory : IDesignTimeDbContextFactory<OptiGraphExtensionsDataContext>
    {
        public OptiGraphExtensionsDataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OptiGraphExtensionsDataContext>();
            
            // Use a temporary connection string for design time
            optionsBuilder.UseSqlServer("Server=.;Database=OptiGraphExtensions_DesignTime;Integrated Security=true;TrustServerCertificate=true;");
            
            // Create a simple logger
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<OptiGraphExtensionsDataContext>();
            
            return new OptiGraphExtensionsDataContext(optionsBuilder.Options, logger);
        }
    }
}