# VinhKhanhAudioGuide

PRD v1.0 - Full System Documentation  
Cập nhật: Tháng 4/2026

VinhKhanhAudioGuide (AudioLife) là hệ thống hướng dẫn âm thanh du lịch ẩm thực khu Vĩnh Khánh, Quận 4, TP.HCM.

## Tổng quan

Ngăn xếp công nghệ:
- Mobile: .NET MAUI 8, MVVM, DI, CommunityToolkit.Mvvm
- Web: ASP.NET Core 8 Razor Pages + Minimal API
- Dữ liệu: SQL Server + EF Core
- Lưu trữ audio: Cloudinary CDN
- TTS: Edge Neural Voices + Google TTS fallback
- Auth/RBAC: SystemAdmin, PoiAdmin

Chỉ số hệ thống:
- 17 trang mobile
- 15 viewmodels
- 13+ API endpoints
- 12 bảng dữ liệu
- 6 ngôn ngữ TTS
- 2 vai trò quản trị

## Trạng thái triển khai hiện tại (Tháng 4/2026)

Đã cập nhật theo codebase hiện tại:
- QR -> Payment -> Session đã chuyển sang xác thực DB theo deviceId/sessionToken
- Session mobile không tin local session snapshot để vào app
- Payment packages phải đồng bộ từ server DB, không fake-success local
- Audio guides được lấy online theo ngôn ngữ đã cache trên máy
- Geofencing đã chuẩn hóa Haversine + debounce/cooldown để tránh spam
- Splash system đang dùng Resources/Splash/splash.svg
- Settings page đã fix runtime crash do resource key sai

## Chương 1 - Kiến trúc hệ thống

Kiến trúc tổng thể:
- Mobile sử dụng MVVM + DI
- Web sử dụng Razor Pages cho admin/shop và Minimal API cho mobile
- Database code-first qua EF Core
- Audio lưu trên Cloudinary, URL transform f_mp3 on-the-fly

Tài liệu/ảnh tham chiếu:
- Class Diagram: dbdiagram.jpg
- ER Diagram: ERD.jpg

## Chương 2 - Mobile app (MVVM)

Các trang chính:
- IntroPage
- MainPage
- MapPage
- LocationDetailPage
- AudioPlayerPage
- ToursPage, TourDetailPage
- SearchPage
- FavoritesPage
- HistoryPage
- DownloadsPage
- ProfilePage, EditProfilePage
- SettingsPage, LanguageSection
- HelpPage, AboutPage

Runtime notes hiện tại:
- Pre-auth flow (Intro, QR, Payment, Language selection) mặc định tiếng Việt
- Ngôn ngữ đã chọn được lưu trên máy và dùng cho shell/audio sau khi session DB hợp lệ
- Khi session DB hết hạn, language preference bị xóa và app reset về vi cho pre-auth
- QR sample có sẵn để test onboarding

## Chương 3 - Audio playback

AudioService:
- Plugin.Maui.Audio
- Hỗ trợ play/pause/resume/stop/seek/volume
- Position timer 500ms
- Transcript sync theo AudioScriptSegment

State flow:
- None -> Loading -> Playing -> Paused -> Stopped -> Completed/Error

Bảo vệ runtime:
- Debounce/cooldown cho auto-play nearest POI
- Tránh phát trùng lặp khi geolocation event đến liên tục

## Chương 4 - Geolocation

GeolocationService:
- Tracking interval: 60s
- Accuracy: Medium
- Nearby radius: 100m (0.1km)
- Distance: Haversine (Earth radius 6371km)

Cập nhật hiện tại:
- Công thức distance đã được chuẩn hóa dùng utility chung
- Trigger auto-play có debounce/cooldown

## Chương 5 - Mobile API endpoints

| Endpoint | Method | Mô tả |
|---|---|---|
| /api/mobile/health | GET | Health check |
| /api/mobile/categories | GET | Categories + location count |
| /api/mobile/locations | GET | Tất cả locations + audio guides |
| /api/mobile/locations/{id} | GET | Chi tiết location + audio guides |
| /api/mobile/locations/by-category/{categoryId} | GET | Locations theo category |
| /api/mobile/locations/search?query= | GET | Tìm kiếm theo tên/mô tả/địa chỉ |
| /api/mobile/locations/nearby?lat=&lng=&radiusKm= | GET | Nearby theo Haversine |
| /api/mobile/tours | GET | Tất cả tours |
| /api/mobile/tours/{id} | GET | Chi tiết tour |
| /api/mobile/tours/featured | GET | Tours nổi bật |
| /api/mobile/audio/by-location/{locationId} | GET | Audio guides theo location |
| /api/mobile/audio/{id} | GET | Chi tiết audio guide |
| /api/mobile/payment/packages | GET | Payment packages từ DB |
| /api/mobile/session/by-device?deviceId= | GET | Check session theo device |
| /api/mobile/session/scan | POST | Lưu QR onboarding session vào DB |
| /api/mobile/payment/complete | POST | Complete payment và cập nhật session/subscription |
| /api/mobile/session/validate | GET | Validate session token + deviceId |

Lưu ý hiện tại:
- Các API liên quan session/payment không được fallback fake-success local
- Audio guide retrieval ưu tiên online DB theo ngôn ngữ đã lưu trên máy

