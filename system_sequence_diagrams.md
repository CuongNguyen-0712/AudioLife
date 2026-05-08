# VinhKhanhAudioGuide - System Sequence Diagrams

Dưới đây là các Sequence Diagram (Biểu đồ tuần tự) mô tả luồng hoạt động của các chức năng trong hệ thống VinhKhanhAudioGuide dựa trên kiến trúc và yêu cầu hiện tại.

## 1. Authentication (Đăng nhập/Đăng xuất)
Xử lý đăng nhập phân quyền cho Admin và POI Admin.

```mermaid
sequenceDiagram
    actor User as Admin / POI Admin
    participant Web as Web Interface
    participant Auth as Auth Controller
    participant DB as Database
    
    User->>Web: Nhập email & password
    Web->>Auth: POST /login (credentials)
    Auth->>DB: Truy vấn thông tin user & hash password
    DB-->>Auth: Trả về kết quả xác thực & Role
    alt Đăng nhập thành công
        Auth->>Web: Tạo Cookie session, chuyển hướng trang theo Role (Admin/Shop)
        Web-->>User: Hiển thị Dashboard tương ứng
    else Sai thông tin
        Auth-->>Web: Trả về lỗi
        Web-->>User: Hiển thị thông báo lỗi
    end

    User->>Web: Click Đăng xuất
    Web->>Auth: POST /logout
    Auth->>Web: Xóa Cookie session
    Web-->>User: Chuyển hướng về trang Đăng nhập
```

## 2. Dashboard Admin
Thống kê tổng quan số liệu hệ thống.

```mermaid
sequenceDiagram
    actor Admin
    participant Web as Web Dashboard
    participant API as Dashboard Controller
    participant DB as Database
    
    Admin->>Web: Truy cập trang chủ Dashboard
    Web->>API: Yêu cầu lấy dữ liệu thống kê
    API->>DB: Count(Locations, AudioGuides, Tours, PoiAdmins, PendingRequests)
    DB-->>API: Trả về số liệu thống kê
    API-->>Web: Render Razor Page với dữ liệu
    Web-->>Admin: Hiển thị Dashboard tổng quan
```

## 3. Quản lý địa điểm (Locations)
Thêm/sửa/xóa địa điểm, cập nhật tọa độ, hình ảnh, cấu hình bán kính phát hiện (Geofencing).

```mermaid
sequenceDiagram
    actor Admin as Admin/POI Admin
    participant Web as Web Interface
    participant LocCtrl as Location Controller
    participant DB as Database
    
    Admin->>Web: Thêm/Sửa/Xóa địa điểm (Nhập Tọa độ, Bán kính, Ảnh...)
    Web->>LocCtrl: POST/PUT/DELETE /Locations
    LocCtrl->>DB: Validate & Cập nhật Location Table
    DB-->>LocCtrl: Trạng thái thành công
    LocCtrl-->>Web: Redirect/Hiển thị thông báo
    Web-->>Admin: Cập nhật danh sách địa điểm
```

## 4. Quản lý Audio Guide
Thêm/sửa/xóa nội dung audio, upload file trực tiếp, quản lý transcript và tùy chọn ngôn ngữ.

```mermaid
sequenceDiagram
    actor Admin as Admin/POI Admin
    participant Web as Web Interface
    participant AudioCtrl as AudioGuide Controller
    participant Cloud as CloudinaryStorage
    participant DB as Database
    
    Admin->>Web: Tạo/Sửa Audio Guide (Upload file/Transcript/Ngôn ngữ)
    Web->>AudioCtrl: POST /AudioGuides (File, Text, Lang, LocationId)
    alt Có upload file audio trực tiếp
        AudioCtrl->>Cloud: UploadStream(File)
        Cloud-->>AudioCtrl: Trả về CloudinaryAudioUrl & PublicId
    end
    AudioCtrl->>DB: Lưu AudioGuide Record
    DB-->>AudioCtrl: Trạng thái thành công
    AudioCtrl-->>Web: Redirect/Thông báo
    Web-->>Admin: Cập nhật danh sách Audio Guide
```

## 5. Quản lý danh mục (Categories)
Thao tác CRUD danh mục dành cho các địa điểm.

```mermaid
sequenceDiagram
    actor Admin
    participant Web as Web Interface
    participant CatCtrl as Category Controller
    participant DB as Database
    
    Admin->>Web: Thêm/Sửa/Xóa Category
    Web->>CatCtrl: Gửi thay đổi /Categories
    CatCtrl->>DB: Validate & Update Categories Table
    DB-->>CatCtrl: Thành công
    CatCtrl-->>Web: Render lại danh sách
    Web-->>Admin: Hiển thị danh mục mới
```

## 6. Quản lý Tour
Tạo, chỉnh sửa tour và sắp xếp các điểm dừng (stops).

