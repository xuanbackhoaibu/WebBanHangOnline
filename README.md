# WebBanHangOnline - Online Fashion E-commerce Website

WebBanHangOnline la website ban hang thoi trang xay dung bang **ASP.NET Core MVC + SQL Server + Entity Framework Core + Identity**. Du an tap trung vao luong e-commerce end-to-end: catalog, gio hang, checkout, phan quyen admin/customer, dashboard doanh thu, tich hop payment va API testing demo.

> Muc tieu repo: tro thanh du an chu luc trong portfolio cho vi tri **Backend .NET Intern**, **System Analyst Intern** va **API Testing Intern**.

## Highlights for Recruiters

- Full e-commerce flow: product catalog, variants, stock, cart, wishlist, review, checkout, order tracking.
- Role-based access control with ASP.NET Core Identity: `Admin`, `Client`, `User`.
- Admin dashboard: total orders, revenue, today revenue, status distribution, recent orders, top products.
- Payment flows: COD, VNPay, MoMo, VietQR and demo payment webhook for API testing.
- Public API + admin analytics API with OpenAPI YAML and Postman collection.
- Docker Compose demo with SQL Server, seeded accounts and seeded fashion products.
- System Analyst documentation pack: BRD, SRS, Use Case, ERD, API spec, test cases.

## Tech Stack

| Layer | Technology |
| --- | --- |
| Backend | C#, ASP.NET Core MVC/Razor Pages, ASP.NET Core Identity |
| API | ASP.NET Core Controller API, OpenAPI documentation, Postman |
| Database | SQL Server, Entity Framework Core, Migrations, Seed data |
| Realtime | SignalR chat hub |
| Payment | COD, VNPay callback/IPN, MoMo service, VietQR |
| AI/API | Google Gemini API for product consultation chatbot |
| Frontend | Razor Views, Bootstrap, jQuery, CSS/JavaScript |
| DevOps | Docker, Docker Compose |
| Reporting | Admin dashboard, revenue report, Excel export with ClosedXML |

## Core Features

### Customer

- Browse products by category and SEO URL: `san-pham/{slug}-{id}`.
- View product images, variants, price, flash sale price and reviews.
- Add to cart, update cart, wishlist products and checkout selected items.
- Choose payment method: COD, VNPay, MoMo, VietQR.
- View order history and cancel pending orders.
- Use support/chat features and chatbot consultation.

### Admin

- Manage products, product images, variants, categories and stock.
- Manage users and roles.
- Manage orders and update order status.
- Manage notifications, support requests and FAQs.
- View revenue dashboard and recent orders.
- Export revenue report to Excel.
- Test admin analytics API and demo payment webhook.

## Role Matrix

| Feature | Guest | Client | Admin |
| --- | --- | --- | --- |
| Browse catalog | Yes | Yes | Yes |
| Product detail | Yes | Yes | Yes |
| Cart/checkout | No | Yes | Yes |
| Order history | No | Own orders | All through admin |
| Admin dashboard | No | No | Yes |
| Product/order/user management | No | No | Yes |
| Public catalog API | Yes | Yes | Yes |
| Admin analytics API | No | No | Yes |

## Project Structure

```text
WebBanHangOnline/
  Areas/Admin/          Admin controllers and Razor views
  Areas/Identity/       Login, register and account management
  Controllers/          Customer MVC controllers
  Controllers/Api/      Catalog, analytics and payment demo APIs
  Data/                 DbContext and database seeding
  Hubs/                 SignalR chat hub
  Models/               Entities, view models and payment models
  Services/             Payment integrations
  Views/                Customer Razor views
  wwwroot/              CSS, JavaScript, images and frontend libraries
docs/
  BRD.md
  SRS.md
  USE_CASES.md
  ERD.md
  API_SPEC.md
  TEST_CASES.md
  openapi.yaml
  postman/WebBanHangOnline.postman_collection.json
```

## Run with Docker

Requirements:

- Docker Desktop

Start app and SQL Server:

```bash
docker compose up --build
```

Open:

```text
http://localhost:8083
```

Docker Compose creates:

- Web container: `webthoitrang`
- SQL Server container: `webthoitrang-db`
- Database: `WebThoiTrang`
- Volume: `webthoitrang-sql-data`

