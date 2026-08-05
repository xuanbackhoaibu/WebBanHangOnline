using WebBanHangOnline.Models;
using WebBanHangOnline.Services;

namespace WebBanHangOnline.Tests;

public class InventoryServiceTests
{
    [Theory]
    [InlineData(5, 1)]
    [InlineData(5, 5)]
    public void CanReserve_ReturnsTrue_WhenStockIsEnough(int stock, int quantity)
    {
        var variant = new ProductVariant { Stock = stock };

        Assert.True(InventoryService.CanReserve(variant, quantity));
    }

    [Theory]
    [InlineData(5, 6)]
    [InlineData(5, 0)]
    [InlineData(5, -1)]
    public void CanReserve_ReturnsFalse_WhenQuantityIsInvalidOrExceedsStock(int stock, int quantity)
    {
        var variant = new ProductVariant { Stock = stock };

        Assert.False(InventoryService.CanReserve(variant, quantity));
    }

    [Fact]
    public void Reserve_DecreasesStock_WhenStockIsEnough()
    {
        var variant = new ProductVariant { Stock = 8 };

        InventoryService.Reserve(variant, 3);

        Assert.Equal(5, variant.Stock);
    }

    [Fact]
    public void Reserve_Throws_WhenStockIsNotEnough()
    {
        var variant = new ProductVariant { Stock = 2 };

        Assert.Throws<InvalidOperationException>(() => InventoryService.Reserve(variant, 3));
        Assert.Equal(2, variant.Stock);
    }

    [Fact]
    public void Release_IncreasesStock_WhenQuantityIsPositive()
    {
        var variant = new ProductVariant { Stock = 2 };

        InventoryService.Release(variant, 3);

        Assert.Equal(5, variant.Stock);
    }
}
