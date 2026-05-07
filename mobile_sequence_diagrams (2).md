# VinhKhanhAudioGuide - Mobile Sequence Diagrams

Tài liệu này định nghĩa 15 Sequence Diagram chuyên biệt cho từng chức năng trên thiết bị di động (Mobile App).

## 1. Phát thuyết minh audio tự động theo vị trí (Auto-Playback)
Quy trình gọi tự động kích hoạt audio khi đã xác định được điểm phù hợp từ hàng đợi.

```mermaid
sequenceDiagram
    participant Queue as QueueManager
    participant Audio as AudioService
    participant Player as UI/AudioPlayer
    
    Queue->>Queue: Lấy POI ưu tiên cao nhất trong hàng đợi
    Queue->>Audio: Yêu cầu Auto-Play(AudioId, LocationId)
    Audio->>Audio: Kích hoạt tải & phát luồng Audio
    Audio-->>Player: Thông báo bắt đầu phát (Mở Player dạng Modal/Mini)
    Player-->>User: Nghe thấy audio & hiển thị UI tương ứng
```

## 2. Trình phát audio (Audio Player)
Giao tiếp giữa người dùng và bộ điều khiển Audio (Play, Pause, Seek).

```mermaid
sequenceDiagram
    actor User
    participant UI as AudioPlayerPage
    participant Audio as AudioService
    
    User->>UI: Bấm Play/Pause
    UI->>Audio: Play() / Pause()
    Audio->>Audio: Thay đổi trạng thái
    Audio-->>UI: Cập nhật Icon Play/Pause
    
    User->>UI: Kéo thanh tiến trình (Seek)
    UI->>Audio: SeekTo(Position)
    Audio->>Audio: Thay đổi mốc thời gian
    
    loop Cập nhật UI (Timer 500ms)
        Audio-->>UI: Trả về Position hiện tại & Cập nhật UI (Progress Bar/Transcript)
    end
```

## 3. Quản lý lịch sử nghe
Ghi nhận quá trình nghe của người dùng sau khi hoàn tất hoặc dừng audio.

```mermaid
sequenceDiagram
    participant Audio as AudioService
    participant History as HistoryService
    participant DB as Local SQLite
    participant API as Mobile API
    
    Audio->>History: Sự kiện: Audio kết thúc hoặc dừng (LocationId, AudioId, Duration)
    History->>DB: Insert/Update vào bảng ListenHistory
    DB-->>History: Xác nhận lưu trữ local
    History->>API: POST /api/mobile/history (Đồng bộ lên Server qua Background)
    API-->>History: 200 OK
```

## 4. Xem chi tiết điểm tham quan (POI)
Kiểm tra dữ liệu local trước khi lấy dữ liệu qua mạng.

```mermaid
sequenceDiagram
    actor User
    participant View as LocationDetailPage
    participant API as Mobile API
    participant Cache as Local Storage
    
    User->>View: Bấm chọn một POI
    View->>Cache: Kiểm tra dữ liệu POI
    alt Có trong cache & chưa hết hạn
        Cache-->>View: Trả về POI Data local
    else Chưa có cache hoặc đã cũ
        View->>API: GET /api/mobile/locations/{id}
        API-->>View: Trả về chi tiết POI + D/s Audio
        View->>Cache: Lưu lại dữ liệu (Cache)
    end
    View-->>User: Hiển thị giao diện chi tiết & danh sách Audio
```

## 5. Định vị GPS & phát hiện điểm gần nhất
Theo dõi vị trí thiết bị và lấy dữ liệu các POI nằm trong bán kính cho phép.

```mermaid
sequenceDiagram
    participant Device as GPS Hardware
    participant GeoService as GeolocationService
    participant API as Mobile API
    
    loop Chu kỳ quét (VD: 60s)
        GeoService->>Device: Yêu cầu tọa độ hiện tại
        Device-->>GeoService: Trả về Lat, Lng
        GeoService->>API: GET /api/mobile/locations/nearby?lat=...&lng=...&radius=0.1
        API-->>GeoService: Trả về danh sách POI nằm trong bán kính (Nearby)
        GeoService->>GeoService: Tính toán khoảng cách chính xác bằng công thức Haversine
    end
```

