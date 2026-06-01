using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Models;

public class appDbContext(DbContextOptions<appDbContext> options) : DbContext(options)
{
    public DbSet<DataAccessLayer.Models.Product> Product { get; set; } = default!;

public DbSet<DataAccessLayer.Models.Account> Account { get; set; } = default!;

public DbSet<DataAccessLayer.Models.Address> Address { get; set; } = default!;
}
