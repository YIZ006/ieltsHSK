# BÁO CÁO BẢO MẬT: THIẾT LẬP BẢO VỆ CỔNG QUẢN TRỊ (PORTAL-HUB) VÀ WAF CLOUDFLARE

> **Ngày cập nhật:** 10/10/2026  
> **Trạng thái:** Đã hoàn thành dọn sạch đường dẫn trong mã nguồn & đóng gói bản build sẵn sàng.

---

## 1. Dọn Sạch Đường Dẫn Quản Trị Trong Mã Nguồn

Để đảm bảo kẻ tấn công không thể dò quét các đường dẫn quản trị mặc định (như `/admin`, `/admin/login`, `/admin/users`...), toàn bộ các chỉ thị routing `@page "/admin/*"` đã được loại bỏ triệt để.

### Bảng đối chiếu đường dẫn:
| Chức năng | Đường dẫn cũ (Đã xóa) | Đường dẫn duy nhất hiện tại |
| :--- | :--- | :--- |
| **Đăng nhập Quản trị** | ~`/admin/login`~ | **`/portal-hub/login`** |
| **Bảng điều khiển (Dashboard)** | ~`/admin`~ | **`/portal-hub`** |
| **Quản lý học viên** | ~`/admin/users`~, ~`/admin/ielts-users`~ | **`/portal-hub/users`**, **`/portal-hub/ielts-users`** |
| **Chấm điểm thi** | ~`/admin/grading`~ | **`/portal-hub/grading`** |
| **Ngân hàng ngữ pháp** | ~`/admin/grammar`~ | **`/portal-hub/grammar`** |
| **Video luyện nghe** | ~`/admin/listen-videos`~ | **`/portal-hub/listen-videos`** |
| **Đề thi thử (Mock Tests)** | ~`/admin/mock-tests`~ | **`/portal-hub/mock-tests`** |
| **Điều hướng (Navigation)** | ~`/admin/navigation`~ | **`/portal-hub/navigation`** |
| **Thông báo hệ thống** | ~`/admin/notifications`~ | **`/portal-hub/notifications`** |
| **Công cụ bài đọc** | ~`/admin/reading-tool`~ | **`/portal-hub/reading-tool`** |
| **Speak Along** | ~`/admin/speak-along`~ | **`/portal-hub/speak-along`** |
| **Truyện đọc (Stories)** | ~`/admin/stories`~ | **`/portal-hub/stories`** |

> Giờ đây nếu bất kỳ ai cố tình truy cập vào `https://ieltshsk.me/admin` hay `https://ieltshsk.me/admin/login`, hệ thống sẽ trả về lỗi **404 Not Found**.

---

## 2. Hướng Dẫn Kích Hoạt Form Bảo Vệ Cloudflare (WAF Custom Rule / Turnstile)

Để thiết lập lớp khiên chắn Cloudflare bảo vệ cho đường dẫn bí mật `/portal-hub*`, bạn thực hiện trực tiếp trên trang quản trị Cloudflare như sau (chỉ mất khoảng 1 phút):

### Bước 1: Mở Cloudflare WAF Custom Rules
1. Truy cập [Cloudflare Dashboard](https://dash.cloudflare.com/) và đăng nhập tài khoản.
2. Chọn tên miền **`ieltshsk.me`**.
3. Ở menu bên trái, chọn **Security** $\rightarrow$ **WAF** (Web Application Firewall).
4. Tại tab **Custom rules**, bấm nút **Create rule** (Tạo quy tắc).

### Bước 2: Thiết lập điều kiện chặn & thử thách bảo mật
Điền các thông số chính xác như sau:
- **Rule name (Tên quy tắc):** `Protect Portal Hub Admin`
- **Field (Trường dữ liệu):** Chọn **URI Path**
- **Operator (Toán tử):** Chọn **starts with** (bắt đầu bằng)
- **Value (Giá trị):** Nhập **`/portal-hub`**
- **Choose action (Hành động thực thi):**
  - Chọn **Managed Challenge** *(Khuyên dùng: Tự động hiển thị màn hình thử thách của Cloudflare để lọc bot tự động, script crawler và giảm thiểu rủi ro brute-force; kết hợp cùng phân quyền JWT bắt buộc tại backend)*.
  - *Hoặc chọn **Interactive Challenge** (yêu cầu người dùng tích xác thực).*

### Bước 3: Lưu và Kích hoạt
- Bấm nút **Deploy** (Triển khai).
- Quy tắc có hiệu lực ngay lập tức trên toàn cầu trong vòng 5 giây!

---

## 3. Trải Nghiệm Truy Cập Sau Khi Thiết Lập
1. Khi bạn hoặc bất kỳ ai gõ: `https://ieltshsk.me/portal-hub/login`
2. **Lớp 1 (Cloudflare WAF):** Trình duyệt lập tức hiển thị màn hình thử thách của Cloudflare ("Checking your browser before accessing ieltshsk.me...").
3. **Lớp 2 (Portal Hub Form):** Sau khi xác thực hợp lệ qua Cloudflare, giao diện đăng nhập Admin Hub mới xuất hiện.
4. **Đăng nhập:** Điền thông tin tài khoản quản trị:
   - **Email:** `admin@ieltshsk.com`
   - **Mật khẩu:** Thiết lập qua biến môi trường `ADMIN_INITIAL_PASSWORD` khi khởi tạo (hoặc mật khẩu do quản trị viên đổi sau lần đăng nhập đầu).
