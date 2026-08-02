# SRS - Software Requirements Specification

## 1. Functional Requirements

| ID | Requirement | Priority |
| --- | --- | --- |
| FR-01 | User can register, login, logout and manage account profile | High |
| FR-02 | Customer can browse products by category and keyword | High |
| FR-03 | Customer can view product details, variants, reviews and stock | High |
| FR-04 | Customer can add product variants to cart and update quantity | High |
| FR-05 | Customer can checkout selected cart items | High |
| FR-06 | System validates address, phone, payment method and stock before creating order | High |
| FR-07 | System supports COD, VNPay, MoMo and VietQR payment flows | High |
| FR-08 | Payment callback/IPN updates order payment status after validating signature/amount | High |
| FR-09 | Customer can view order history and cancel pending orders | Medium |
| FR-10 | Admin can manage products, categories, variants, users and orders | High |
| FR-11 | Admin can view revenue dashboard and export revenue report | High |
| FR-12 | Admin can call analytics API for dashboard summary and top products | Medium |
| FR-13 | Public API exposes product catalog and category data for API testing/demo | Medium |

## 2. Non-functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-01 | Application can run locally with Docker Compose |
| NFR-02 | Database schema is managed through EF Core migrations |
| NFR-03 | Secrets are configured through environment variables/user-secrets |
| NFR-04 | Admin endpoints require role-based authorization |
| NFR-05 | Public API responses should be paginated where needed |
| NFR-06 | Payment webhook must validate status, order existence and amount |

## 3. Roles and Permissions

| Feature | Guest | Client | Admin |
| --- | --- | --- | --- |
| Browse products | Yes | Yes | Yes |
| Add to cart | No | Yes | Yes |
| Checkout | No | Yes | Yes |
| View own orders | No | Yes | Yes |
| Manage products | No | No | Yes |
| Manage users | No | No | Yes |
| Revenue dashboard | No | No | Yes |
| Admin analytics API | No | No | Yes |

## 4. Core Flows

### Checkout

1. Client logs in.
2. Client adds product variant to cart.
3. Client submits checkout form.
4. System validates stock, address, phone and payment method.
5. System creates order and order details inside a transaction.
6. System deducts stock and removes purchased cart items.
7. System redirects to selected payment flow or order success page.

### Payment Callback/IPN

1. Payment provider sends callback/IPN with transaction data.
2. System validates secure hash/signature.
3. System validates order id and amount.
4. System prevents duplicate finalization.
5. System updates order status and payment date.

