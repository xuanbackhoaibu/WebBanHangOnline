using WebBanHangOnline.Models;

namespace WebBanHangOnline.Services;

public interface IInventoryService
{
    bool CanReserve(ProductVariant variant, int quantity);
    void Reserve(ProductVariant variant, int quantity);
    void Release(ProductVariant variant, int quantity);
}

public sealed class InventoryService : IInventoryService
{
    public bool CanReserve(ProductVariant variant, int quantity)
    {
        return quantity > 0 && variant.Stock >= quantity;
    }

    public void Reserve(ProductVariant variant, int quantity)
    {
        if (!CanReserve(variant, quantity))
        {
            throw new InvalidOperationException("Insufficient stock for product variant.");
        }

        variant.Stock -= quantity;
    }

    public void Release(ProductVariant variant, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        variant.Stock += quantity;
    }
}
