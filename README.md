# 🍜 DayliFood — Hướng dẫn chạy và test

## Yêu cầu hệ thống

| Tool | Version |
|------|---------|
| Docker Desktop | 4.x+ |
| .NET SDK | 10.x (brew install dotnet) |
| macOS | Monterey+ |

---

## 🚀 Chạy nhanh bằng Docker

```bash
# 1. Clone về
git clone <repo-url>
cd Daylifood_chatbot_vnpay

# 2. Khởi động
docker compose up -d

# 3. Truy cập app
open http://localhost:5174
```

> DB được tự tạo và migrate khi app khởi động lần đầu.

---

## ⚙️ Cấu hình môi trường

Tất cả secrets nằm trong file `.env` (không commit git):

```bash
# Gemini AI (chatbot)
OPENAI_API_KEY=AIzaSy...
OPENAI_BASE_URL=https://generativelanguage.googleapis.com/v1beta/openai/
OPENAI_MODEL=gemini-2.5-flash

# VNPay Sandbox
VNPAY_TMN_CODE=ZDFZQ69K
VNPAY_HASH_SECRET=GPJ10UEZTC8IJODE9T5O5QSADE42TVQ5

# Momo Sandbox
MOMO_PARTNER_CODE=MOMO
MOMO_ACCESS_KEY=F8BBA842ECF85
MOMO_SECRET_KEY=K951B6PE1waDMi640xX08PD3vg6EkVlz
```

---

## 💳 Test thanh toán VNPay

### Thông tin tài khoản sandbox

| Thông tin | Giá trị |
|-----------|---------|
| TmnCode | `ZDFZQ69K` |
| Merchant Admin | https://sandbox.vnpayment.vn/merchantv2/ |
| Tài liệu API | https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html |

### Thẻ test NCB

| Trường | Giá trị |
|--------|---------|
| **Ngân hàng** | NCB |
| **Số thẻ** | `9704198526191432198` |
| **Tên chủ thẻ** | `NGUYEN VAN A` |
| **Ngày phát hành** | `07/15` |
| **OTP** | `123456` |

### Quy trình test VNPay

```
1. Đăng nhập → Thêm món vào giỏ hàng
2. Checkout → Chọn "Thanh toán qua VNPay"
3. Nhấn Đặt hàng → Tự động chuyển sang sandbox.vnpayment.vn
4. Chọn ngân hàng NCB → Điền thông tin thẻ test ở trên
5. Nhập OTP: 123456
6. Được redirect về /Payment/VnPayReturn
7. Trạng thái đơn hàng cập nhật thành "Đã thanh toán"
```

> ⚠️ ReturnUrl `http://localhost:5174` phải được đăng ký trong [Merchant Admin](https://sandbox.vnpayment.vn/merchantv2/).

---

## 💰 Test thanh toán Momo

### Thông tin sandbox Momo

| Trường | Giá trị |
|--------|---------|
| PartnerCode | `MOMO` |
| AccessKey | `F8BBA842ECF85` |
| SecretKey | `K951B6PE1waDMi640xX08PD3vg6EkVlz` |
| RequestType | `payWithATM` |
| API URL | `https://test-payment.momo.vn/v2/gateway/api/create` |

### Quy trình test Momo

```
1. Đăng nhập → Thêm món vào giỏ hàng
2. Checkout → Chọn "Thanh toán Momo"
3. Nhấn Đặt hàng → Chuyển sang trang Momo sandbox
4. Dùng app Momo test hoặc thẻ ATM test để thanh toán
5. Được redirect về /Payment/MomoReturn
```

> ℹ️ Momo IPN yêu cầu URL public (ngrok) để nhận callback. Khi test local, trạng thái được cập nhật qua ReturnUrl thay vì IPN.

---

## 🤖 Test Chatbot AI (Gemini)

Chatbot xuất hiện ở góc phải màn hình. Gợi ý câu hỏi:

```
- "Gợi ý món ăn ngon dưới 50k"
- "Menu quán có gì?"
- "Tôi muốn đặt phở bò"
- "Thanh toán VNPay hay Momo nhanh hơn?"
```

Chatbot sẽ hiển thị **product cards** clickable từ dữ liệu thực trong DB.

---

## 🛠️ Commands hữu ích

```bash
# Xem logs realtime
docker compose logs -f app

# Xem logs Momo/VNPay
docker compose logs app | grep -i "momo\|vnpay\|payment"

# Restart app (không rebuild)
docker compose restart app

# Rebuild image khi thay đổi code
docker compose up -d --build

# Dừng tất cả
docker compose down

# Xóa database để reset
docker compose down -v
docker compose up -d
```

---

## 🪟 Chạy trực tiếp trên Windows (bạn)

```powershell
cd Daylifood
dotnet restore
dotnet ef database update
dotnet run
# App chạy tại https://localhost:5174
```

> Connection string dùng `Server=.` (LocalDB) đã có trong `appsettings.json` gốc.

---

## 📂 Cấu trúc project

```
Daylifood/
├── Controllers/
│   ├── PaymentController.cs   # VNPay + Momo routes
│   ├── ChatbotController.cs   # AI chatbot API
│   └── OrderController.cs     # Checkout flow
├── Services/
│   ├── VnPayService.cs        # HMAC-SHA512, build URL
│   ├── MomoService.cs         # HMAC-SHA256, Momo v2 API
│   └── OpenAiChatbotService.cs # Gemini chat/completions
├── Options/
│   ├── VnPayOptions.cs
│   ├── MomoOptions.cs
│   └── OpenAiOptions.cs
├── Views/
│   ├── Payment/VnPayReturn.cshtml
│   ├── Payment/MomoReturn.cshtml
│   └── Shared/_ChatbotWidget.cshtml  # Product cards UI
├── Dockerfile                 # Multi-stage .NET 10 build
docker-compose.yml             # SQL Server 2022 + App
.env                           # Secrets (không commit git)
```

---

## 🔐 Bảo mật

- `.env` và `appsettings.Development.json` **không được commit** lên git
- Xác thực chữ ký HMAC-SHA512 (VNPay) và HMAC-SHA256 (Momo) trên mọi callback
- Kiểm tra `Amount` khớp với DB trước khi cập nhật trạng thái thanh toán
