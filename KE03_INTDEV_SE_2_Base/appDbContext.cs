using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Models;

public class appDbContext(DbContextOptions<appDbContext> options) : DbContext(options)
{
    public DbSet<DataAccessLayer.Models.Product> Product { get; set; } = default!;

    public DbSet<DataAccessLayer.Models.Account> Account { get; set; } = default!;

    public DbSet<DataAccessLayer.Models.DeliveryAddress> Address { get; set; } = default!;

    public DbSet<DataAccessLayer.Models.Order> Order { get; set; } = default!;
    public DbSet<KE03_INTDEV_SE_2_Base.Models.LeverancierModel> LeverancierModel { get; set; } = default!;
}
