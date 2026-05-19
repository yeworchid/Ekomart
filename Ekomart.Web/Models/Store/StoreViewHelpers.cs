using System.Globalization;

namespace Ekomart.Web.Models.Store;

public static class StoreViewHelpers
{
    private static readonly string[] ProductImages =
    {
        "/assets/ekomart/images/grocery/01.jpg",
        "/assets/ekomart/images/grocery/02.jpg",
        "/assets/ekomart/images/grocery/03.jpg",
        "/assets/ekomart/images/grocery/04.jpg",
        "/assets/ekomart/images/grocery/05.jpg",
        "/assets/ekomart/images/grocery/06.jpg",
        "/assets/ekomart/images/grocery/07.jpg",
        "/assets/ekomart/images/grocery/08.jpg"
    };

    private static readonly string[] CategoryImages =
    {
        "/assets/ekomart/images/category/01.png",
        "/assets/ekomart/images/category/02.png",
        "/assets/ekomart/images/category/03.png",
        "/assets/ekomart/images/category/04.png",
        "/assets/ekomart/images/category/05.png",
        "/assets/ekomart/images/category/06.png",
        "/assets/ekomart/images/category/07.png",
        "/assets/ekomart/images/category/08.png",
        "/assets/ekomart/images/category/09.png",
        "/assets/ekomart/images/category/10.png"
    };

    public static string ProductImage(string? imageUrl, int stableKey)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            return imageUrl;
        }

        var index = Math.Abs(stableKey) % ProductImages.Length;
        return ProductImages[index];
    }

    public static string CategoryImage(int stableKey)
    {
        var index = Math.Abs(stableKey) % CategoryImages.Length;
        return CategoryImages[index];
    }

    public static string Money(decimal amount)
    {
        return amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));
    }
}
