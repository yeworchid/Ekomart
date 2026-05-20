using Ekomart.Infrastructure;
using Ekomart.Web.Authorization;
using Ekomart.Web.Hubs;
using Ekomart.Web.Middleware;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddSignalR();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AppPolicies.AdminOnly,
        policy => policy.RequireRole(AppRoles.Admin));

    options.AddPolicy(
        AppPolicies.ManagerOrAdmin,
        policy => policy.RequireRole(AppRoles.Manager, AppRoles.Admin));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Errors/StatusCode", "?code={0}");
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<TechnicalLogMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<OrderHub>("/hubs/orders");

app.Run();