## 6. Quản lý hàng đợi
Đẩy các POI nhận được từ tính năng định vị vào một hàng chờ để xử lý tuần tự.

```mermaid
sequenceDiagram
    participant Geo as GeolocationService
    participant Queue as QueueManager
    
    Geo->>Queue: Truyền vào danh sách POI gần nhất vừa lấy được
    Queue->>Queue: Dọn dẹp các POI cũ đã nằm ngoài bán kính (Exit geofence)
    Queue->>Queue: Thêm các POI mới vào hàng đợi hệ thống
    Queue->>Queue: Phân loại & Cập nhật thứ tự ưu tiên
    Queue-->>Geo: Phản hồi trạng thái hàng đợi đã sẵn sàng
```

## 7. Xử lý trùng (chia rõ các trường hợp)
Các lớp bảo vệ nhằm tránh việc spam phát audio hoặc lặp lại cùng một địa điểm liên tục.

```mermaid
sequenceDiagram
    participant Queue as QueueManager
    participant Cache as PlayedHistoryCache
    
    Queue->>Queue: Đánh giá một POI trong hàng đợi
    Queue->>Cache: Kiểm tra tính hợp lệ
    alt Trường hợp 1: Debounce (Chống spam)
        Cache-->>Queue: Đã quét qua POI này trong vòng 30s qua
        Queue->>Queue: Bỏ qua (Drop) để tránh trigger liên tục
    else Trường hợp 2: Đã phát hoàn toàn (Played)
        Cache-->>Queue: Đã nghe trọn vẹn POI này trong 24h qua
        Queue->>Queue: Xóa khỏi hàng đợi Auto-play (User vẫn có thể bấm tay)
    else Trường hợp 3: Hợp lệ
        Cache-->>Queue: POI mới hoặc đã qua thời gian Cooldown
        Queue->>Queue: Đưa vào danh sách được phép Auto-play
    end
```

## 8. Ưu tiên POI theo scoring
Thuật toán tính điểm (Scoring) để chọn ra POI nào sẽ được phát audio trước khi có nhiều điểm ở gần nhau.

```mermaid
sequenceDiagram
    participant Queue as QueueManager
    
    Queue->>Queue: Nhận danh sách các POI hợp lệ trong hàng đợi
    loop Tính điểm từng POI
        Queue->>Queue: BaseScore (Mặc định)
        Queue->>Queue: + Khoảng cách thực (Gần hơn -> Điểm cao hơn)
        Queue->>Queue: x Multiplier (Nhân hệ số nếu là POI Nổi bật/Featured)
    end
    Queue->>Queue: Sắp xếp danh sách giảm dần theo Tổng Điểm (DESC)
    Queue->>Queue: Chọn POI có điểm cao nhất để kích hoạt phát audio
```

## 9. Tìm kiếm địa điểm & tour (Search)
Tìm kiếm tức thời (với Debounce) để giảm tải request.

```mermaid
sequenceDiagram
    actor User
    participant View as SearchPage
    participant API as Mobile API
    
    User->>View: Nhập từ khóa tìm kiếm
    View->>View: Timer: Chờ 300ms không gõ phím (Debounce)
    View->>API: GET /api/mobile/locations/search?query={keyword}
    API-->>View: Trả về danh sách kết quả
    View-->>User: Hiển thị danh sách kết quả (Locations, Tours)
```

## 10. Xem chi tiết tour
Hiển thị mô tả và lộ trình các điểm dừng.

```mermaid
sequenceDiagram
    actor User
    participant View as TourDetailPage
    participant API as Mobile API
    
    User->>View: Bấm vào xem một Tour
    View->>API: GET /api/mobile/tours/{id}
    API-->>View: Trả về thông tin tổng quan & List<Stops>
    View-->>User: Hiển thị bản đồ lộ trình & thứ tự các điểm dừng (Stops)
```

## 11. Quản lý tiến trình tour
Lưu trạng thái và giúp người dùng tiếp tục tour từ điểm đến hiện tại.

