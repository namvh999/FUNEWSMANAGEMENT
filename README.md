# PRN232 – ASSIGNMENT 02: Distributed and Intelligent News Management System (FUNewsManagementSystem v2)

Hệ thống Quản lý Tin tức Phân tán và Thông minh được thiết kế theo kiến trúc Microservices gồm **4 thành phần độc lập**: **Core API**, **Analytics API**, **AI API**, và **Frontend (ASP.NET Core MVC)**, giao tiếp qua `HttpClient`, bảo mật với JWT & Refresh Token, thông báo thời gian thực với SignalR, gợi ý thẻ thông minh bằng AI & Learning Cache, và trực quan hóa dữ liệu qua Chart.js.

---

## 🏛️ Kiến trúc Hệ thống (Distributed Architecture)

```
                       +-----------------------------------+
                       |    Frontend (Port 5000)           |
                       |    ASP.NET Core MVC / Bootstrap 5 |
                       +-----------------+-----------------+
                                         |
                +------------------------+------------------------+
                | (HttpClient + Polly)   | (HttpClient + Polly)   | (HttpClient + Polly)
                v                        v                        v
     +---------------------+  +----------------------+  +--------------------+
     |      Core API       |  |    Analytics API     |  |       AI API       |
     |     (Port 5001)     |  |     (Port 5002)      |  |    (Port 5003)     |
     | JWT, CRUD, SignalR, |  | Dashboard, Trending, |  | NLP Keyword Engine |
     | Audit Log, Upload   |  | Recommend, Excel Exp |  | & Learning Cache   |
     +----------+----------+  +----------+-----------+  +--------------------+
                |                        |
                +-----------+------------+
                            | (EF Core)
                            v
               +---------------------------+
               |  Database: SQL Server     |
               |  FUNewsManagement.sql     |
               +---------------------------+
```

---

## 📋 Cấu trúc Thư mục và File Nộp bài (Section 6 Submission)

| Tên File / Thư mục | Mô tả |
| :--- | :--- |
| **`FUNewsManagement.sql`** | Toàn bộ script tạo bảng, khóa chính, khóa ngoại và dữ liệu mẫu (Seed Data) |
| **`FUNewsManagement_CoreAPI.sln`** | Solution riêng cho Core API |
| **`FUNewsManagement_AnalyticsAPI.sln`** | Solution riêng cho Analytics API |
| **`FUNewsManagement_AIAPI.sln`** | Solution riêng cho AI API |
| **`FUNewsManagement_FE.sln`** | Solution riêng cho Frontend MVC |
| **`FUNewsManagementSystem.sln`** | Solution tổng hợp chứa cả 4 project để mở cùng lúc trong Visual Studio |
| **`run_all.bat` / `run_all.sh`** | Script 1-click khởi chạy đồng thời cả 4 dịch vụ |
| **`README.md`** | Tài liệu hướng dẫn cài đặt, tài khoản thử nghiệm và đặc tả API |

---

## 🔑 Tài khoản Đăng nhập Hệ thống (Test Accounts)

| Vai trò (Role) | Email | Mật khẩu (Password) | Quyền hạn |
| :--- | :--- | :--- | :--- |
| **Admin (Quản trị viên)** | `admin@funews.com` | `admin@123` | Xem Dashboard, Quản lý toàn bộ Tin tức, Chuyên mục, Thẻ, Quản lý Tài khoản, Xem Nhật ký Audit Log, Xuất Excel |
| **Staff (Biên tập viên)** | `staff1@funews.com` | `staff@123` | Quản lý Bài viết, Thêm/Sửa tin, Gợi ý Tags AI, Xem chuyên mục và thẻ |
| **Staff (Phóng viên)** | `staff2@funews.com` | `staff@123` | Quản lý Bài viết |

---

## 🚀 Hướng dẫn Cài đặt & Khởi chạy

