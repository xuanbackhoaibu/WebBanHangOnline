using WebBanHangOnline.Models;

namespace WebBanHangOnline.Services;

public static class DiscountCalculator
{
    public static VoucherCalculation CalculateCartTotalWithVoucher(
        decimal subtotal,
        DiscountCode? discountCode,
        DateTime now,
        decimal shippingFee = 0)
    {
        subtotal = Math.Max(0, subtotal);
        shippingFee = Math.Max(0, shippingFee);

        if (discountCode == null)
        {
            return VoucherCalculation.Valid(null, subtotal, 0, 0, subtotal + shippingFee);
        }

        var normalizedCode = discountCode.Code.Trim().ToUpperInvariant();
        var validationError = Validate(discountCode, subtotal, now);
        if (validationError != null)
        {
            return VoucherCalculation.Invalid(normalizedCode, validationError, subtotal + shippingFee);
        }

        var discountAmount = CalculateDiscountAmount(discountCode, subtotal);
        var shippingDiscountAmount = discountCode.DiscountType == DiscountTypes.FreeShipping
            ? shippingFee
            : 0;
        var finalTotal = Math.Max(0, subtotal - discountAmount) + Math.Max(0, shippingFee - shippingDiscountAmount);

        return VoucherCalculation.Valid(
            normalizedCode,
            subtotal,
            discountAmount,
            shippingDiscountAmount,
            finalTotal);
    }

    public static decimal CalculateDiscountAmount(DiscountCode discountCode, decimal subtotal)
    {
        subtotal = Math.Max(0, subtotal);

        var amount = discountCode.DiscountType switch
        {
            DiscountTypes.Percent => subtotal * discountCode.DiscountValue / 100,
            DiscountTypes.FixedAmount => discountCode.DiscountValue,
            DiscountTypes.FreeShipping => 0,
            _ => 0
        };

        if (discountCode.MaximumDiscountAmount.HasValue)
        {
            amount = Math.Min(amount, discountCode.MaximumDiscountAmount.Value);
        }

        return Math.Min(Math.Max(0, amount), subtotal);
    }

    private static string? Validate(DiscountCode discountCode, decimal subtotal, DateTime now)
    {
        if (!discountCode.IsActive)
        {
            return "Mã giảm giá đã bị tắt.";
        }

        if (discountCode.StartsAt.HasValue && discountCode.StartsAt.Value > now)
        {
            return "Mã giảm giá chưa đến thời gian sử dụng.";
        }

        if (discountCode.EndsAt.HasValue && discountCode.EndsAt.Value < now)
        {
            return "Mã giảm giá đã hết hạn.";
        }

        if (discountCode.UsageLimit.HasValue && discountCode.UsedCount >= discountCode.UsageLimit.Value)
        {
            return "Mã giảm giá đã hết lượt sử dụng.";
        }

        if (subtotal < discountCode.MinimumOrderAmount)
        {
            return $"Đơn hàng cần tối thiểu {discountCode.MinimumOrderAmount:N0} VND để dùng mã này.";
        }

        return null;
    }
}

public sealed record VoucherCalculation(
    bool IsValid,
    string? Code,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ShippingDiscountAmount,
    decimal FinalTotal,
    string? Message)
{
    public static VoucherCalculation Valid(
        string? code,
        decimal subtotal,
        decimal discountAmount,
        decimal shippingDiscountAmount,
        decimal finalTotal)
        => new(true, code, subtotal, discountAmount, shippingDiscountAmount, finalTotal, null);

    public static VoucherCalculation Invalid(string? code, string message, decimal finalTotal)
        => new(false, code, 0, 0, 0, finalTotal, message);
}
