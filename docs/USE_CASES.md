# Use Case Pack

## UC-01 - Browse Product Catalog

**Actor:** Guest, Client  
**Precondition:** Product data exists  
**Main Flow:**

1. Actor opens product list.
2. Actor filters by category or keyword.
3. System returns active products with price, image, slug and stock.
4. Actor opens product detail.

**Alternative Flow:** If no product matches, system shows empty state.

## UC-02 - Checkout Order

**Actor:** Client  
**Precondition:** Client is logged in and cart is not empty  
**Main Flow:**

1. Client selects cart items.
2. Client enters address, phone and payment method.
3. System validates input and stock.
4. System creates order and order details.
5. System deducts stock and redirects to payment/order success.

**Exception:** If stock is insufficient, system rejects checkout.

## UC-03 - Manage Products

**Actor:** Admin  
**Precondition:** Admin is authenticated  
**Main Flow:**

1. Admin opens product management.
2. Admin creates/updates product information.
3. Admin manages images, variants and stock.
4. System saves changes to database.

## UC-04 - View Revenue Dashboard

**Actor:** Admin  
**Precondition:** Admin is authenticated  
**Main Flow:**

1. Admin opens dashboard.
2. System calculates order count, revenue, customers and products.
3. System renders 7-day revenue chart and order status distribution.
4. Admin checks recent orders and top products.

## UC-05 - Payment Webhook Confirmation

**Actor:** Payment Provider / Admin demo caller  
**Precondition:** Order exists and amount is known  
**Main Flow:**

1. Caller sends order id, amount, provider, status and transaction code.
2. System validates order existence and amount.
3. System updates status to `Paid`, `Failed` or `Refunded`.
4. System returns updated payment result.

