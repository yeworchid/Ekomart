using Ekomart.Application.Interfaces;
using Ekomart.Infrastructure.Data;
using Ekomart.Infrastructure.Identity;
using Ekomart.Infrastructure.Options;
using Ekomart.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ekomart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default")));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<MongoOptions>(
            configuration.GetSection("Mongo"));

        var mongoConnectionString =
            configuration.GetConnectionString("Mongo");

        services.AddSingleton<IMongoClient>(
            new MongoClient(mongoConnectionString!));

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
