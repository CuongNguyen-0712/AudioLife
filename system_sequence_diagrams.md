# VinhKhanhAudioGuide - System Sequence Diagrams

Dưới đây là các Sequence Diagram (Biểu đồ tuần tự) cập nhật theo kiến trúc và chức năng hiện tại của dự án AudioLife (VinhKhanhAudioGuide).

## 1. Authentication (Web Admin)
Xử lý đăng nhập phân quyền cho System Admin và POI Admin trên giao diện Web.

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

## 2. Mobile Onboarding & Payment (Kích hoạt thiết bị)
Quy trình từ lúc Mobile app quét QR/chọn gói đến khi nhận được JWT Access Token.

```mermaid
sequenceDiagram
    participant Mobile as Mobile App
    participant API as Mobile API (/payment)
    participant JWT as JWT Token Service
    participant DB as Database
    
    Mobile->>API: GET /payment/packages
    API->>DB: Query active packages
    DB-->>API: List packages
    API-->>Mobile: Trả về danh sách gói cước
    
    Note over Mobile, API: Người dùng thanh toán qua cổng ngoài (VNPay/Momo/Store)
    
    Mobile->>API: POST /payment/complete (DeviceId, PackageId, PaymentRef)
    API->>DB: Tìm/Tạo AppUser (theo DeviceId)
    API->>DB: Tạo UserSubscription & UserAppSession
    JWT->>JWT: Generate JWT AccessToken (UserId, DeviceId)
    DB-->>API: Lưu thành công
    API-->>Mobile: Trả về SessionToken, AccessToken, RefreshToken, ExpireDate
```

## 3. Session Validation & Refresh (Mobile)
Kiểm tra và làm mới phiên làm việc của thiết bị di động.

```mermaid
sequenceDiagram
    participant Mobile as Mobile App
    participant API as Mobile API (/session)
    participant JWT as JWT Token Service
    participant DB as Database

    alt Kiểm tra session khi khởi động
        Mobile->>API: GET /session/validate (SessionToken, DeviceId)
        API->>DB: Kiểm tra Session & Subscription còn hạn?
        alt Hợp lệ
            JWT->>JWT: Generate new AccessToken
            API-->>Mobile: 200 OK (AccessToken, IsValid=true)
        else Hết hạn/Không tồn tại
            API-->>Mobile: 200 OK (IsValid=false, Message)
            Mobile->>Mobile: Force Logout / Redirect to Onboarding
        end
    end

    alt Làm mới Token (Refresh)
        Mobile->>API: POST /session/refresh (RefreshToken, DeviceId)
        API->>DB: Kiểm tra RefreshToken & Session
        API->>DB: Rotate RefreshToken (Tạo token mới)
        JWT->>JWT: Generate new AccessToken
        DB-->>API: Lưu thay đổi
        API-->>Mobile: Trả về AccessToken & RefreshToken mới
    end
```

## 4. Heartbeat & Activity Logging (Mobile)
Duy trì session và ghi nhận hoạt động người dùng theo thời gian thực.

```mermaid
sequenceDiagram
    participant Mobile as Mobile App
    participant API as Mobile API (/heartbeat)
    participant DB as Database
    
    loop Định kỳ (mỗi 1-5 phút)
        Mobile->>API: POST /heartbeat (SessionToken, DeviceId, Activity, Route, IsForeground)
        API->>DB: Validate Session
        alt Session Valid
            API->>DB: Cập nhật LastSeenAtUtc & CurrentActivity
            API->>DB: Ghi log vào AppUserActivityLogs
            API->>DB: Prolong Session Expiry (nếu < 30p)
            DB-->>API: Success
            API-->>Mobile: 200 OK (Keep-alive)
        else Session Invalid
            API-->>Mobile: 401 Unauthorized / Session Invalid
            Mobile->>Mobile: Stop Background Services & Redirect
        end
    end
```

## 5. Catalog Management (Locations, Tours, Audio)
Luồng lấy dữ liệu danh mục cho Mobile App với cơ chế Cache.

```mermaid
sequenceDiagram
    participant Mobile as Mobile App
    participant API as Mobile API
    participant Cache as MemoryCache
    participant DB as Database
    
    Mobile->>API: GET /locations (language)
    API->>Cache: TryGet(CacheKey)
    alt Cache Miss
        API->>DB: Query Locations + Categories + Reviews
        API->>Cache: Set(CacheKey, data, 5 mins)
    end
    API-->>Mobile: Trả về danh sách Locations (DTO)
    
    Note right of Mobile: Tương tự cho /categories, /tours, /audio/by-location
```

## 6. Listening History (Lịch sử nghe)
Ghi nhận tiến trình nghe audio của người dùng.

```mermaid
sequenceDiagram
    participant Mobile as Mobile App
    participant API as Mobile API (/history)
    participant DB as Database
    
    Mobile->>API: POST /history (AudioGuideId, LocationId, Progress, ListenedSeconds)
    Note over API: Sử dụng [Authorize] JWT Claim để lấy UserId
    API->>DB: Find/Create ListeningHistory (UserId, AudioGuideId)
    API->>DB: Cập nhật Progress, Duration, IsCompleted, LastListenedAtUtc
    DB-->>API: Lưu thành công
    API-->>Mobile: Trả về ListeningHistory DTO hiện tại
```

## 7. Location Reviews (Đánh giá địa điểm)
Người dùng gửi đánh giá và Admin kiểm duyệt.

```mermaid
sequenceDiagram
    actor User as Mobile User
    participant API as Mobile API (/reviews)
    participant DB as Database
    actor Admin as System Admin
    participant Web as Web Admin

    User->>API: POST /reviews (LocationId, Rating, Comment)
    API->>DB: Lưu LocationReview (Status: Pending)
    API-->>User: Thông báo chờ duyệt
    
    Admin->>Web: Truy cập trang quản lý đánh giá
    Web->>DB: Query Pending Reviews
    Admin->>Web: Phê duyệt (Approve)
    Web->>DB: Update Status = Approved
    Note right of DB: Review sẽ hiển thị trên Mobile từ thời điểm này
```

## 8. Text-to-Speech (TTS) Workflow
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
    alt Nếu Edge API Lỗi (403/Timeout/Unsupported)
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

## 10. Background Session & Subscription Cleanup
Tự động dọn dẹp các session và gói cước hết hạn (SessionCleanupBackgroundService).

```mermaid
sequenceDiagram
    participant Cron as Background Service
    participant DB as Database
    
    loop Mỗi giờ
        Cron->>DB: Query UserAppSessions (Expired OR Revoked) AND Active
        DB-->>Cron: Danh sách session quá hạn
        Cron->>DB: Set IsActive = false
        
        Cron->>DB: Query UserSubscriptions (Active) AND Expired
        DB-->>Cron: Danh sách gói cước hết hạn
        Cron->>DB: Set Status = 'Expired'
        
        Cron->>DB: SaveChangesAsync()
    end
```

## 11. Dashboard & Reports (Web Admin)
Thống kê số liệu hệ thống dành cho Admin.

```mermaid
sequenceDiagram
    actor Admin
    participant Web as Web Dashboard
    participant API as Dashboard/Report Controller
    participant DB as Database
    
    Admin->>Web: Truy cập Dashboard/Reports
    Web->>API: Yêu cầu lấy dữ liệu thống kê
    API->>DB: Aggregate (Locations, Audio, Tours, Subscriptions, History)
    DB-->>API: Trả về kết quả
    API-->>Web: Render Charts & Tables
    Web-->>Admin: Hiển thị thông tin tổng quan
```
