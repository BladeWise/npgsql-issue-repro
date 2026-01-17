namespace EFCore.PostgreSQL.Tests;

#region Namespaces
using Microsoft.EntityFrameworkCore;
#endregion

public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}