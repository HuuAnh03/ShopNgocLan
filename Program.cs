using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

using ShopNgocLan.Models;
using ShopNgocLan.Models.Momo;
using ShopNgocLan.Repository;
using ShopNgocLan.Services;
using System;
using ShopNgocLan.Hubs;

namespace ShopNgocLan
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<DBShopNLContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddScoped<IDanhMucSpRepository, DanhMucRepository>();
            builder.Services.AddScoped<IAddToCartRepository, AddToCartRepository>();

            builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
            builder.Services.AddScoped<IMomoService, MomoService>();

            builder.Services.AddRazorPages();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "ShopNgocLan.Session";
            });

            builder.Services.AddControllersWithViews();

            // SignalR
            builder.Services.AddSignalR();
            builder.Services.AddScoped<IChatBotService, DummyChatBotService>();
            builder.Services.AddServerSideBlazor();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // 👇👇👇 THÊM DÒNG NÀY (map hub cho SignalR)
            app.MapHub<ChatHub>("/chathub");

            // Route Admin
            app.MapControllerRoute(
                name: "AdminArea",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            // Route mặc định
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
