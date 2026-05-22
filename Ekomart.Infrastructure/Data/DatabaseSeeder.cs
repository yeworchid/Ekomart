using Ekomart.Domain.Entities;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ekomart.Infrastructure.Data;

public class DatabaseSeeder
{
    public const string AdminEmail = "admin@ekomart.test";
    public const string DemoPassword = "Ekomart123!";
    public const string ManagerEmail = "manager@ekomart.test";
    public const string UserEmail = "user@ekomart.test";

    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DatabaseSeeder(
        AppDbContext dbContext,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        var users = await SeedUsersAsync();
        var categories = await SeedCategoriesAsync(cancellationToken);
        await SeedProductsAsync(categories, cancellationToken);
        var plans = await SeedSubscriptionPlansAsync(cancellationToken);
        await SeedDemoSubscriptionAsync(users.User.Id, plans.Basic.Id, cancellationToken);
        await SeedDemoOrderAsync(users.User.Id, plans.Basic.DiscountPercent, cancellationToken);
        await SeedAuditLogsAsync(users, cancellationToken);

        _logger.LogInformation("Database seed completed.");
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in new[] { "User", "Manager", "Admin" })
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(role));
            EnsureSuccess(result);
        }
    }

    private async Task<(ApplicationUser User, ApplicationUser Manager, ApplicationUser Admin)> SeedUsersAsync()
    {
        var user = await EnsureUserAsync(UserEmail, "Demo User", "User");
        var manager = await EnsureUserAsync(ManagerEmail, "Demo Manager", "Manager");
        var admin = await EnsureUserAsync(AdminEmail, "Demo Admin", "Admin");

        return (user, manager, admin);
    }

    private async Task<ApplicationUser> EnsureUserAsync(string email, string fullName, string role)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var createResult = await _userManager.CreateAsync(user, DemoPassword);
            EnsureSuccess(createResult);
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            EnsureSuccess(roleResult);
        }

        return user;
    }

    private async Task<Dictionary<string, Category>> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existingCategories = await _dbContext.Categories
            .ToDictionaryAsync(category => category.Slug, cancellationToken);

        var categories = new[]
        {
            new Category { Name = "Fresh Fruits", Slug = "fresh-fruits", Description = "Seasonal fruits and everyday picks.", DisplayOrder = 10 },
            new Category { Name = "Vegetables", Slug = "vegetables", Description = "Fresh vegetables for daily cooking.", DisplayOrder = 20 },
            new Category { Name = "Dairy", Slug = "dairy", Description = "Milk, cheese, yogurt and other dairy products.", DisplayOrder = 30 },
            new Category { Name = "Bakery", Slug = "bakery", Description = "Bread, buns and pastry.", DisplayOrder = 40 },
            new Category { Name = "Pantry", Slug = "pantry", Description = "Groceries with longer shelf life.", DisplayOrder = 50 }
        };

        foreach (var category in categories)
        {
            if (existingCategories.ContainsKey(category.Slug))
            {
                continue;
            }

            _dbContext.Categories.Add(category);
            existingCategories[category.Slug] = category;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext.Categories
            .ToDictionaryAsync(category => category.Slug, cancellationToken);
    }

    private async Task SeedProductsAsync(
        IReadOnlyDictionary<string, Category> categories,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new[]
        {
            Product("fresh-fruits", "Royal Gala Apples", "royal-gala-apples", "Crisp sweet apples for snacks and baking.", 3.49m, 70),
            Product("fresh-fruits", "Organic Bananas", "organic-bananas", "Ripe organic bananas sold by bunch.", 2.20m, 90),
            Product("fresh-fruits", "Seedless Grapes", "seedless-grapes", "Juicy green grapes without seeds.", 4.80m, 45),
            Product("fresh-fruits", "Navel Oranges", "navel-oranges", "Bright oranges with balanced sweetness.", 3.90m, 60),
            Product("fresh-fruits", "Strawberry Pack", "strawberry-pack", "Fresh strawberries in a family pack.", 5.50m, 35),
            Product("vegetables", "Cherry Tomatoes", "cherry-tomatoes", "Small tomatoes for salads and snacks.", 3.10m, 55),
            Product("vegetables", "Broccoli Crown", "broccoli-crown", "Fresh broccoli crowns.", 2.75m, 40),
            Product("vegetables", "Carrot Bag", "carrot-bag", "Sweet carrots in a one kilogram bag.", 1.90m, 80),
            Product("vegetables", "Baby Spinach", "baby-spinach", "Washed spinach leaves ready for salads.", 3.60m, 35),
            Product("vegetables", "Red Bell Peppers", "red-bell-peppers", "Crunchy red peppers.", 4.25m, 42),
            Product("dairy", "Whole Milk", "whole-milk", "Fresh whole milk, one liter.", 1.80m, 120),
            Product("dairy", "Greek Yogurt", "greek-yogurt", "Thick plain Greek yogurt.", 2.95m, 75),
            Product("dairy", "Cheddar Cheese", "cheddar-cheese", "Mature cheddar cheese block.", 4.40m, 38),
            Product("dairy", "Salted Butter", "salted-butter", "Creamy salted butter.", 3.70m, 50),
            Product("dairy", "Cottage Cheese", "cottage-cheese", "Fresh cottage cheese.", 2.65m, 48),
            Product("bakery", "Sourdough Bread", "sourdough-bread", "Rustic sourdough loaf.", 4.10m, 30),
            Product("bakery", "Croissant Pack", "croissant-pack", "Flaky butter croissants.", 5.90m, 26),
            Product("bakery", "Wholegrain Toast", "wholegrain-toast", "Sliced wholegrain toast bread.", 2.85m, 45),
            Product("bakery", "Cinnamon Rolls", "cinnamon-rolls", "Soft cinnamon rolls with glaze.", 4.95m, 28),
            Product("bakery", "Bagel Set", "bagel-set", "Classic bagels for breakfast.", 3.80m, 35),
            Product("pantry", "Basmati Rice", "basmati-rice", "Aromatic basmati rice.", 6.40m, 64),
            Product("pantry", "Olive Oil", "olive-oil", "Extra virgin olive oil.", 8.90m, 40),
            Product("pantry", "Pasta Fusilli", "pasta-fusilli", "Durum wheat fusilli pasta.", 1.95m, 110),
            Product("pantry", "Tomato Passata", "tomato-passata", "Smooth tomato passata.", 2.30m, 75),
            Product("pantry", "Granola Mix", "granola-mix", "Crunchy granola with nuts.", 4.70m, 52)
        };

        foreach (var product in products)
        {
            product.CategoryId = categories[product.Category!.Slug].Id;
            product.Category = null;
            _dbContext.Products.Add(product);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Product Product(
        string categorySlug,
        string name,
        string slug,
        string description,
        decimal price,
        int stockQuantity)
    {
        return new Product
        {
            Category = new Category { Slug = categorySlug },
            Name = name,
            Slug = slug,
            Description = description,
            Price = price,
            ImageUrl = $"/images/products/{slug}.jpg",
            StockQuantity = stockQuantity,
            IsActive = true
        };
    }

    private async Task<(SubscriptionPlan Basic, SubscriptionPlan Plus)> SeedSubscriptionPlansAsync(
        CancellationToken cancellationToken)
    {
        var basic = await EnsurePlanAsync(
            "Basic",
            "basic",
            "Basic grocery discount for regular customers.",
            4.99m,
            30,
            3m,
            new[]
            {
                ("ORDER_DISCOUNT_3", "3% order discount", "Applies a 3% discount during checkout."),
                ("PROFILE_BADGE", "Profile badge", "Shows active Basic subscription in profile.")
            },
            cancellationToken);

        var plus = await EnsurePlanAsync(
            "Plus",
            "plus",
            "Higher discount for frequent grocery orders.",
            9.99m,
            30,
            7m,
            new[]
            {
                ("ORDER_DISCOUNT_7", "7% order discount", "Applies a 7% discount during checkout."),
                ("PRIORITY_SUPPORT", "Priority support", "Marks support requests as priority."),
                ("PROFILE_BADGE", "Profile badge", "Shows active Plus subscription in profile.")
            },
            cancellationToken);

        return (basic, plus);
    }

    private async Task<SubscriptionPlan> EnsurePlanAsync(
        string name,
        string slug,
        string description,
        decimal price,
        int durationDays,
        decimal discountPercent,
        IEnumerable<(string Code, string Name, string Description)> features,
        CancellationToken cancellationToken)
    {
        var plan = await _dbContext.SubscriptionPlans
            .Include(item => item.Features)
            .FirstOrDefaultAsync(item => item.Slug == slug, cancellationToken);

        if (plan is null)
        {
            plan = new SubscriptionPlan
            {
                Name = name,
                Slug = slug,
                Description = description,
                Price = price,
                DurationDays = durationDays,
                DiscountPercent = discountPercent,
                IsActive = true
            };

            _dbContext.SubscriptionPlans.Add(plan);
        }
        else
        {
            plan.Name = name;
            plan.Description = description;
            plan.Price = price;
            plan.DurationDays = durationDays;
            plan.DiscountPercent = discountPercent;
            plan.IsActive = true;
            plan.UpdatedAtUtc = DateTime.UtcNow;
        }

        foreach (var feature in features)
        {
            if (plan.Features.Any(item => item.Code == feature.Code))
            {
                continue;
            }

            plan.Features.Add(new SubscriptionFeature
            {
                Code = feature.Code,
                Name = feature.Name,
                Description = feature.Description
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return plan;
    }

    private async Task SeedDemoSubscriptionAsync(
        string userId,
        int subscriptionPlanId,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.UserSubscriptions.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return;
        }

        _dbContext.UserSubscriptions.Add(new UserSubscription
        {
            UserId = userId,
            SubscriptionPlanId = subscriptionPlanId,
            StartsAtUtc = DateTime.UtcNow.AddDays(-3),
            EndsAtUtc = DateTime.UtcNow.AddDays(27),
            Status = SubscriptionStatus.Active
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoOrderAsync(
        string userId,
        decimal discountPercent,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.Orders.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return;
        }

        var products = await _dbContext.Products
            .OrderBy(item => item.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (products.Count < 2)
        {
            return;
        }

        var orderItems = products
            .Select(product => new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 2,
                LineTotal = product.Price * 2
            })
            .ToList();

        var itemsTotal = orderItems.Sum(item => item.LineTotal);
        var discountAmount = Math.Round(itemsTotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        _dbContext.Orders.Add(new Order
        {
            UserId = userId,
            Status = OrderStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            ItemsTotal = itemsTotal,
            SubscriptionDiscountPercent = discountPercent,
            SubscriptionDiscountAmount = discountAmount,
            TotalAmount = itemsTotal - discountAmount,
            CustomerName = "Demo User",
            CustomerPhone = "+10000000000",
            CustomerEmail = UserEmail,
            DeliveryAddress = "Demo Street, 1",
            DeliveryMethod = DeliveryMethod.FreeShipping,
            PaymentMethod = PaymentMethod.DirectBankTransfer,
            TermsAccepted = true,
            Items = orderItems
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAuditLogsAsync(
        (ApplicationUser User, ApplicationUser Manager, ApplicationUser Admin) users,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.AuditLogs.AnyAsync(cancellationToken))
        {
            return;
        }

        _dbContext.AuditLogs.AddRange(
            new AuditLog
            {
                UserId = users.User.Id,
                Action = AuditAction.Register,
                EntityName = nameof(ApplicationUser),
                EntityId = users.User.Id,
                Details = "Demo user account created by seed."
            },
            new AuditLog
            {
                UserId = users.Manager.Id,
                Action = AuditAction.ProductUpdated,
                EntityName = nameof(Product),
                Details = "Demo manager action for dashboard."
            },
            new AuditLog
            {
                UserId = users.Admin.Id,
                Action = AuditAction.UserRoleChanged,
                EntityName = nameof(ApplicationUser),
                EntityId = users.Manager.Id,
                Details = "Demo admin role audit entry."
            });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException(errors);
    }
}
