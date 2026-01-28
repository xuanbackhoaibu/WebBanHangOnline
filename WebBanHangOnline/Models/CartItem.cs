using WebBanHangOnline.Models;

public class CartItem
{
    public int Id { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; }

    public int Quantity { get; set; }
}