```mermaid
sequenceDiagram
    actor Admin
    participant Web as Web Interface
    participant TourCtrl as Tour Controller
    participant DB as Database
    
    Admin->>Web: Tạo/Sửa Tour (Nhập thông tin & chọn/sắp xếp Locations)
    Web->>TourCtrl: Gửi thông tin Tour & List<LocationIds>
    TourCtrl->>DB: Cập nhật Tour & thứ tự TourStops Table
    DB-->>TourCtrl: Cập nhật thành công
    TourCtrl-->>Web: Redirect/Thông báo
    Web-->>Admin: Hiển thị danh sách Tour
```

## 7. Text-to-Speech (TTS)
Tự động sinh audio từ văn bản (Hỗ trợ nhiều ngôn ngữ, fallback giữa Edge TTS và Google TTS).

```mermaid
sequenceDiagram
    actor Admin
    participant Web as Web Interface
    participant TTS as TTS Service
    participant Edge as Edge Neural API
    participant Google as Google TTS API
    participant Cloud as Cloudinary
    participant DB as Database
    
    Admin->>Web: Nhập Transcript, Ngôn ngữ & chọn Auto-Generate
    Web->>TTS: Yêu cầu GenerateAudio(Text, Language)
    TTS->>Edge: Gọi API Edge Neural (vi/en/fr/jp/ko/zh)
    alt Nếu Edge API Lỗi (403/Timeout)
        Edge-->>TTS: Trả về lỗi
        TTS->>Google: Fallback gọi Google TTS API
        Google-->>TTS: Audio Stream
    else Edge API Thành công
        Edge-->>TTS: Audio Stream
    end
    TTS->>Cloud: UploadStream(Generated Audio)
    Cloud-->>TTS: Trả về CloudinaryAudioUrl & PublicId
    TTS->>DB: Lưu AudioGuide record với URL âm thanh mới
    DB-->>TTS: Thành công
    TTS-->>Web: Thông báo sinh audio thành công
    Web-->>Admin: Hiển thị Audio Guide vừa tạo
```

## 8. Upload audio lên Cloudinary
Tích hợp dịch vụ lưu trữ file audio độc lập trên Cloudinary.

```mermaid
sequenceDiagram
    participant App as Hệ thống (Controller/Service)
    participant CloudStorage as CloudinaryStorageService
    participant Cloudinary as Cloudinary CDN
    
    App->>CloudStorage: Yêu cầu Upload(FileStream)
    CloudStorage->>Cloudinary: Gọi API Upload (lưu vào folder: audio/)
    Cloudinary-->>CloudStorage: Trả về PublicId & SecureUrl gốc
    CloudStorage->>CloudStorage: Transform URL (thêm tham số f_mp3)
    CloudStorage-->>App: Trả về URL đã được tối ưu hóa
```

## 9. Quy trình Change Request (POI Admin)
Luồng duyệt yêu cầu thay đổi từ phía POI Admin lên System Admin.

```mermaid
sequenceDiagram
    actor POI as POI Admin
    actor SysAdmin as System Admin
    participant Web as Web Portal
    participant ReqCtrl as ChangeRequest Controller
    participant DB as Database
    
    POI->>Web: Gửi yêu cầu thay đổi (Location/Audio)
    Web->>ReqCtrl: POST yêu cầu
    ReqCtrl->>DB: Lưu ChangeRequest (Status: Pending)
    DB-->>ReqCtrl: Thành công
    ReqCtrl-->>Web: Thông báo chờ duyệt cho POI Admin
    
    SysAdmin->>Web: Xem danh sách yêu cầu chờ duyệt
    Web->>ReqCtrl: Admin duyệt hoặc từ chối
    alt Từ chối
        ReqCtrl->>DB: Update Status = Rejected
    else Duyệt
        ReqCtrl->>DB: Update Status = Approved
        ReqCtrl->>DB: Gọi hàm TryApplyChangeSetAsync() áp dụng dữ liệu
    end
    DB-->>ReqCtrl: Lưu thành công
    ReqCtrl-->>Web: Cập nhật danh sách yêu cầu
```

## 10. Quản lý tài khoản POI (System Admin)
Xem, khóa, mở khóa người dùng và phân quyền quản lý POI.

```mermaid
sequenceDiagram
    actor Admin as System Admin
    participant Web as Web Admin
    participant UserCtrl as User Controller
    participant DB as Database
    
    Admin->>Web: Xem User, Block/Unblock, Gán/Gỡ POI Role
    Web->>UserCtrl: Yêu cầu thay đổi trạng thái user
    UserCtrl->>DB: Cập nhật bảng AuthUserAccounts (Status, Role, Assigned POIs)
    DB-->>UserCtrl: Cập nhật thành công
    UserCtrl-->>Web: Cập nhật trạng thái hiển thị
    Web-->>Admin: Thông báo thao tác thành công
```

