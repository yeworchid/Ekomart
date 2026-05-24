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
            new Category { Name = "Pantry", Slug = "pantry", Description = "Groceries with longer shelf life.", DisplayOrder = 50 },
            new Category { Name = "Beverages", Slug = "beverages", Description = "Juices, soda and everyday drinks.", DisplayOrder = 60 },
            new Category { Name = "Snacks", Slug = "snacks", Description = "Crackers, nuts, chips and quick snacks.", DisplayOrder = 70 },
            new Category { Name = "Baby Food", Slug = "baby-food", Description = "Formula, cereal and baby food packs.", DisplayOrder = 80 }
        };

        foreach (var category in categories)
        {
            if (existingCategories.TryGetValue(category.Slug, out var existingCategory))
            {
                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.DisplayOrder = category.DisplayOrder;
                existingCategory.IsActive = true;
                existingCategory.UpdatedAtUtc = DateTime.UtcNow;
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
        var products = new[]
        {
            Product("beverages", "Apple Juice Bottle", "apple-juice-bottle", "Family-size apple juice for breakfast and snacks.", 3.49m, 70, "grocery/04.jpg", "royal-gala-apples"),
            Product("fresh-fruits", "Farm Produce Basket", "farm-produce-basket", "Mixed fresh produce for everyday cooking.", 8.90m, 32, "category/01.png", "organic-bananas"),
            Product("fresh-fruits", "Guava Pack", "guava-pack", "Ripe guava packed for fresh desserts and smoothies.", 4.80m, 45, "products/product-filt2.jpg", "seedless-grapes"),
            Product("fresh-fruits", "Fresh Oranges", "fresh-oranges", "Bright oranges with balanced sweetness.", 3.90m, 60, "grocery/05.jpg", "navel-oranges"),
            Product("fresh-fruits", "Strawberry Pack", "strawberry-pack", "Fresh strawberries in a family pack.", 5.50m, 35, "category/02.png"),
            Product("vegetables", "Market Vegetable Basket", "market-vegetable-basket", "Fresh vegetables selected for soups and salads.", 6.20m, 44, "shop/06.jpg", "cherry-tomatoes"),
            Product("vegetables", "Cauliflower Head", "cauliflower-head", "Fresh cauliflower for roasting and side dishes.", 2.75m, 40, "grocery/10.jpg", "broccoli-crown"),
            Product("vegetables", "Vegetable Medley Box", "vegetable-medley-box", "Colorful vegetables for daily meals.", 4.30m, 52, "category/05.jpg", "carrot-bag"),
            Product("pantry", "Chicken Broccoli Meal", "chicken-broccoli-meal", "Ready chicken and broccoli meal for a quick dinner.", 5.90m, 28, "grocery/23.jpg", "baby-spinach"),
            Product("vegetables", "Mixed Vegetable Basket", "mixed-vegetable-basket", "Basket of seasonal vegetables for the week.", 7.40m, 38, "category/01.png", "red-bell-peppers"),
            Product("dairy", "Chocolate Protein Milk", "chocolate-protein-milk", "Chocolate protein drink in a shelf-stable carton.", 2.95m, 75, "grocery/11.jpg", "whole-milk"),
            Product("dairy", "Sour Cream Tub", "sour-cream-tub", "Classic sour cream for baked potatoes and sauces.", 2.80m, 54, "best-seller/03.png", "greek-yogurt"),
            Product("dairy", "Shredded Mozzarella", "shredded-mozzarella", "Shredded mozzarella cheese for pizza and pasta.", 4.40m, 38, "grocery/14.jpg", "cheddar-cheese"),
            Product("dairy", "Almond Drink Pack", "almond-drink-pack", "Almond drink for cereal, coffee and smoothies.", 3.70m, 50, "discount-product/01.jpg", "salted-butter"),
            Product("baby-food", "Aptamil Gold Formula", "aptamil-gold-formula", "Infant formula powder in a sealed tin.", 18.90m, 24, "grocery/03.jpg", "cottage-cheese"),
            Product("bakery", "Original Crackers", "original-crackers", "Crisp original crackers for soups and cheese boards.", 3.20m, 46, "grocery/16.jpg", "sourdough-bread"),
            Product("bakery", "Pumpkin Spice Muffin Mix", "pumpkin-spice-muffin-mix", "Pumpkin spice muffin mix for home baking.", 4.60m, 33, "grocery/02.jpg", "croissant-pack"),
            Product("bakery", "Pecan Granola Cereal", "pecan-granola-cereal", "Crunchy pecan cereal for breakfast bowls.", 4.95m, 28, "category/03.jpg", "wholegrain-toast"),
            Product("bakery", "Chocolate Biscuit Pack", "chocolate-biscuit-pack", "Chocolate sandwich biscuits for tea and coffee.", 4.20m, 36, "grocery/20.jpg", "cinnamon-rolls"),
            Product("bakery", "Pancake Mix", "pancake-mix", "Original pancake mix for quick breakfasts.", 3.80m, 35, "grocery/15.jpg", "bagel-set"),
            Product("pantry", "Organic Rice Quinoa", "organic-rice-quinoa", "Organic rice and quinoa blend for side dishes.", 6.40m, 64, "category/07.jpg", "basmati-rice"),
            Product("pantry", "Cooking Oil Bottle", "cooking-oil-bottle", "Everyday cooking oil for frying and baking.", 8.90m, 40, "category/01.jpg", "olive-oil"),
            Product("pantry", "Pasta Pouch Set", "pasta-pouch-set", "Pasta and sauce pouches for weeknight meals.", 3.60m, 80, "grocery/22.jpg", "pasta-fusilli"),
            Product("pantry", "Pasta Sauce Pouches", "pasta-sauce-pouches", "Sauce pouches for pasta, rice and vegetables.", 2.30m, 75, "grocery/06.jpg", "tomato-passata"),
            Product("snacks", "Hunter Trail Mix", "hunter-trail-mix", "Crunchy trail mix with nuts and dried fruit.", 4.70m, 52, "grocery/08.jpg", "granola-mix"),
            Product("pantry", "Quaker Oats", "quaker-oats", "Rolled oats for porridge, baking and granola.", 3.95m, 68, "grocery/24.jpg"),
            Product("pantry", "Chocos Cereal", "chocos-cereal", "Chocolate breakfast cereal for milk bowls.", 3.40m, 58, "grocery/07.jpg"),
            Product("baby-food", "Cerelac Wheat Cereal", "cerelac-wheat-cereal", "Baby wheat cereal in an easy storage tin.", 5.20m, 30, "grocery/01.jpg"),
            Product("baby-food", "PediaSure Vanilla", "pediasure-vanilla", "Vanilla nutrition drink powder for children.", 12.90m, 22, "shop/04.jpg"),
            Product("baby-food", "Bobbie Infant Formula", "bobbie-infant-formula", "Infant formula powder for daily feeding.", 16.50m, 26, "best-seller/05.png"),
            Product("baby-food", "Mellin Baby Cereal", "mellin-baby-cereal", "Baby cereal with rice flour and milk.", 6.10m, 34, "best-seller/02.png"),
            Product("baby-food", "Feeding Pouches Variety", "feeding-pouches-variety", "Assorted baby food pouches for quick meals.", 7.80m, 42, "grocery/27.jpg"),
            Product("pantry", "Tyson Chicken Nuggets", "tyson-chicken-nuggets", "Breaded chicken nuggets for quick family dinners.", 7.20m, 39, "grocery/17.jpg"),
            Product("pantry", "Turkey Breast Slices", "turkey-breast-slices", "Smoked turkey breast slices for sandwiches.", 5.75m, 31, "grocery/28.jpg"),
            Product("snacks", "Cheez-It Crackers", "cheez-it-crackers", "White cheddar baked snack crackers.", 4.10m, 55, "grocery/26.jpg"),
            Product("snacks", "Spicy Corn Curls", "spicy-corn-curls", "Spicy crunchy corn curls in a party bag.", 3.30m, 63, "grocery/21.jpg"),
            Product("beverages", "Soda Cup Variety", "soda-cup-variety", "Assorted soda cups for parties and picnics.", 6.25m, 48, "grocery/25.jpg"),
            Product("beverages", "Fruit Drink Pack", "fruit-drink-pack", "Fruit drink pack with assorted flavors.", 4.95m, 46, "grocery/19.jpg"),
            Product("beverages", "Sparkling Cans", "sparkling-cans", "Colorful sparkling drinks in ready-to-chill cans.", 3.85m, 57, "category/04.jpg"),
            Product("snacks", "McCain Potato Smiles", "mccain-potato-smiles", "Crispy potato smiles for quick sides.", 5.10m, 37, "category/10.png"),
            Product("snacks", "Peanut Butter Sandwiches", "peanut-butter-sandwiches", "Frozen peanut butter sandwiches for lunch boxes.", 4.90m, 42, "best-seller/06.png")
        };

        var existingProducts = await _dbContext.Products
            .ToDictionaryAsync(product => product.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var seedProduct in products)
        {
            var product = FindExistingProduct(seedProduct, existingProducts);
            var originalSlug = product?.Slug;

            if (product is null)
            {
                product = new Product();
                _dbContext.Products.Add(product);
            }

            ApplySeedProduct(product, seedProduct, categories[seedProduct.CategorySlug].Id);

            if (!string.IsNullOrWhiteSpace(originalSlug) &&
                !string.Equals(originalSlug, product.Slug, StringComparison.OrdinalIgnoreCase))
            {
                existingProducts.Remove(originalSlug);
            }

            existingProducts[product.Slug] = product;
        }

        foreach (var product in existingProducts.Values)
        {
            var normalizedImageUrl = NormalizeSeedImageUrl(product.ImageUrl);
            if (normalizedImageUrl is not null &&
                !string.Equals(normalizedImageUrl, product.ImageUrl, StringComparison.Ordinal))
            {
                product.ImageUrl = normalizedImageUrl;
                product.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SeedProduct Product(
        string categorySlug,
        string name,
        string slug,
        string description,
        decimal price,
        int stockQuantity,
        string imagePath,
        params string[] legacySlugs)
    {
        return new SeedProduct(
            categorySlug,
            name,
            slug,
            description,
            price,
            stockQuantity,
            $"/assets/ekomart/images/{imagePath}",
            legacySlugs);
    }

    private static string? NormalizeSeedImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var value = imageUrl.Trim();
        if (value.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
        {
            value = "/" + value;
        }

        if (value.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
        {
            return "/assets/ekomart/images/" + value["/images/".Length..];
        }

        if (value.StartsWith("/assets/ecomart/", StringComparison.OrdinalIgnoreCase))
        {
            return "/assets/ekomart/" + value["/assets/ecomart/".Length..];
        }

        return value;
    }

    private static Product? FindExistingProduct(
        SeedProduct seedProduct,
        IReadOnlyDictionary<string, Product> existingProducts)
    {
        if (existingProducts.TryGetValue(seedProduct.Slug, out var product))
        {
            return product;
        }

        foreach (var legacySlug in seedProduct.LegacySlugs)
        {
            if (existingProducts.TryGetValue(legacySlug, out product))
            {
                return product;
            }
        }

        return null;
    }

    private static void ApplySeedProduct(
        Product product,
        SeedProduct seedProduct,
        int categoryId)
    {
        var isExistingProduct = product.Id != 0;

        product.CategoryId = categoryId;
        product.Name = seedProduct.Name;
        product.Slug = seedProduct.Slug;
        product.Description = seedProduct.Description;
        product.Price = seedProduct.Price;
        product.ImageUrl = seedProduct.ImageUrl;
        product.StockQuantity = seedProduct.StockQuantity;
        product.IsActive = true;

        if (isExistingProduct)
        {
            product.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private sealed record SeedProduct(
        string CategorySlug,
        string Name,
        string Slug,
        string Description,
        decimal Price,
        int StockQuantity,
        string ImageUrl,
        IReadOnlyCollection<string> LegacySlugs);

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
