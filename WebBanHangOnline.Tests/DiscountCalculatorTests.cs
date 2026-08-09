using WebBanHangOnline.Models;
using WebBanHangOnline.Services;

namespace WebBanHangOnline.Tests;

public class DiscountCalculatorTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 10, 0, 0);

    [Fact]
    public void CalculateCartTotalWithVoucher_AppliesPercentageDiscountWithMaximumCap()
    {
        var voucher = new DiscountCode
        {
            Code = "SALE50",
            DiscountType = DiscountTypes.Percent,
            DiscountValue = 50,
            MaximumDiscountAmount = 100_000,
            IsActive = true
        };

        var result = DiscountCalculator.CalculateCartTotalWithVoucher(500_000, voucher, Now);

        Assert.True(result.IsValid);
        Assert.Equal(100_000, result.DiscountAmount);
        Assert.Equal(400_000, result.FinalTotal);
    }

    [Fact]
    public void CalculateCartTotalWithVoucher_AppliesFixedAmountWithoutNegativeTotal()
    {
        var voucher = new DiscountCode
        {
            Code = "VIP500",
            DiscountType = DiscountTypes.FixedAmount,
            DiscountValue = 500_000,
            IsActive = true
        };

        var result = DiscountCalculator.CalculateCartTotalWithVoucher(300_000, voucher, Now);

        Assert.True(result.IsValid);
        Assert.Equal(300_000, result.DiscountAmount);
        Assert.Equal(0, result.FinalTotal);
    }

    [Fact]
    public void CalculateCartTotalWithVoucher_AppliesFreeShipping()
    {
        var voucher = new DiscountCode
        {
            Code = "FREESHIP",
            DiscountType = DiscountTypes.FreeShipping,
            IsActive = true
        };

        var result = DiscountCalculator.CalculateCartTotalWithVoucher(250_000, voucher, Now, shippingFee: 30_000);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.DiscountAmount);
        Assert.Equal(30_000, result.ShippingDiscountAmount);
        Assert.Equal(250_000, result.FinalTotal);
    }

    [Fact]
    public void CalculateCartTotalWithVoucher_RejectsVoucherWhenMinimumOrderIsNotMet()
    {
        var voucher = new DiscountCode
        {
            Code = "MIN500",
            DiscountType = DiscountTypes.FixedAmount,
            DiscountValue = 50_000,
            MinimumOrderAmount = 500_000,
            IsActive = true
        };

        var result = DiscountCalculator.CalculateCartTotalWithVoucher(300_000, voucher, Now);

        Assert.False(result.IsValid);
        Assert.Equal(300_000, result.FinalTotal);
        Assert.Contains("tối thiểu", result.Message);
    }

    [Fact]
    public void CalculateCartTotalWithVoucher_RejectsExhaustedVoucher()
    {
        var voucher = new DiscountCode
        {
            Code = "LIMITED",
            DiscountType = DiscountTypes.Percent,
            DiscountValue = 10,
            UsageLimit = 50,
            UsedCount = 50,
            IsActive = true
        };

        var result = DiscountCalculator.CalculateCartTotalWithVoucher(300_000, voucher, Now);

        Assert.False(result.IsValid);
        Assert.Equal("Mã giảm giá đã hết lượt sử dụng.", result.Message);
    }
}
