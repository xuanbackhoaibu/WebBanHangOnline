using WebBanHangOnline.Models;

namespace WebBanHangOnline.Services;

public static class InventoryService
{
    public static bool CanReserve(ProductVariant variant, int quantity)
    {
        return quantity > 0 && variant.Stock >= quantity;
    }

    public static void Reserve(ProductVariant variant, int quantity)
    {
        if (!CanReserve(variant, quantity))
        {
            throw new InvalidOperationException("Insufficient stock for product variant.");
        }

        variant.Stock -= quantity;
    }

    public static void Release(ProductVariant variant, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        variant.Stock += quantity;
    }
}
