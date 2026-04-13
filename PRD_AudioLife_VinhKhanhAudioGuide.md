VinhKhanhAudioGuide
-------------------

PRD v1.0 — Full System Documentation

PRD v1.0 — April 2026

VinhKhanhAudioGuide  
Hệ Thống Hướng Dẫn Âm Thanh
=================================================

Full-stack enterprise: .NET MAUI (MVVM + DI) — ASP.NET Core 8 Razor Pages + Minimal API — SQL Server + EF Core — Cloudinary CDN — Edge TTS + Google TTS fallback — POI Change Request Workflow — Role-based RBAC (SystemAdmin / PoiAdmin).

.NET MAUI 8 (MVVM)ASP.NET Core 8 Razor PagesEF Core + SQL ServerCloudinary CDNEdge TTS NeuralCookie Auth + RBACPOI Change RequestsPlugin.Maui.Audio

17

Mobile Pages

15

ViewModels

13+

API Endpoints

12

DB Tables

6

TTS Languages

2

Admin Roles

Giới Thiệu Sản Phẩm
-------------------

**VinhKhanhAudioGuide** (AudioLife) là hệ thống hướng dẫn âm thanh du lịch ẩm thực đường phố Vĩnh Khánh, Q4, TP.HCM. Kiến trúc enterprise với **MVVM pattern** trên mobile, **Razor Pages** trên web, **EF Core + SQL Server**, lưu trữ audio trên **Cloudinary CDN**, và TTS tự động bằng **Edge Neural Voices** với Google TTS fallback. Hệ thống có **2 portal riêng biệt**: SystemAdmin (quản lý toàn bộ) và PoiAdmin/Shop (chủ quán quản lý địa điểm của mình).

### Mục Tiêu

*   Audio guide tự động cho du khách khi đến gần địa điểm (geolocation 100m)
*   TTS đa ngôn ngữ: vi, en, zh, ja, ko, fr (Edge Neural + Google fallback)
*   Cloudinary CDN lưu trữ audio MP3 chuyên nghiệp
*   MVVM + Dependency Injection trên mobile
*   POI Change Request: PoiAdmin gửi yêu cầu -> SystemAdmin duyệt
*   Offline support: SQLite local database + audio download

### 2 Roles Quản Trị

*   **SystemAdmin:** Full access — CRUD Locations, Categories, Tours, AudioGuides, Users, Reports, Settings, Change Requests
*   **PoiAdmin (Shop):** Quản lý địa điểm được gán — sửa Location, tạo/sửa AudioGuide, gửi Change Request, xem Analytics, Reviews

### Tính Năng Nổi Bật

*   Plugin.Maui.Audio — audio player chuyên nghiệp (play/pause/seek/volume)
*   AudioScriptSegment — hiển thị transcript đồng bộ theo thời gian phát
*   Cloudinary upload + f\_mp3 on-the-fly transformation
*   Edge TTS với 6 Neural voices + Google TTS fallback khi 403
*   QR code scanning cho AppUser identification
*   Feedback/Rating system (1-5 stars) per location
*   Download audio offline với SQLite tracking

Chapter 01

Kiến Trúc Hệ Thống
------------------

**Kiến trúc:** Mobile dùng MVVM + DI (CommunityToolkit.Mvvm). Web dùng Razor Pages + Minimal API. Database: SQL Server + EF Core (code-first migrations). Audio storage: Cloudinary CDN với on-the-fly MP3 transformation. TTS: Edge Neural Voices (vi, en, zh, ja, ko, fr) với Google TTS fallback khi Edge trả về 403.

Class Diagram
-------------

![Class Diagram](dbdiagram.jpg)

ER Diagram — SQL Server
-----------------------

![ER Diagram](ERD.jpg)

Chapter 02

Mobile App — 17 Pages, MVVM Pattern
-----------------------------------

Kiến trúc **MVVM + Dependency Injection** với CommunityToolkit.Mvvm. Tất cả ViewModels dùng `[ObservableProperty]`, `[RelayCommand]`. Services được inject qua constructor. Navigation qua Shell routing.

