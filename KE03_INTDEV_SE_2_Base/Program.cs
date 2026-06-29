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
            builder.Services.AddScoped<CustomerRepository>();
            builder.Services.AddScoped<CategoryRespository>();
            builder.Services.AddScoped<ProductRepository>();
            builder.Services.AddScoped<OrderRepository>();
            builder.Services.AddScoped<LeverancierRepository>();
            builder.Services.AddScoped<SendOrderRepository>();

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
                pattern: "{controller=Dashboard}/{action=Index}/{id?}");

            app.Run();
        }
    }
}