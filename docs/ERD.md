# ERD and Data Dictionary

## Main Entities

```text
ApplicationUser 1---n CartItem
ApplicationUser 1---n Order
Category        1---n Product
Product         1---n ProductImage
Product         1---n ProductVariant
Product         1---n Review
ProductVariant 1---n CartItem
ProductVariant 1---n OrderDetail
Order           1---n OrderDetail
```

## Data Dictionary

### Product

| Field | Meaning |
| --- | --- |
| ProductId | Primary key |
| Name | Product name |
| Slug | SEO URL slug |
| CategoryId | Category foreign key |
| Price | Base selling price |
| FlashSalePrice | Optional sale price |
| Thumbnail/ImageUrl | Main product image |
| IsActive | Product visibility flag |

### ProductVariant

| Field | Meaning |
| --- | --- |
| Id | Primary key |
| ProductId | Product foreign key |
| Size | Variant size |
| Color | Variant color |
| Stock | Available quantity |
| Price | Variant price |

### Order

| Field | Meaning |
| --- | --- |
| Id | Primary key |
| UserId | Customer foreign key |
| OrderDate | Date/time when order is created |
| ShippingAddress | Delivery address |
| PhoneNumber | Customer phone |
| TotalAmount | Total order amount |
| Status | Pending, Confirmed, Paid, Failed, Cancelled, Refunded |
| PaymentMethod | COD, VNPay, Momo, VietQR, Card |
| PaymentDate | Date/time when payment is confirmed |

### OrderDetail

| Field | Meaning |
| --- | --- |
| Id | Primary key |
| OrderId | Order foreign key |
| ProductVariantId | Variant foreign key |
| Quantity | Purchased quantity |
| Price | Price at purchase time |