### IntroPage

Onboarding giới thiệu app. Chuyển đến MainPage sau khi hoàn thành.

### MainPage

Trang chủ — danh sách locations, categories, featured tours. Search bar.

### MapPage

Bản đồ tương tác hiển thị locations. Geolocation tracking 100m radius.

### LocationDetailPage

Chi tiết địa điểm — hình ảnh, mô tả, audio guides, feedbacks. Favorite toggle.

### AudioPlayerPage

Player chuyên nghiệp: play/pause/seek/volume. Transcript hiển thị đồng bộ theo AudioScriptSegment.

### ToursPage + TourDetailPage

Danh sách tours + chi tiết tour với locations theo thứ tự SortOrder.

### SearchPage

Tìm kiếm locations theo tên, mô tả, địa chỉ.

### FavoritesPage

Danh sách địa điểm yêu thích (SQLite local).

### HistoryPage

Lịch sử nghe audio — ListeningHistory với listened seconds, completion.

### DownloadsPage

Audio đã tải về offline. Quản lý dung lượng. Xóa downloads.

### ProfilePage + EditProfilePage

Hồ sơ người dùng. Chỉnh sửa thông tin.

### SettingsPage + LanguageSection

Cài đặt app. Chọn ngôn ngữ (vi/en/zh/ja/ko/fr).

### HelpPage + AboutPage

Hướng dẫn sử dụng và thông tin ứng dụng.

Chapter 03

Audio Playback — Plugin.Maui.Audio
----------------------------------

AudioService dùng **Plugin.Maui.Audio** (IAudioManager) để phát MP3. Hỗ trợ play/pause/resume/stop/seek/volume. Position timer cập nhật mỗi 500ms. AudioScriptSegment hiển thị transcript đồng bộ theo thời gian.

Load URL/File→ IAudioPlayer.Play()→ Position Timer 500ms→ Transcript Sync→ PlaybackEnded

**Audio States:** None → Loading → Playing → Paused → Stopped → Completed → Error. Event-driven: StateChanged + PositionChanged callbacks.

Chapter 04

Geolocation — Nearby Detection
------------------------------

GeolocationService theo dõi vị trí mỗi 60 giây. Khi phát hiện location trong bán kính 100m (Haversine), phát sự kiện `NearbyLocationDetected`. Chỉ trigger 1 lần cho mỗi location (theo dõi `_lastNearestLocationId`).

### Tracking Config

*   Radius: 100m (0.1 km)
*   Interval: 60 seconds
*   Accuracy: GeolocationAccuracy.Medium
*   Timeout: 10 seconds per request

### Haversine Formula

Earth radius 6,371 km. Tính khoảng cách chính xác giữa 2 tọa độ GPS. Kết quả trả về km.

Chapter 05

Mobile API — Minimal API Endpoints
----------------------------------

Endpoint

Method

Mô Tả

`/api/mobile/health`

GET

Health check

`/api/mobile/categories`

GET

Tất cả categories + location count

`/api/mobile/locations`

GET

Tất cả locations + audio guides

`/api/mobile/locations/{id}`

GET

Chi tiết location + audio guides

`/api/mobile/locations/by-category/{categoryId}`

GET

Locations theo category

`/api/mobile/locations/search?query=`

GET

Tìm kiếm (Name, Description, Address)

`/api/mobile/locations/nearby?lat=&lng=&radiusKm=`

GET

Nearby (Haversine, default 100m)

`/api/mobile/tours`

GET

Tất cả tours + location IDs

`/api/mobile/tours/{id}`

GET

Chi tiết tour

`/api/mobile/tours/featured`

GET

Tours nổi bật (IsFeatured=true)

`/api/mobile/audio/by-location/{locationId}`

GET

Audio guides + script segments theo location

`/api/mobile/audio/{id}`

GET

Chi tiết audio guide + segments

Chapter 06