## Run Locally without Docker

Requirements:

- .NET SDK 9
- SQL Server

```bash
git clone https://github.com/xuanbackhoaibu/WebBanHangOnline.git
cd WebBanHangOnline
dotnet restore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=WebBanHangOnline;Trusted_Connection=True;TrustServerCertificate=True" --project WebBanHangOnline/WebBanHangOnline.csproj
dotnet run --project WebBanHangOnline/WebBanHangOnline.csproj
```

Optional secrets:

```bash
dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key" --project WebBanHangOnline/WebBanHangOnline.csproj
dotnet user-secrets set "VNPay:TmnCode" "your-vnpay-code" --project WebBanHangOnline/WebBanHangOnline.csproj
dotnet user-secrets set "VNPay:HashSecret" "your-vnpay-secret" --project WebBanHangOnline/WebBanHangOnline.csproj
```

## Demo Accounts

Admin:

```text
Email: admin@shop.com
Password: Admin@123
```

Customer:

```text
Email: customer@shop.com
Password: Customer@123
```

## Seeded Demo Data

When database is empty, the app seeds:

- 4 categories: Do Nam, Do Nu, Be Trai, Be Gai.
- 12 fashion products with images, descriptions and flash sale data.
- Product variants with size, color and stock.
- FAQ/support content and notifications.
- 1 admin account, 1 customer account and 1 sample order.

## API Documentation

OpenAPI file:

```text
docs/openapi.yaml
```

Postman collection:

```text
docs/postman/WebBanHangOnline.postman_collection.json
```

Main API endpoints:

```text
GET  /api/catalog/categories
GET  /api/catalog/products?page=1&pageSize=12
GET  /api/catalog/products/{id}
GET  /api/admin/analytics/summary
GET  /api/admin/analytics/revenue?days=7
GET  /api/admin/analytics/top-products?take=10
POST /api/payments/demo-webhook
```

Admin APIs require an authenticated Admin cookie. In Postman, login to the website as admin first, then reuse the browser/session cookie or configure Postman cookie jar for `localhost`.

## Payment Flow

Supported methods:

- `COD`: creates and confirms order in local demo flow.
- `VNPay`: builds payment URL, validates return callback and IPN signature.
- `MoMo`: service layer is prepared for MoMo payment integration.
- `VietQR`: generates QR payment screen based on order amount.
- `Demo webhook`: admin-only endpoint for API testing payment status updates.

Demo webhook request:

```json
{
  "orderId": 1,
  "amount": 100000,
  "status": "Paid",
  "provider": "DemoGateway",
  "transactionCode": "DEMO-0001"
}
```

## System Analyst Documentation

This repo includes a BA/SA documentation pack:

- [BRD](docs/BRD.md)
- [SRS](docs/SRS.md)
- [Use Cases](docs/USE_CASES.md)
- [ERD and Data Dictionary](docs/ERD.md)
- [API Specification](docs/API_SPEC.md)
- [API Test Cases](docs/TEST_CASES.md)

These documents show requirement analysis, scope control, business rules, role matrix, data design and API testing mindset.

## Suggested Demo Script

1. Start project with Docker.
2. Open home page and browse fashion products.
3. Login as customer, add products to cart and checkout.
4. Login as admin and review dashboard/reports.
5. Open Postman and test catalog API.
6. Test admin analytics API with admin session.
7. Show BRD/SRS/ERD/API test case documentation in `docs/`.

## Screenshot Placeholders

Put real screenshots here after running the project:

```text
docs/screenshots/home.png
docs/screenshots/product-detail.png
docs/screenshots/cart-checkout.png
docs/screenshots/admin-dashboard.png
docs/screenshots/postman-api.png
```

Current visual assets are available in:

```text
WebBanHangOnline/wwwroot/images/
```

## Production Notes

- Do not commit real payment secrets, API keys or production database passwords.
- Replace demo SQL password before public deployment.
- Configure payment return URLs per deployment domain.
- Use HTTPS and a managed SQL Server/PostgreSQL instance for production hosting.
- Add automated tests for checkout, stock update, admin authorization and payment callbacks.

## Author

Tran Xuan Bac

- GitHub: https://github.com/xuanbackhoaibu
- Portfolio: https://xuanbackhoaibu.github.io/profile/