## 11. Quản lý gói thanh toán
Thêm, sửa, xóa, quản lý trạng thái các gói dịch vụ và thống kê Subscription.

```mermaid
sequenceDiagram
    actor Admin
    participant Web as Web Admin
    participant PkgCtrl as PaymentPackage Controller
    participant DB as Database
    
    Admin->>Web: Tạo/Sửa Package (Giá, Thời hạn, Status)
    Web->>PkgCtrl: Submit thông tin Package
    PkgCtrl->>DB: CRUD bảng PaymentPackages
    DB-->>PkgCtrl: Xử lý xong
    PkgCtrl-->>Web: Cập nhật UI
    Web-->>Admin: Xem thống kê subscription
```

## 12. Báo cáo & Thống kê (Reports)
Tổng số lượng thực thể, phân bổ và top địa điểm phổ biến.

```mermaid
sequenceDiagram
    actor Admin
    participant Web as Web Admin
    participant RepCtrl as Report Controller
    participant DB as Database
    
    Admin->>Web: Vào trang Báo cáo thống kê
    Web->>RepCtrl: Yêu cầu dữ liệu báo cáo
    RepCtrl->>DB: Aggregate data (Total, Group by Category, Top Locations with most audio)
    DB-->>RepCtrl: Trả về Dataset thống kê
    RepCtrl-->>Web: Render Biểu đồ & Bảng
    Web-->>Admin: Hiển thị giao diện báo cáo
```

## 13. Lịch sử sử dụng (Usage History)
Ghi nhận và hiển thị lịch sử hoạt động của người dùng hệ thống/thiết bị.

```mermaid
sequenceDiagram
    actor Admin as System/POI Admin
    participant Web as Web Interface
    participant UsageCtrl as Usage History Controller
    participant DB as Database
    
    Admin->>Web: Yêu cầu xem Lịch sử sử dụng
    Web->>UsageCtrl: Get Log History
    UsageCtrl->>DB: Query bảng UserActivityLogs (Phát audio, Scan QR, v.v)
    DB-->>UsageCtrl: Danh sách Logs
    UsageCtrl-->>Web: Hiển thị danh sách Logs
    Web-->>Admin: Xem chi tiết hoạt động
```

## 14. API phục vụ Mobile
REST API cung cấp dữ liệu cho thiết bị di động.

```mermaid
sequenceDiagram
    participant Mobile as Mobile App (MAUI)
    participant API as Minimal API (/api/mobile/*)
    participant Service as Business Services
    participant DB as Database
    
    Mobile->>API: Gửi HTTP Request (GET Locations, Tours, Session, etc.)
    API->>Service: Gọi service xử lý logic
    Service->>DB: Truy xuất hoặc cập nhật dữ liệu
    DB-->>Service: Dữ liệu Entities
    Service-->>API: Chuyển đổi Entities -> DTOs
    API-->>Mobile: Trả về JSON Response (200 OK, 400, 401...)
```

## 15. Session & Heartbeat Management
Xác thực session, gửi heartbeat duy trì kết nối và ghi log hoạt động của thiết bị.

```mermaid
sequenceDiagram
    participant Mobile as Mobile App
    participant API as Session/Heartbeat API
    participant DB as Database
    
    Mobile->>API: Scan QR / Thanh toán thành công -> Nhận Session Token
    API->>DB: Khởi tạo Session và lưu vào bảng Subscriptions
    DB-->>API: Trả về Token
    API-->>Mobile: Lưu Local Session (Token + DeviceId)
    
    loop Mỗi chu kỳ định kỳ
        Mobile->>API: Gửi Request Heartbeat (kèm DeviceId, Token)
        API->>DB: Validate Token & Kiểm tra Hết hạn
        alt Session Hợp lệ
            DB->>DB: Cập nhật LastActiveTime & ghi log
            DB-->>API: Trạng thái Valid
            API-->>Mobile: 200 OK (Keep-alive)
        else Session Hết hạn hoặc Lỗi
            DB-->>API: Trạng thái Invalid
            API-->>Mobile: 401 Unauthorized
            Mobile->>Mobile: Xóa Local Session & Force Redirect về trang Intro
        end
    end
```

## 16. Dọn dẹp tài khoản hết hạn
Background job tự động dọn dẹp các session/tài khoản quá hạn sử dụng.

```mermaid
sequenceDiagram
    participant Cron as Background Job (Hosted Service)
    participant DB as Database
    
    loop Chạy theo lịch trình (VD: Mỗi giờ / Nửa đêm)
        Cron->>DB: Truy vấn bảng Sessions/Subscriptions đã vượt quá ExpireDate
        DB-->>Cron: Trả về Danh sách cần dọn dẹp
        Cron->>DB: Thực hiện Update Status = Expired / Hard Delete các record liên quan
        DB-->>Cron: Xác nhận cập nhật thành công
    end
```
