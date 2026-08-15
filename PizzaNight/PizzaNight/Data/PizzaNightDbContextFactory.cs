using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PizzaNight.Data;

public sealed class PizzaNightDbContextFactory : IDesignTimeDbContextFactory<PizzaNightDbContext>
{
    public PizzaNightDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PizzaNightDbContext>()
            .UseSqlite("Data Source=App_Data/pizza-knight.db")
            .Options;

        return new PizzaNightDbContext(options);
    }
}
