using System.Globalization;
using Ekomart.Domain.Enums;

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

    private static readonly string[] CategoryIcons =
    {
        "/assets/ekomart/images/icons/01.svg",
        "/assets/ekomart/images/icons/02.svg",
        "/assets/ekomart/images/icons/03.svg",
        "/assets/ekomart/images/icons/04.svg",
        "/assets/ekomart/images/icons/05.svg",
        "/assets/ekomart/images/icons/06.svg",
        "/assets/ekomart/images/icons/07.svg",
        "/assets/ekomart/images/icons/08.svg",
        "/assets/ekomart/images/icons/09.svg",
        "/assets/ekomart/images/icons/10.svg"
    };

    public static string ProductImage(string? imageUrl, int stableKey)
    {
        var normalizedImageUrl = NormalizeProductImageUrl(imageUrl);
        if (normalizedImageUrl is not null)
        {
            return normalizedImageUrl;
        }

        var index = Math.Abs(stableKey) % ProductImages.Length;
        return ProductImages[index];
    }

    private static string? NormalizeProductImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var value = imageUrl.Trim();
        if (!value.StartsWith('/'))
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

        if (value.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return null;
    }

    public static string CategoryImage(int stableKey)
    {
        var index = Math.Abs(stableKey) % CategoryImages.Length;
        return CategoryImages[index];
    }

    public static string CategoryIcon(int stableKey)
    {
        var index = Math.Abs(stableKey) % CategoryIcons.Length;
        return CategoryIcons[index];
    }

    public static string Money(decimal amount)
    {
        return amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));
    }

    public static string DeliveryMethodName(DeliveryMethod method)
    {
        return method switch
        {
            DeliveryMethod.FreeShipping => "Free Shipping",
            DeliveryMethod.FlatRate => "Flat Rate",
            DeliveryMethod.LocalPickup => "Local Pickup",
            _ => method.ToString()
        };
    }

    public static string PaymentMethodName(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.DirectBankTransfer => "Direct Bank Transfer",
            PaymentMethod.CheckPayments => "Check Payments",
            PaymentMethod.CashOnDelivery => "Cash On Delivery",
            PaymentMethod.PayPal => "PayPal",
            _ => method.ToString()
        };
    }
}