### Bước 1: Khởi tạo Cơ sở Dữ liệu (Database Setup)
1. Mở **SQL Server Management Studio (SSMS)** hoặc Azure Data Studio.
2. Mở file [FUNewsManagement.sql](./FUNewsManagement.sql) và bấm **Execute**.
3. CSDL `FUNewsManagement` sẽ được tạo cùng các bảng: `SystemAccount`, `Category`, `Tag`, `NewsArticle`, `NewsTag`, `AuditLog`, `LearningTag` và dữ liệu mẫu ban đầu.
4. *(Tùy chọn)* Cấu hình chuỗi kết nối trong `appsettings.json` của Core API và Analytics API nếu bạn sử dụng thông tin kết nối SQL Server khác với mặc định (`Server=localhost;Database=FUNewsManagement;...`).
   *(Lưu ý: Nếu không có SQL Server, hệ thống đã cài sẵn chế độ InMemory Fallback tự động nên vẫn chạy được 100% dữ liệu mẫu).*

### Bước 2: Khởi chạy 4 Dịch vụ (Microservices)

#### Cấu hình khóa JWT
Core API yêu cầu khóa ký JWT tối thiểu 32 byte. Hãy tạo và đặt biến môi trường trước khi khởi chạy dịch vụ.

```bash
# macOS / Linux
export Jwt__Key="$(openssl rand -base64 48)"
```

```powershell
# Windows PowerShell
$env:Jwt__Key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

#### Cách 1: Chạy bằng file Script (Khuyên dùng - 1 Click)
- **Trên Windows**: Click đúp vào file `run_all.bat`.
- **Trên macOS / Linux**: Mở terminal tại thư mục này và chạy `./run_all.sh`.

#### Cách 2: Chạy độc lập bằng dòng lệnh
Mở 4 cửa sổ Terminal riêng biệt:
```bash
# Terminal 1: Core API (Port 5001)
cd FUNewsManagement_CoreAPI && dotnet run --launch-profile http

# Terminal 2: Analytics API (Port 5002)
cd FUNewsManagement_AnalyticsAPI && dotnet run --launch-profile http

# Terminal 3: AI API (Port 5003)
cd FUNewsManagement_AIAPI && dotnet run --launch-profile http

