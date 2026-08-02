# API Testing Checklist

## Catalog API

| ID | Scenario | Steps | Expected Result |
| --- | --- | --- | --- |
| TC-API-01 | Get categories | GET `/api/catalog/categories` | 200 OK, list has category id/name/productCount |
| TC-API-02 | Get first page products | GET `/api/catalog/products?page=1&pageSize=12` | 200 OK, paginated response |
| TC-API-03 | Search product | GET `/api/catalog/products?keyword=ao` | 200 OK, matching products |
| TC-API-04 | Invalid product id | GET `/api/catalog/products/999999` | 404 Not Found |
| TC-API-05 | Page size limit | GET `/api/catalog/products?pageSize=1000` | 200 OK, pageSize capped at 50 |

## Admin API

| ID | Scenario | Steps | Expected Result |
| --- | --- | --- | --- |
| TC-API-06 | Guest accesses admin summary | GET `/api/admin/analytics/summary` without login | 401/302 to login |
| TC-API-07 | Admin accesses summary | Login admin, GET `/api/admin/analytics/summary` | 200 OK, summary metrics |
| TC-API-08 | Revenue chart | GET `/api/admin/analytics/revenue?days=7` | 200 OK, 7 data points |
| TC-API-09 | Top products | GET `/api/admin/analytics/top-products?take=5` | 200 OK, max 5 products |

## Payment Webhook Demo

| ID | Scenario | Steps | Expected Result |
| --- | --- | --- | --- |
| TC-PAY-01 | Valid paid webhook | POST valid order id + exact amount + `Paid` | 200 OK, order status updated |
| TC-PAY-02 | Invalid amount | POST amount different from order total | 400 Bad Request |
| TC-PAY-03 | Invalid status | POST status `Unknown` | 400 Bad Request |
| TC-PAY-04 | Missing order | POST non-existing order id | 404 Not Found |
| TC-PAY-05 | Duplicate finalization | POST for already final order | 200 OK, no duplicate update |

## Business Flow

| ID | Scenario | Expected Result |
| --- | --- | --- |
| TC-BIZ-01 | Customer checkout COD | Order created, cart cleared, stock reduced |
| TC-BIZ-02 | Customer cancels pending order | Order status `Cancelled`, stock restored |
| TC-BIZ-03 | Customer cancels paid order | Request rejected |
| TC-BIZ-04 | Admin updates order status | Dashboard revenue changes when status becomes paid/completed |

