"""# Tài Liệu Kỹ Thuật: Quy Trình Xác Thực (Authentication Flow)

Tài liệu này mô tả chi tiết luồng xác thực sử dụng cơ chế **JWT (JSON Web Token)** với cấu hình thời gian sống cố định (Fixed Window), kết hợp bảng Blacklist để quản lý đăng xuất (Logout) mà không cần lưu trữ trạng thái Refresh Token trong cơ sở dữ liệu.

## 1. Cấu Hình Tokens (Token Specifications)

| Loại Token | Thời gian hết hạn (`exp`) | Cơ chế định danh (`jti`) | Mô tả & Hành vi |
| :--- | :--- | :--- | :--- |
| **Access Token** | 5 phút | Mỗi token có một `jti` riêng biệt | Dùng để xác thực các yêu cầu API thông thường. Thường xuyên được làm mới tự động bởi client. |
| **Refresh Token** | 7 ngày | Mỗi token có một `jti` riêng biệt | Được cấp khi đăng nhập thành công. Thời gian hết hạn tính từ lúc đăng nhập và **không gia hạn** (Fixed Window). |

> 💡 **Blacklist Table**: Hệ thống cơ sở dữ liệu (ví dụ: Redis hoặc một bảng trong DB tốc độ cao) lưu trữ danh sách các `jti` của những token đã bị vô hiệu hóa trước thời hạn tự nhiên (khi người dùng thực hiện hành động Đăng xuất - Logout).

---

## 2. Quy Trình Chi Tiết (Detailed Flow)

### 1) Đăng Nhập (Login)
* Người dùng gửi thông tin đăng nhập thành công lên hệ thống.
* Server tiến hành khởi tạo bộ đôi mã thông báo:
  * **Access Token** (thời gian sống 5 phút, đính kèm `jti` riêng).
  * **Refresh Token** (thời gian sống 7 ngày từ thời điểm đăng nhập, đính kèm `jti` riêng).
* Client tiếp nhận và lưu trữ bộ đôi token này bảo mật ở phía client (và có thể lưu kèm `deviceId` nếu cần thiết).

### 2) Gọi API Thông Thường (API Request)
* Client đính kèm **Access Token** vào HTTP Header khi gửi yêu cầu lên Server: `Authorization: Bearer <access_token>`
* Server thực hiện giải mã và kiểm tra chữ ký (Verify JWT) theo phương thức thông thường.
* Server thực hiện đối chiếu `jti` của Access Token với **Blacklist Table**:
  * **Trường hợp jti nằm trong danh sách blacklist**: Server ngay lập tức phản hồi mã lỗi `401 Unauthorized`.
  * **Trường hợp jti không nằm trong blacklist**: Cho phép yêu cầu đi qua và truy cập tài nguyên bình thường.

### 3) Làm Mới Token (Refresh Token Flow)
*Luồng này được kích hoạt tự động ở phía client khi Access Token hết hạn.*
* Client gửi yêu cầu tới endpoint `/refresh`, truyền theo **Refresh Token** hiện tại.
* Server thực hiện xác thực chữ ký và kiểm tra hạn sử dụng của Refresh Token.
* Server đối chiếu `jti` của Refresh Token này với **Blacklist Table**:
  * Nếu `jti` đã bị chặn (nằm trong blacklist) -> Trả về lỗi `401`.
* Nếu Refresh Token hoàn toàn hợp lệ, Server cấp một **Access Token mới** (thời hạn sống 5 phút, đi kèm một mã định danh `jti` hoàn toàn mới).
* **Lưu ý quan trọng**: Refresh Token cũ **không được kéo dài hay gia hạn**, mốc thời gian hết hạn tối đa vẫn giữ nguyên thời hạn 7 ngày ban đầu kể từ khi đăng nhập.

### 4) Đăng Xuất Một Thiết Bị (Single-Device Logout)
* Client gửi yêu cầu tới endpoint `/logout`, đính kèm **Access Token** (và **Refresh Token** nếu có).
* Server thực hiện trích xuất giá trị `jti` và thời gian hết hạn `exp` từ Access Token, sau đó ghi thông tin này vào **Blacklist Table**.
* Nếu client gửi kèm Refresh Token, Server cũng thực hiện trích xuất `jti` của Refresh Token đó và lưu vào bảng Blacklist.
* Kể từ khoảnh khắc này, toàn bộ các token cũ của thiết bị này đều bị vô hiệu hóa ngay lập tức đối với mọi yêu cầu kế tiếp.
* Client thực hiện xóa hoàn toàn các token khỏi bộ nhớ cục bộ trên thiết bị của mình.

### 5) Hết Hạn Tự Nhiên & Dọn Dẹp Dữ Liệu (Cleanup Policy)
* **Access Token**: Tự động hết hiệu lực sau tối đa 5 phút kể từ lúc tạo mà không cần can thiệp.
* **Refresh Token**: Tự động hết hiệu lực đúng sau 7 ngày kể từ thời điểm đăng nhập ban đầu.
* **Cơ chế Dọn dẹp (Cleanup)**: Để tối ưu hóa kích thước bảng Blacklist, hệ thống có thể chạy một tiến trình ngầm định kỳ (Cron Job) để xóa bỏ hoàn toàn những bản ghi blacklist có giá trị `exp` nhỏ hơn thời gian hiện tại (các token đã hết hạn tự nhiên).

---

## 3. Ghi Chú Kiến Trúc (Architecture Notes)
* **Hệ Thống Không Trạng Thái (Stateless)**: Đây là mô hình sử dụng danh sách blacklist duy nhất, giúp tối ưu hiệu năng khi không bắt buộc phải lưu trữ và cập nhật liên tục trạng thái hoạt động của mọi Refresh Token trong cơ sở dữ liệu chính.
* **Đăng Xuất Độc Lập**: Hành động Đăng xuất (Logout) chỉ áp dụng và vô hiệu hóa các mã thông báo của riêng thiết bị đang sử dụng, hoàn toàn không làm ảnh hưởng đến các phiên làm việc hợp lệ của cùng một tài khoản trên những thiết bị khác.
"""