# Terminal 4: Frontend MVC (Port 5000)
cd FUNewsManagement_FE && dotnet run --launch-profile http
```

#### Cách 3: Mở trong Visual Studio
Mở file `FUNewsManagementSystem.sln` -> Chuột phải vào Solution -> Chọn **Set Startup Projects...** -> Chọn **Multiple startup projects** -> Đặt Action là **Start** cho cả 4 projects -> Bấm **F5**.

---

## 🌐 Danh sách URL & Endpoints

| Dịch vụ | Địa chỉ URL | Chức năng chính |
| :--- | :--- | :--- |
| **Frontend Web** | **http://localhost:5000** | Giao diện người dùng MVC, Dashboard, Modals, SignalR |
| **Core API Swagger** | **http://localhost:5001/swagger** | CRUD Account, Category, Tag, News, Upload ảnh, Audit Log, JWT Auth |
| **Analytics API Swagger** | **http://localhost:5002/swagger** | Dashboard metrics, Trending news, 3 Related Articles, Export Excel |
| **AI API Swagger** | **http://localhost:5003/swagger** | Suggest Tags (NLP + OpenAI), Learning Cache |
| **SignalR Hub** | **http://localhost:5001/hubs/notifications** | Kênh thông báo bài viết mới thời gian thực |

---

## 🎯 Chi tiết Thực hiện Các Yêu cầu Kỹ thuật

### 1. Core API (Port 5001)
- **JWT Authentication & Refresh Token Flow**:
  - `POST /api/auth/login`: Cấp `accessToken` (HMAC-SHA256, 60 phút) và `refreshToken` (Crypto Random 64 bytes).
  - `POST /api/auth/refresh`: Cấp mới cặp token khi access token hết hạn mà không bắt người dùng đăng nhập lại.
- **Upload Ảnh bài viết**:
  - `POST /api/news/upload-image`: Kiểm tra file type (.jpg, .jpeg, .png, .webp, .gif) và kích thước (tối đa 5MB), lưu vào `wwwroot/uploads`.
- **Audit Logging**:
  - Bảng `AuditLog` ghi nhận: `UserId`, `UserName`, `Action` (CREATE, UPDATE, DELETE, LOGIN), `Entity`, `BeforeData` (JSON) và `AfterData` (JSON), `Timestamp`.
  - Endpoint `GET /api/auditlog` hỗ trợ lọc theo Người dùng, Đối tượng, Hành động và Phân trang.
- **Quy tắc Nghiệp vụ (Business Rules)**:
  - Ngăn chặn xóa Chuyên mục nếu đang có bài viết trực thuộc.
  - Ngăn chặn xóa Tài khoản nếu tài khoản đó đã tạo bài viết.
  - Ngăn chặn tạo hoặc cập nhật trùng tên Thẻ Tag.
  - Bắt buộc nhập Mật khẩu cũ khi thực hiện đổi mật khẩu tài khoản.
- **SignalR Hub**:
  - `/hubs/notifications`: Phát sự kiện `ReceiveNotification` tới mọi client ngay khi bài viết mới được tạo.

### 2. Analytics API (Port 5002)
- `GET /api/analytics/dashboard`: Thống kê tổng số bài viết, phân bố theo chuyên mục, trạng thái, tác giả, hỗ trợ lọc theo thời gian, chuyên mục, tác giả.
- `GET /api/analytics/trending`: Danh sách bài viết được xem nhiều nhất (`ViewCount DESC`).
- `GET /api/recommend/{id}`: Gợi ý tối đa **3 bài viết liên quan** ưu tiên theo độ trùng khớp Thẻ Tags, sau đó đến cùng Chuyên mục.
- `GET /api/analytics/export`: Xuất báo cáo dữ liệu định dạng bảng tính Excel/CSV có UTF-8 BOM hiển thị chuẩn tiếng Việt.

### 3. AI API (Port 5003)
- `POST /api/ai/suggest-tags`:
  - Input: `{ "content": "..." }`
  - Output: Danh sách tags kèm tỷ lệ tin cậy `confidence` (Ví dụ: `AI (95%)`, `Cloud (85%)`).
  - Sử dụng thuật toán phân tích từ khóa NLP, lọc stop-words tiếng Anh & tiếng Việt. Có hỗ trợ tích hợp OpenAI API nếu cấu hình key.
  - **Learning Cache**: Ghi nhớ tần suất các thẻ tag được người dùng lựa chọn, tự động tăng điểm ưu tiên cho các lần gợi ý sau.
- `POST /api/ai/learn-tag`: Ghi nhận thẻ tag vừa được gán vào bộ nhớ học.

### 4. Frontend (Port 5000 - ASP.NET Core MVC)
- **Không truy cập CSDL trực tiếp**: 100% dữ liệu được gọi qua `HttpClientFactory`.
- **DelegatingHandler (`AuthTokenHandler`)**: Tự động đính kèm `Bearer Token` từ Session vào Header, tự động gọi `/api/auth/refresh` khi token hết hạn (401).
- **Polly Retry Policy**: Tự động thử lại cuộc gọi API tối đa 3 lần với cơ chế exponential backoff khi gặp sự cố mạng tạm thời.
- **Chế độ Ngoại tuyến (Offline Mode)**: Tự động phát hiện khi các API không thể kết nối, hiển thị thanh cảnh báo màu vàng "CHẾ ĐỘ NGOẠI TUYẾN (OFFLINE MODE)", hiển thị dữ liệu đã lưu trong Cache và tạm thời vô hiệu hóa các nút Thêm/Sửa/Xóa.
- **Background Worker (`HostedService`)**: Chạy ngầm định kỳ mỗi 6 giờ để làm mới bộ nhớ đệm Dashboard và Chuyên mục.
- **Giao diện & UX**:
  - Biểu đồ **Chart.js** (Pie Chart, Bar Chart) trên Dashboard.
  - Toàn bộ thao tác CRUD sử dụng **Bootstrap Modals**.
  - Nút **"💡 Gợi ý Tags (AI)"** hiển thị Chips tag tương tác ngay trên form tạo bài viết.
  - Hộp thoại xác nhận (**Confirmation Prompt**) trước mọi hành động Xóa hoặc Thao tác quan trọng.
  - Biểu tượng **Loading Indicator** toàn cục khi thực hiện các tác vụ API.
  - Chuông thông báo **SignalR Real-time** ở góc phải thanh menu lưu giữ 10 thông báo mới nhất.
