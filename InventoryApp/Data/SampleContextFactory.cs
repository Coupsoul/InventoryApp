using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace InventoryApp.Data
{
    internal class SampleContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        public ApplicationContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            var connectionString = config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationContext(optionsBuilder.Options);
        }
    }
}