```mermaid
sequenceDiagram
    actor User
    participant View as TourDetailPage
    participant DB as SQLite (Local Progress)
    
    User->>View: Mở lại một Tour đang đi dở
    View->>DB: Truy vấn Progress(TourId)
    DB-->>View: Trả về: Đã hoàn thành đến Checkpoint #3
    View-->>User: Hiển thị UI: Highlight điểm đến tiếp theo (Stop #4)
    
    User->>View: Nhấn hoàn thành Stop #4
    View->>DB: Cập nhật Checkpoint(TourId, StopId=4)
    DB-->>View: Lưu thành công
    View-->>User: Cập nhật lại thanh tiến trình (Progress Bar)
```

## 12. Quét QR, thanh toán
Hai luồng vào duy nhất để lấy được quyền truy cập app.

```mermaid
sequenceDiagram
    actor User
    participant UI as Onboarding
    participant Store as AppStore / CH Play
    participant API as Mobile API (/payment & /session)
    
    alt Luồng 1: Quét QR (Đại lý)
        User->>UI: Chọn Quét QR Đại lý
        UI->>API: POST /api/mobile/session/scan (Kèm QRCode)
        API-->>UI: Trả về SessionToken
    else Luồng 2: Thanh toán Online
        User->>UI: Chọn Mua Gói In-App
        UI->>Store: Request Payment
        Store-->>UI: Trả về Payment Receipt
        UI->>API: POST /api/mobile/payment/complete (Gửi Receipt)
        API-->>UI: Verify Receipt & Trả về SessionToken
    end
    
    UI->>UI: Lưu SessionToken & DeviceId vào Local Secure Storage
```

## 13. Quản lý session thiết bị (lưu/kiểm tra token)
Cơ chế kiểm tra quyền truy cập mỗi khi app khởi động.

```mermaid
sequenceDiagram
    participant App as App Lifecycle
    participant Storage as SecureStorage
    participant API as Mobile API
    
    App->>App: Mở App (OnStart)
    App->>Storage: Lấy SessionToken & DeviceId
    alt Token Null hoặc Trống
        Storage-->>App: None
        App->>App: Điều hướng đến trang Intro / Payment
    else Có Token
        Storage-->>App: TokenData
        App->>API: GET /api/mobile/session/validate?token={Token}
        API-->>App: Trạng thái (Hợp lệ / Lỗi / Hết hạn)
        alt Hợp lệ
            App->>App: Cho phép truy cập vào Trang chủ (Shell)
        else Không hợp lệ / Hết hạn
            App->>Storage: Xóa Token hiện tại
            App->>App: Force điều hướng về trang Intro
        end
    end
```

## 14. Heartbeat định kỳ (duy trì session)
Cập nhật trạng thái 'đang hoạt động' của người dùng lên server và thu hồi quyền ngay khi hết hạn.

```mermaid
sequenceDiagram
    participant App as Background Service
    participant API as Session API
    
    loop Mỗi chu kỳ (VD: 60 giây)
        App->>API: Gọi Heartbeat GET /api/mobile/session/heartbeat (DeviceId, Token)
        API->>API: Cập nhật trường LastActiveTime trong DB
        API-->>App: Trả về HTTP 200 OK (Keep Alive) hoặc HTTP 401 (Hết hạn)
        
        alt Nhận mã 401 Unauthorized
            App->>App: Bắn Event SessionExpired
            App->>App: Xóa dữ liệu & Buộc người dùng thoát về màn hình đăng nhập
        end
    end
```

## 15. Đa ngôn ngữ (i18n)
Dịch thuật giao diện ngay lập tức khi người dùng thay đổi tùy chọn ngôn ngữ.

```mermaid
sequenceDiagram
    actor User
    participant UI as Views/Pages
    participant Resx as LocalizationResources
    participant Pref as Language Preferences
    
    User->>Pref: Thay đổi ngôn ngữ (VD: English)
    Pref->>Pref: Lưu Setting vào LocalStorage ("en-US")
    Pref->>UI: Phát tín hiệu CultureChanged ra toàn App
    
    loop Mọi thành phần UI đang mở
        UI->>Resx: Yêu cầu lấy văn bản theo Resource Key
        Resx-->>UI: Trả về text bằng ngôn ngữ mới (Tiếng Anh)
        UI->>UI: Cập nhật ngay lập tức nội dung hiển thị (Tiêu đề, Mô tả...)
    end
```
