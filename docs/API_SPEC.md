# API Specification

Base URL when running Docker:

```text
http://localhost:8083
```

## Public Catalog API

### GET `/api/catalog/categories`

Returns active product categories.

### GET `/api/catalog/products`

Query parameters:

| Name | Type | Description |
| --- | --- | --- |
| categoryId | integer | Optional category filter |
| keyword | string | Optional product name/description search |
| page | integer | Default `1` |
| pageSize | integer | Default `12`, max `50` |

### GET `/api/catalog/products/{id}`

Returns product detail with images, variants and review summary.

## Admin Analytics API

Requires Admin login cookie.

### GET `/api/admin/analytics/summary`

Returns total orders, revenue, today revenue, month revenue, customers, products and low-stock variants.

### GET `/api/admin/analytics/revenue?days=7`

Returns daily order count, paid order count and revenue for 1-31 days.

### GET `/api/admin/analytics/top-products?take=10`

Returns top selling products by quantity.

## Demo Payment Webhook

Requires Admin login cookie.

### POST `/api/payments/demo-webhook`

Request body:

```json
{
  "orderId": 1,
  "amount": 100000,
  "status": "Paid",
  "provider": "DemoGateway",
  "transactionCode": "DEMO-0001"
}
```

Allowed statuses:

- `Paid`
- `Failed`
- `Refunded`

