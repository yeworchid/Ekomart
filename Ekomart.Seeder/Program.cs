using Ekomart.Infrastructure;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var webProjectPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "../../../../Ekomart.Web"));

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(webProjectPath, "appsettings.json"), optional: true, reloadOnChange: false)
    .AddJsonFile(Path.Combine(webProjectPath, "appsettings.Development.json"), optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
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
