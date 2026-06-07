using DataAccessLayer;
using DataAccessLayer.Repositories;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace KE03_INTDEV_SE_2_Base
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<appDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Connection")));

            builder.Services.AddScoped<DataAccessLayer.Repositories.CustomerRepository>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.CategoryRespository>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.ProductRepository>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.OrderRepository>();

            builder.Services.AddControllersWithViews();

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}