TTS Service — Edge Neural + Google Fallback
-------------------------------------------

`EdgeTextToSpeechService` dùng thư viện **EdgeTTS** (NuGet) để tạo audio MP3 từ text. 6 Neural voices. Khi Edge trả về 403, tự động fallback sang Google Translate TTS (free). Text dài được split thành chunks 180 ký tự, merge MP3 và strip ID3 headers.

Language

Edge Voice

Google Code

vi - Tiếng Việt

vi-VN-HoaiMyNeural

vi

en - English

en-US-JennyNeural

en

zh - Trung

zh-CN-XiaoxiaoNeural

zh-CN

ja - Nhật

ja-JP-NanamiNeural

ja

ko - Hàn

ko-KR-SunHiNeural

ko

fr - Pháp

fr-FR-DeniseNeural

fr

Text input→ Edge TTS Neural→ MP3 bytes→ Cloudinary upload→ CDN URL saved

**Fallback:** Edge 403 → Google Translate TTS (split text 180 chars, merge MP3 chunks, strip ID3 headers). Log error + retry.

Chapter 07

Cloudinary Audio Storage
------------------------

`CloudinaryAudioStorageService` upload audio lên Cloudinary CDN. Dùng `VideoUploadParams` (Cloudinary coi audio là video). URL cuối cùng được transform: `/video/upload/` → `/video/upload/f_mp3/` để on-the-fly convert sang MP3. Mỗi file có unique PublicId.

### Upload Flow

*   IFormFile hoặc Stream → Cloudinary API
*   Folder: `audio/`
*   PublicId: `{prefix}-{GUID}`
*   URL transform: f\_mp3 on-the-fly

### Stored Fields

*   `AudioUrl` — URL cuối cùng (f\_mp3)
*   `CloudinaryAudioUrl` — URL gốc Cloudinary
*   `CloudinaryPublicId` — ID để manage/delete

Authentication & RBAC
---------------------

Cookie Authentication với `CookieAuthenticationDefaults`. 2 roles: **SystemAdmin** và **PoiAdmin**. Auth source: `appsettings.json` (config users) + `AuthUserAccounts` table (DB users). PoiAdmin được gán location qua `PoiAdminLocationAssignments`. Authorization policies: `SystemAdminOnly`, `PoiAdminOnly`.

Role

Access

Folders

**SystemAdmin**

Full access: Admin, Categories, Locations, Tours, AudioGuides

/Admin/\*, /Categories/\*, /Locations/\*, /Tours/\*, /AudioGuides/\*

**PoiAdmin**

Địa điểm được gán: sửa Location, tạo/sửa AudioGuide, gửi ChangeRequest

/Shop/\*

**PoiAdmin Assignment:** Mỗi PoiAdmin được gán một số LocationIds. UserAccessService kiểm tra `CanAccessLocation()` trước mỗi thao tác. SystemAdmin có thể chuyển/gán lại locations giữa các PoiAdmin.

POI Change Request Workflow
---------------------------

`PoiChangeRequestService` quản lý quy trình: PoiAdmin gửi yêu cầu thay đổi (Location hoặc AudioGuide) → SystemAdmin duyệt (Approved/Rejected). Khi Approved, hệ thống tự động `TryApplyChangeSetAsync()` — parse ChangeSetJson và apply từng field vào entity.

PoiAdmin→ Submit Request→ Pending→ SystemAdmin Review→ Approved→ Auto Apply ChangeSet

### TargetType

*   `Location` — sửa Name, Description, Address, ImageUrl, Lat/Lng, Duration
*   `AudioGuide` — sửa Title, Description, AudioUrl, Cloudinary fields, Language, Transcript. Hỗ trợ `create-audio-guide` action.

### Status Flow

*   `Pending` → `InReview` → `Approved` / `Rejected`
*   ChangeSetJson lưu JSON diff của các fields thay đổi
*   ReviewNote: ghi chú của admin khi duyệt/từ chối

Chapter 10

