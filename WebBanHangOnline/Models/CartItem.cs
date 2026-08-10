using WebBanHangOnline.Models;

public class CartItem
{
    public int Id { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; }

    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public decimal DisplayUnitPrice =>
        ProductVariant?.Price > 0
            ? ProductVariant.Price
            : ProductVariant?.Product?.FinalPrice ?? 0;

    public decimal DisplayLineTotal => DisplayUnitPrice * Quantity;
}
