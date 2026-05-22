using Ekomart.Infrastructure;
using Ekomart.Web;
using Ekomart.Web.Authorization;
using Ekomart.Web.Hubs;
using Ekomart.Web.Middleware;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SharedResource));
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
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("ru")
};
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
localizationOptions.RequestCultureProviders =
[
    new CookieRequestCultureProvider(),
    new QueryStringRequestCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider()
];

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Errors/500");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Errors/StatusCode", "?code={0}");
app.UseRequestLocalization(localizationOptions);
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
