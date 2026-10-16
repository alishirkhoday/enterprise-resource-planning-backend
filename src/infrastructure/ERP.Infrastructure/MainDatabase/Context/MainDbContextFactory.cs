using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Infrastructure.MainDatabase.Context
{
    public class MainDbContextFactory : IDesignTimeDbContextFactory<MainDbContext>
    {
        public MainDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MainDbContext>();
            optionsBuilder.UseSqlServer("Data Source=.\\MSSQLSERVER2022;Database=ERP_DB_Development;Trust Server Certificate=true;Trusted_Connection=True;Persist Security Info=False;");
            var dbContext = new MainDbContext(optionsBuilder.Options);
            return dbContext;
        }
    }
}
