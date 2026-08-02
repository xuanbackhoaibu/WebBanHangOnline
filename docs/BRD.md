# BRD - Online Fashion E-commerce Website

## 1. Business Context

WebBanHangOnline la website ban hang thoi trang cho phep khach hang xem san pham, quan ly gio hang, dat hang, thanh toan va theo doi don hang. Admin co the quan ly catalog, ton kho, nguoi dung, don hang, thong bao, ho tro va bao cao doanh thu.

## 2. Business Goals

- Tang kha nang ban hang online thong qua website co day du luong mua sam.
- Giam thao tac thu cong khi quan ly san pham, bien the, don hang va ton kho.
- Cung cap dashboard doanh thu de admin theo doi tinh hinh kinh doanh.
- Ho tro nhieu phuong thuc thanh toan: COD, VNPay, MoMo, VietQR.
- Tao nen tang demo production-ready bang Docker de nha tuyen dung co the clone va chay.

## 3. Stakeholders

| Stakeholder | Muc tieu |
| --- | --- |
| Customer | Tim kiem san pham, dat hang, thanh toan, theo doi don |
| Admin | Quan ly san pham, ton kho, don hang, nguoi dung, bao cao |
| Support Staff | Tiep nhan cau hoi va xu ly yeu cau ho tro |
| Developer/Tester | Kiem thu API, luong nghiep vu va tinh dung du lieu |

## 4. Scope

### In Scope

- Dang ky, dang nhap, phan quyen Admin/Client.
- Danh muc, san pham, hinh anh, bien the size/mau/ton kho.
- Gio hang, wishlist, review, checkout, order tracking.
- Thanh toan COD, VNPay, MoMo, VietQR va webhook/IPN demo.
- Admin dashboard, revenue report, top products, export Excel.
- Public catalog API, admin analytics API, Postman collection.

### Out of Scope

- Ket noi payment production voi tai khoan that.
- Van chuyen/logistics tu dong voi ben thu ba.
- Multi-vendor marketplace.

## 5. Business Rules

- Chi Admin duoc truy cap khu vuc `/Admin` va API `/api/admin/*`.
- Khach hang phai dang nhap de dat hang, xem don hang va huy don.
- Chi duoc huy don hang co trang thai `Pending`.
- Don hang da `Paid`, `Failed` hoac `Refunded` khong duoc webhook demo cap nhat lai.
- So tien webhook phai khop `TotalAmount` cua don hang.
- San pham phai co gia lon hon 0 va ton kho bien the khong am.

## 6. Success Metrics

- Clone repo va chay bang `docker compose up --build` thanh cong.
- Seed duoc tai khoan admin/customer va du lieu san pham mau.
- Checkout tao duoc don hang va tru ton kho.
- Dashboard hien thi tong don, doanh thu, khach hang, san pham, top products.
- Postman collection test duoc catalog API va admin analytics API.