SystemAdmin — Razor Pages
-------------------------

### Admin/Index

Dashboard tổng quan: stats, recent activities.

### Admin/Users

CRUD AuthUserAccounts. Gán role, assign locations cho PoiAdmin.

### Admin/ChangeRequests

Duyệt/từ chối POI Change Requests. Xem diff ChangeSetJson.

### Admin/Reports

Báo cáo thống kê: listening, feedback, locations.

### Admin/Settings

Cấu hình hệ thống.

### Locations/\*

CRUD Locations + MapView (bản đồ tổng quan).

### Categories/\*

CRUD Categories (Quán ăn, WC, Bãi xe...).

### Tours/\*

CRUD Tours + gán TourLocations với SortOrder.

### AudioGuides/\*

CRUD AudioGuides + TTS generation + Cloudinary upload.

Chapter 11

PoiAdmin (Shop) Portal
----------------------

### Shop/Index

Dashboard của PoiAdmin — chỉ hiện locations được gán.

### Shop/Locations/Edit

Sửa thông tin location (chỉ những địa điểm được assign).

### Shop/AudioGuides/\*

CRUD audio guides cho locations của mình. TTS generate + Cloudinary upload.

### Shop/ChangeRequests

Gửi yêu cầu thay đổi. Xem trạng thái pending/approved/rejected.

### Shop/Analytics

Thống kê nghe audio cho locations của mình.

### Shop/Reviews

Xem feedback/reviews của du khách cho locations.

Summary

Tech Stack & Constants
----------------------

### Mobile

*   .NET MAUI 8 (MVVM)
*   CommunityToolkit.Mvvm
*   Plugin.Maui.Audio
*   SQLite-net (local DB)
*   Shell navigation

### Web Backend

*   ASP.NET Core 8 Razor Pages
*   Minimal API (/api/mobile/\*)
*   EF Core + SQL Server
*   Cookie Authentication
*   EdgeTTS (NuGet)
*   CloudinaryDotNet

### Infrastructure

*   SQL Server (AudioDB)
*   Cloudinary CDN (audio)
*   Edge Neural Voices (6 langs)
*   Google TTS (fallback)
*   Bootstrap 5 (admin UI)

Constant

Value

Location

Nearby Radius

100m (0.1 km)

GeolocationService.cs

Tracking Interval

60 seconds

GeolocationService.cs

Position Timer

500ms

AudioService.cs

Cookie Expiry

8 hours (sliding)

Program.cs

Database

AudioDB (SQL Server)

appsettings.json

Cloudinary Folder

audio/

appsettings.json

Google TTS chunk

180 chars max

EdgeTTSService.cs

Audio Transform

f\_mp3 (on-the-fly)

CloudinaryService.cs

SystemAdmin role

SystemAdmin

RoleNames.cs

PoiAdmin role

PoiAdmin

RoleNames.cs

Future

Roadmap
-------

### Security

*   JWT tokens thay Cookie cho mobile API
*   Password hashing (bcrypt/Argon2)
*   API rate limiting
*   HTTPS enforcement

### Features

*   Realtime listening analytics dashboard
*   Push notifications khi gán POI
*   Social sharing / QR code tours
*   Multi-language UI (mobile app)
*   Payment integration cho tours có phí

### Infrastructure

*   Docker containerization
*   CI/CD pipeline (GitHub Actions)
*   Azure deployment
*   CDN caching optimization

**Đã hoàn thành:** Full-stack enterprise với MVVM mobile (17 pages, 15 VMs), Razor Pages admin (2 portals), EF Core + SQL Server (12 tables), Cloudinary CDN, Edge TTS 6 languages + Google fallback, POI Change Request workflow, Role-based RBAC, AudioScriptSegment transcript sync, offline downloads, feedback/rating system.

**VinhKhanhAudioGuide (AudioLife)** — PRD v1.0

.NET MAUI (MVVM) + ASP.NET Core 8 Razor Pages + SQL Server + Cloudinary + Edge TTS