## Chương 6 - TTS (Edge + Google fallback)

EdgeTextToSpeechService:
- Edge Neural voices: vi, en, zh, ja, ko, fr
- Nếu Edge 403 -> fallback Google TTS
- Text split chunks 180 chars
- Merge MP3 chunks, strip ID3 headers

Bảng mapping:

| Language | Edge voice | Google code |
|---|---|---|
| vi | vi-VN-HoaiMyNeural | vi |
| en | en-US-JennyNeural | en |
| zh | zh-CN-XiaoxiaoNeural | zh-CN |
| ja | ja-JP-NanamiNeural | ja |
| ko | ko-KR-SunHiNeural | ko |
| fr | fr-FR-DeniseNeural | fr |

## Chương 7 - Cloudinary audio storage

CloudinaryAudioStorageService:
- Upload audio stream/file
- Folder: audio/
- PublicId: prefix + GUID
- URL transform f_mp3 on-the-fly

Field lưu trữ:
- AudioUrl
- CloudinaryAudioUrl
- CloudinaryPublicId

## Chương 8 - Authentication và RBAC

Role:
- SystemAdmin: full access
- PoiAdmin: thao tác trên các locations được assign

Nguồn auth:
- appsettings.json
- AuthUserAccounts table

Policy:
- SystemAdminOnly
- PoiAdminOnly

## Chương 9 - POI Change Request workflow

Flow:
- PoiAdmin submit request
- Status: Pending -> InReview -> Approved/Rejected
- Nếu Approved: TryApplyChangeSetAsync tự động apply thay đổi

Target:
- Location fields
- AudioGuide fields, hỗ trợ create-audio-guide

## Chương 10 - SystemAdmin pages

- Admin/Index
- Admin/Users
- Admin/ChangeRequests
- Admin/Reports
- Admin/Settings
- Locations/*
- Categories/*
- Tours/*
- AudioGuides/*

## Chương 11 - PoiAdmin (Shop) portal

- Shop/Index
- Shop/Locations/Edit
- Shop/AudioGuides/*
- Shop/ChangeRequests
- Shop/Analytics
- Shop/Reviews

## Tổng kết - Tech stack và constants

Mobile:
- .NET MAUI 8
- CommunityToolkit.Mvvm
- Plugin.Maui.Audio
- SQLite-net
- Shell navigation

Web:
- ASP.NET Core 8 Razor Pages
- Minimal API
- EF Core + SQL Server
- Cookie authentication
- EdgeTTS
- CloudinaryDotNet

Infrastructure:
- SQL Server AudioDB
- Cloudinary CDN
- Edge TTS + Google fallback

Constants:

| Constant | Value | Location |
|---|---|---|
| Nearby Radius | 100m (0.1km) | GeolocationService.cs |
| Tracking Interval | 60s | GeolocationService.cs |
| Position Timer | 500ms | AudioService.cs |
| Cookie Expiry | 8h sliding | Program.cs |
| Database | AudioDB | appsettings.json |
| Cloudinary Folder | audio/ | appsettings.json |
| Google TTS Chunk | 180 chars | EdgeTTSService.cs |
| Audio Transform | f_mp3 | Cloudinary service |
| SystemAdmin role | SystemAdmin | RoleNames.cs |
| PoiAdmin role | PoiAdmin | RoleNames.cs |

## Roadmap

Security:
- JWT cho mobile API
- Password hashing (bcrypt/Argon2)
- Rate limiting
- HTTPS hardening

Features:
- Realtime listening analytics dashboard
- Push notifications khi assign POI
- Social sharing / QR tours
- Payment integration nâng cao

Infrastructure:
- Docker
- CI/CD (GitHub Actions)
- Azure deployment
- CDN caching optimization

---

VinhKhanhAudioGuide (AudioLife) - PRD v1.0  
.NET MAUI + ASP.NET Core 8 + SQL Server + Cloudinary + Edge TTS


sequenceDiagram
    participant Mobile as Mobile App
    participant Store as AppSessionStore
    participant HB as AppHeartbeatService
    participant API as MobileApiEndpoints
    participant DB as Database
    
    Mobile->>API: POST /api/mobile/payment/complete
    API->>DB: Create UserAppSession & Subscription
    DB-->>API: SessionToken
    API-->>Mobile: Lưu Local AppSessionSnapshot (Token, DeviceId)
    Mobile->>Store: SaveSnapshotAsync(Token, DeviceId)
    Mobile->>HB: StartAsync()
    
    loop Every 60s
        HB->>HB: SendHeartbeatAsync()
        HB->>API: POST /api/mobile/heartbeat (DeviceId, Token)
        API->>DB: Find active UserAppSession
        alt Session Valid
            API->>DB: Update LastSeen, Subscription.ValidationTime
            API-->>HB: 200 OK (Keep-alive)
        else Session Invalid/Expired
            API-->>HB: 401 Unauthorized
            HB->>Store: ClearSnapshotAsync()
            HB->>HB: StopAsync()
            HB->>Mobile: Event: SessionExpired
        end
    end
    note right of API
      Heartbeat ghi lại vị trí thiết bị qua AppHeartbeatService. GetOrCreateDeviceIdAsync() đảm bảo ID duy nhất.
    end note