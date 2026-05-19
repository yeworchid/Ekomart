using Ekomart.Infrastructure;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var webProjectPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "../../../../Ekomart.Web"));

var configuration = new ConfigurationBuilder()
    .SetBasePath(webProjectPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddInfrastructure(configuration);

await using var serviceProvider = services.BuildServiceProvider();
await using var scope = serviceProvider.CreateAsyncScope();

var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.MigrateAsync();

var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
await seeder.SeedAsync();
