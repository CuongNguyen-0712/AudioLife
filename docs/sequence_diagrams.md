# Sequence Diagrams — AudioLife (VinhKhanhAudioGuide)

> [!NOTE]
> Tài liệu cập nhật theo code mới nhất (04/2026). Bao gồm **11 sequence diagrams** cho toàn bộ chức năng chính, phản ánh các thay đổi: `AutoPlaybackService` (scoring system, queue, TH1-TH4), `GeolocationService` (tracking 5s, NearbyLocationCandidate), `ExpiredAccountCleanupService`, `PaymentPackageService` (CRUD + dashboard), và `AudioService` (PlayAsync với locationId/audioGuideId).

---

## 1. Khởi động App & Kiểm tra Session (Offline-first)

App hỗ trợ **offline-first**: nếu offline và có snapshot → vào app luôn. Nếu online → verify với server.

```mermaid
sequenceDiagram
    actor User
    participant App as App.xaml.cs
    participant Loading as StartupLoadingPage
    participant Store as AppSessionStore
    participant Net as Connectivity
    participant API as RemoteApiService
    participant Server as Web API
    participant DB as SQL Server

    User->>App: Mở app
    App->>Loading: Hiển thị StartupLoadingPage
    Loading->>App: InitializeStartupAsync()
    App->>Store: GetOrCreateDeviceIdAsync()
    Store-->>App: deviceId
    App->>Store: GetSnapshotAsync()
    Store-->>App: localSnapshot (or null)
    App->>Net: Check NetworkAccess

    alt Offline + có snapshot
        App->>App: NavigateToShellRoot()
        App->>App: StartAutoPlaybackAsync()
        App->>User: Vào AppShell (offline mode)
    else Offline + không có snapshot
        App->>App: NavigateToShellRoot() (fallback local data)
        App->>User: Vào AppShell (guest/offline)
    else Online
        alt Có valid local session → vào app trước
            App->>App: NavigateToShellRoot()
            App->>App: StartAutoPlaybackAsync()
        end

        App->>API: CheckDeviceSessionAsync(deviceId)
        API->>Server: GET /api/mobile/session/by-device?deviceId=
        Server->>DB: Query UserAppSessions + UserSubscriptions
        DB-->>Server: session data
        Server-->>API: DeviceSessionCheckResult
        API-->>App: check

        alt Không có session trên server
            App->>Store: ClearSnapshotAsync()
            App->>App: StopHeartbeatAsync()
            App->>User: NavigateToIntro()
        else Có session → validate
            App->>API: ValidateSessionAsync(token, deviceId)
            API->>Server: GET /api/mobile/session/validate
            Server->>DB: Check session + active subscription
            Server-->>API: SessionValidationResult

            alt Invalid
                App->>Store: ClearSnapshotAsync()
                App->>User: NavigateToIntro()
            else Valid
                App->>Store: SaveSnapshotAsync(newSnapshot)
                Note over App: Nếu chưa vào shell → NavigateToShellRoot()
                App->>App: StartHeartbeatAsync()
                App->>User: AppShell (verified)
            end
        end
    end
```

---

## 2. QR Onboarding → Payment → Language → App

```mermaid
sequenceDiagram
    actor User
    participant Intro as IntroPage
    participant QR as QrScannerPage
    participant QRSvc as QrCodePayloadService
    participant App as App.xaml.cs
    participant PaySel as PaymentPlanSelectionPage
    participant PayOut as PaymentCheckoutPage
    participant API as RemoteApiService
    participant Server as Web API
    participant DB as SQL Server
    participant Lang as LanguageSection

    User->>Intro: Nhấn "Bắt đầu"
    Intro->>App: NavigateToQrScanner()
    App->>QR: Hiển thị QrScannerPage

    User->>QR: Quét mã QR
    QR->>QRSvc: TryParseAudioPayload(rawValue)
    QRSvc-->>QR: QrAudioPayload(locationId, audioGuideId, ...)

    QR->>App: CompleteQrOnboardingAsync(payload)
    App->>App: StorePendingQrPayload()
    App->>PaySel: Hiển thị PaymentPlanSelectionPage

    PaySel->>API: GetPaymentPackagesAsync()
    API->>Server: GET /api/mobile/payment/packages
    Server->>DB: Query PaymentPackages (Active, OrderBy Price)
    Server-->>API: List<PaymentPackage>
    API-->>PaySel: packages
    PaySel->>User: Hiển thị danh sách gói

    User->>PaySel: Chọn gói
    PaySel->>PayOut: Navigate to PaymentCheckoutPage

    User->>PayOut: Xác nhận thanh toán
    PayOut->>API: CompletePaymentAsync(request)
    API->>Server: POST /api/mobile/payment/complete
    Server->>DB: FindOrCreate AppUser (QrToken)
    Server->>DB: FindOrCreate UserSubscription (packageId)
    Server->>DB: FindOrCreate UserAppSession (deviceId)
    Server->>DB: SaveChangesAsync()
    Server-->>API: {sessionToken, expiresAt, success}
    API-->>PayOut: result

    PayOut->>App: NavigateToLanguageSelection()
    App->>Lang: Hiển thị LanguageSection

    User->>Lang: Chọn ngôn ngữ (vi/en/zh/ja/ko/fr)
    Lang->>Lang: PersistCulture()
    Lang->>App: NavigateToShellRoot()
    App->>App: StartHeartbeatAsync()
    App->>App: StartAutoPlaybackAsync()
    App->>User: AppShell (MainPage)
```

---

## 3. Duyệt & Tìm kiếm POI (với Cache + Localization)

```mermaid
sequenceDiagram
    actor User
    participant VM as MainViewModel
    participant API as RemoteApiService
    participant Cache as LocalDatabaseService
    participant L10n as ContentLocalizationMapper
    participant Fallback as ApiService (local)
    participant Server as Web API
    participant DB as SQL Server

    User->>VM: Mở MainPage / Pull-to-refresh
    VM->>API: GetLocationsAsync()
    API->>Server: GET /api/mobile/locations?language=vi

    alt Server OK
        Server->>DB: Locations + Category + AudioGuides
        Server->>Server: ResolveLanguageSelection()
        Server-->>API: List<LocationDto>
        API->>Cache: UpsertCacheAsync("catalog.locations")
        API->>L10n: LocalizeLocations(data, "vi")
        L10n-->>API: localized
        API-->>VM: locations
    else Server unavailable
        API->>Cache: GetCachedJsonAsync("catalog.locations")
        alt Cache hit
            Cache-->>API: JSON → locations
            API->>L10n: LocalizeLocations()
            API-->>VM: cached locations
        else Cache miss
            API->>Fallback: GetLocationsAsync()
            Fallback-->>VM: local seed data
        end
    end

    VM->>User: Hiển thị danh sách (Featured + Popular)

    Note over User,VM: Tìm kiếm
    User->>VM: Nhập query
    VM->>API: SearchLocationsAsync(query)
    API->>Server: GET /api/mobile/locations/search?query=&language=
    Server->>DB: LIKE Name/Description/Address
    Server-->>API: results
    API-->>VM: filtered locations
    VM->>User: Hiển thị kết quả
```

---

## 4. Auto-Playback System (TH1 → TH4)

Service mới `AutoPlaybackService` xử lý 4 tình huống phức tạp dựa trên scoring system.

```mermaid
sequenceDiagram
    actor User
    participant GeoSvc as GeolocationService
    participant GPS as Device GPS
    participant API as RemoteApiService
    participant Auto as AutoPlaybackService
    participant Audio as AudioService
    participant CDN as Cloudinary

    Note over GeoSvc: Tracking mỗi 5 giây

    loop Mỗi 5s
        GeoSvc->>GPS: GetLocationAsync(Medium, 10s)
        GPS-->>GeoSvc: (lat, lng, accuracy)
        GeoSvc->>API: GetNearbyLocationsAsync(lat, lng, radius)
        API-->>GeoSvc: List<Location>
        GeoSvc->>GeoSvc: Tạo List<NearbyLocationCandidate>
        GeoSvc-->>Auto: NearbyLocationDetected(candidates)
    end

    Note over Auto: TH1: User đi vào bán kính 5m của POI

    Auto->>Auto: Filter candidates ≤ 5m (TriggerRadius)
    Auto->>Auto: ResolveTieBreaker() - Scoring System
    Note over Auto: Distance(40%) + Approach(30%)<br/>+ Priority(20%) + History(10%)

    alt POI đã nghe trong 5 phút qua (Cooldown)
        Auto->>User: DisplayAlert("Bạn đã nghe, muốn nghe lại?")
        alt User chọn "Không"
            Auto->>Auto: Skip
        else User chọn "Có"
            Auto->>Auto: QueueOrPlayAsync()
        end
    else POI mới
        Auto->>Auto: QueueOrPlayAsync(locationId)
    end

    Note over Auto: TH3: Đang phát A, đi ngang B → B xếp hàng
    
    alt Audio đang playing
        Auto->>Auto: pendingPlaybackQueue.Enqueue(locationId)
    else Audio idle
        Auto->>API: GetLocationByIdAsync(locationId)
        API-->>Auto: location + audioGuides
        Auto->>Audio: PlayAsync(url, locationId, guideId)
        Audio->>CDN: HTTP GET audio stream
        CDN-->>Audio: MP3 stream
        Audio->>Audio: CreatePlayer → Play()
        Audio-->>Auto: StateChanged(Playing)
    end

    Note over Auto: Khi A phát xong → tự phát B từ queue
    Audio-->>Auto: StateChanged(Stopped)
    Auto->>Auto: pendingPlaybackQueue.Dequeue()
    Auto->>Auto: PlayLocationAudioAsync(nextLocationId)

    Note over Auto: TH4: User bấm tay phát B → ngắt A
    User->>Auto: HandleManualPlaybackAsync(locB, guideB)
    Auto->>Auto: Lưu interruptedLocationId + position
    Auto->>Audio: StopAsync() (ngắt A)
    Auto->>Audio: PlayAsync(urlB, locB, guideB) (phát B liền)

    Note over Auto: Sau khi B xong → hỏi user
    Audio-->>Auto: StateChanged(Stopped)
    Auto->>User: DisplayActionSheet("Nghe tiếp A?")
    alt "Nghe tiếp từ chỗ bị ngắt"
        Auto->>Audio: PlayAsync(urlA) + SeekAsync(savedPosition)
    else "Nghe lại từ đầu"
        Auto->>Audio: PlayAsync(urlA)
    else "Bỏ qua"
        Note over Auto: Không làm gì
    end

    Note over Auto: Exit logic: Ra ngoài 20m → xóa khỏi "đang ở trong"
    Auto->>Auto: _currentlyInsideLocations.Remove(locId)
```

---

## 5. Phát Audio Guide (Manual)

```mermaid
sequenceDiagram
    actor User
    participant Detail as LocationDetailPage
    participant PlayerVM as AudioPlayerViewModel
    participant AudioSvc as AudioService
    participant Plugin as Plugin.Maui.Audio
    participant CDN as Cloudinary CDN

    User->>Detail: Chọn location → xem chi tiết
    User->>Detail: Nhấn Play audio guide
    Detail->>PlayerVM: Navigate to AudioPlayerPage

    PlayerVM->>AudioSvc: PlayAsync(cloudinaryUrl, locationId, guideId)
    Note over AudioSvc: Cooldown check (2.5s)

    AudioSvc->>AudioSvc: SetState(Loading)
    AudioSvc->>AudioSvc: CleanupPlayer() (dispose cũ)
    AudioSvc->>CDN: HTTP GET audio (f_mp3 URL)
    CDN-->>AudioSvc: MP3 byte stream
    AudioSvc->>AudioSvc: CopyTo MemoryStream
    AudioSvc->>Plugin: CreatePlayer(stream)
    AudioSvc->>Plugin: player.Play()
    AudioSvc->>AudioSvc: StartPositionTimer(150ms)
    AudioSvc-->>PlayerVM: StateChanged(Playing)

    loop Timer 150ms
        AudioSvc->>Plugin: player.CurrentPosition
        AudioSvc-->>PlayerVM: PositionChanged(pos, duration)
        PlayerVM->>PlayerVM: Sync transcript segment
    end

    User->>PlayerVM: Pause / Resume / Seek
    PlayerVM->>AudioSvc: PauseAsync() / ResumeAsync() / SeekAsync()

    Plugin-->>AudioSvc: PlaybackEnded
    AudioSvc-->>PlayerVM: StateChanged(Stopped)
    PlayerVM->>PlayerVM: AddListeningHistoryAsync()
```

---

## 6. Heartbeat & Session Keep-alive

```mermaid
sequenceDiagram
    participant App as App.xaml.cs
    participant HB as AppHeartbeatService
    participant Store as AppSessionStore
    participant API as RemoteApiService
    participant Server as Web API
    participant DB as SQL Server

    App->>HB: StartAsync(onSessionInvalidated)
    HB->>Store: GetSnapshotAsync()

    loop Mỗi 5 giây
        HB->>Store: GetSnapshotAsync()
        HB->>HB: Resolve Activity/Screen/Route

        HB->>API: SendHeartbeatAsync(request)
        API->>Server: POST /api/mobile/heartbeat
        Server->>DB: Find session(token + deviceId)
        Server->>DB: Check user status (not Blocked)
        Server->>DB: Check active subscription

        alt Session valid + subscription active
            Server->>DB: Update user.LastSeenAtUtc
            Server->>DB: session.ExpiresAtUtc += 30min
            Server->>DB: Add AppUserActivityLog
            Server->>DB: SaveChangesAsync()
            Server-->>HB: {Success: true}
            HB->>Store: SaveSnapshotAsync(refreshed)
        else Invalid / expired / blocked
            Server-->>HB: {Success: false}
            HB->>HB: StopAsync()
            HB->>Store: ClearSnapshotAsync()
            HB->>App: onSessionInvalidated()
            App->>App: ClearPersistedCulture()
            App->>App: NavigateToIntro()
        end
    end
```

---

## 7. Đăng nhập Web Admin (Cookie Auth)

```mermaid
sequenceDiagram
    actor Admin
    participant Login as Account/Login
    participant Auth as AuthUserStore
    participant DB as SQL Server
    participant Cookie as CookieAuth (8h sliding)

    Admin->>Login: POST username + password
    Login->>Auth: FindByCredentialsAsync(username, password)
    Auth->>DB: Query AuthUserAccounts (username, password, IsActive)
    DB-->>Auth: dbUser

    alt Không hợp lệ
        Auth-->>Login: null
        Login->>Admin: Lỗi đăng nhập
    else Hợp lệ
        Auth->>Auth: NormalizeRole()

        alt PoiAdmin
            Auth->>DB: Query PoiAdminLocationAssignments
            DB-->>Auth: assigned locationIds
        end

        Auth-->>Login: AuthenticatedUser(role, locationIds)
        Login->>Cookie: SignInAsync(claims)

        alt SystemAdmin
            Login->>Admin: → /Admin/Index
            Note over Admin: Full access: Locations, Categories,<br/>Tours, AudioGuides, Users, ChangeRequests,<br/>Reports, Settings, PaymentPackages
        else PoiAdmin
            Login->>Admin: → /Shop/Index
            Note over Admin: Shop portal: Edit assigned locations,<br/>AudioGuides, ChangeRequests, Analytics
        end
    end
```

---

## 8. POI Change Request Workflow (với TTS on Approval)

```mermaid
sequenceDiagram
    actor PoiAdmin
    participant Shop as Shop/ChangeRequests
    participant CRS as PoiChangeRequestService
    participant DB as SQL Server
    actor SysAdmin
    participant AdminCR as Admin/ChangeRequests
    participant TTS as EdgeTTSService
    participant CDN as Cloudinary

    PoiAdmin->>Shop: Tạo Change Request
    Shop->>CRS: SubmitAsync(request)
    CRS->>DB: Add PoiChangeRequest (status: Pending)
    DB-->>CRS: OK
    CRS-->>Shop: Created
    Shop->>PoiAdmin: "Đã gửi thành công"

    SysAdmin->>AdminCR: Xem danh sách requests
    AdminCR->>CRS: GetAllAsync()
    CRS->>DB: Query PoiChangeRequests
    CRS-->>AdminCR: list

    SysAdmin->>AdminCR: Approve request
    AdminCR->>CRS: TryUpdateStatusAsync(id, Approved)
    CRS->>CRS: CanTransition(Pending→Approved) ✓
    CRS->>CRS: TryApplyChangeSetAsync()
    CRS->>CRS: ParseChangeSet(JSON)

    alt Target = Location
        alt Action = create-location
            CRS->>DB: new Location + fields
            CRS->>DB: EnsureCategoryExists()
            CRS->>DB: EnsurePoiAdminOwnsLocation()
        else Action = delete-location
            CRS->>DB: Check ListeningHistory
            CRS->>DB: Remove assignments + location
        else Update fields
            CRS->>DB: Apply field changes
        end
    else Target = AudioGuide
        alt Action = create-audio-guide
            CRS->>DB: new AudioGuide
            CRS->>CRS: ApplyAudioGuideFieldChanges()
        end

        opt __tts_on_approval = true
            CRS->>TTS: SynthesizeAsync(transcript, language)
            TTS-->>CRS: byte[] audio
            CRS->>CDN: UploadAudioAsync(stream)
            CDN-->>CRS: AudioUrl + PublicId
            CRS->>CRS: Update AudioGuide (URL, TTS fields)
        end
    end

    CRS->>DB: Status = Approved, SaveChanges
    CRS-->>AdminCR: true
    AdminCR->>SysAdmin: "Đã duyệt & áp dụng"
```

---

## 9. TTS Generation (Edge + Google Fallback)

```mermaid
sequenceDiagram
    participant Caller as PoiChangeRequestService
    participant TTS as EdgeTextToSpeechService
    participant Edge as Edge Neural TTS
    participant Google as Google Translate TTS
    participant Storage as CloudinaryStorageService
    participant CDN as Cloudinary

    Caller->>TTS: SynthesizeAsync(text, "vi")
    TTS->>TTS: GetVoiceForLanguage("vi") → "vi-VN-HoaiMyNeural"
    TTS->>Edge: Communicate.Stream(text, voice)

    alt Edge OK
        Edge-->>TTS: audio chunks
        TTS-->>Caller: byte[] (MP3)
    else Edge 403
        TTS->>TTS: SynthesizeWithGoogleFallbackAsync()
        TTS->>TTS: SplitText(text, 180 chars)

        loop Mỗi chunk
            TTS->>Google: GET translate_tts?tl=vi&q={chunk}
            Google-->>TTS: MP3 bytes
            opt Chunk > 1
                TTS->>TTS: StripId3Header()
            end
            TTS->>TTS: Merge into stream
        end

        TTS-->>Caller: byte[] (merged MP3)
    end

    Caller->>Storage: UploadAudioAsync(stream, filename, prefix)
    Storage->>CDN: Upload (folder: audio/, publicId: prefix-GUID)
    CDN-->>Storage: SecureUrl + PublicId
    Storage->>Storage: URL → /video/upload/f_mp3/...
    Storage-->>Caller: {AudioUrl, CloudinaryPublicId}
```

---

## 10. Payment Package Management (Admin CRUD)

Service mới `PaymentPackageService` cho SystemAdmin quản lý gói thanh toán.

```mermaid
sequenceDiagram
    actor SysAdmin
    participant Page as Admin/PaymentPackages
    participant Svc as PaymentPackageService
    participant DB as SQL Server

    SysAdmin->>Page: Mở trang quản lý gói
    Page->>Svc: GetDashboardStatsAsync()
    Svc->>DB: Query PaymentPackages + UserSubscriptions (GroupBy)
    DB-->>Svc: packages + subscription stats
    Svc-->>Page: PackageDashboardStatsDto
    Note over Page: TotalPackages, ActiveSubs,<br/>Revenue, MostPopular

    SysAdmin->>Page: Tạo gói mới
    Page->>Svc: CreateAsync(PackageUpsertDto)
    Svc->>DB: Add PaymentPackage
    DB-->>Svc: entity
    Svc-->>Page: created

    SysAdmin->>Page: Cập nhật gói
    Page->>Svc: UpdateAsync(dto)
    Svc->>DB: Update Name, Price, Duration, TargetType, ...
    Svc-->>Page: updated

    SysAdmin->>Page: Toggle active/inactive
    Page->>Svc: ToggleActiveAsync(id)
    Svc->>DB: entity.IsActive = !IsActive
    Svc-->>Page: true

    SysAdmin->>Page: Xóa gói
    Page->>Svc: DeleteAsync(id)
    Svc->>DB: Check Subscriptions (Active/Pending)
    alt Có subscription đang active
        Svc-->>Page: (false, "Không thể xóa")
    else Không có
        Svc->>DB: Remove PaymentPackage
        Svc-->>Page: (true, null)
    end
```

---

## 11. Expired Account Cleanup (Background Service)

```mermaid
sequenceDiagram
    participant Host as ASP.NET Host
    participant Cleanup as ExpiredAccountCleanupService
    participant DB as SQL Server

    Host->>Cleanup: ExecuteAsync() (BackgroundService)

    loop Mỗi 6 tiếng
        Cleanup->>DB: Query AppUsers (IsDeleted = false)<br/>Include Subscriptions
        DB-->>Cleanup: users with subscriptions

        loop Mỗi user
            Cleanup->>Cleanup: Check hasActiveSubscription?
            Note over Cleanup: Active = Status=="Active"<br/>&& ExpiresAtUtc > now

            alt Không có subscription active
                Cleanup->>Cleanup: user.IsDeleted = true
                Cleanup->>Cleanup: user.Status = "Expired"
            end
        end

        Cleanup->>DB: SaveChangesAsync()
        Cleanup->>Cleanup: Log "Soft deleted {N} accounts"

        Cleanup->>Cleanup: Task.Delay(6 hours)
    end
```

---

## Tổng quan Kiến trúc Hệ thống

```mermaid
sequenceDiagram
    box rgb(40,60,90) Mobile App (.NET MAUI 8)
        participant UI as Views + XAML
        participant VM as ViewModels (MVVM)
        participant AutoPlay as AutoPlaybackService
        participant Geo as GeolocationService (5s)
        participant Audio as AudioService
        participant HB as HeartbeatService (5s)
        participant Remote as RemoteApiService
        participant Local as LocalDB + Cache
    end

    box rgb(50,80,50) Web Server (ASP.NET Core 8)
        participant API as Minimal API (/api/mobile/*)
        participant Pages as Razor Pages (Admin/Shop)
        participant Svc as Services (TTS, POI, Payment)
    end

    box rgb(80,50,50) External Services
        participant DB as SQL Server (AudioDB)
        participant CDN as Cloudinary CDN
        participant TTS as Edge/Google TTS
    end

    UI->>VM: User interaction (binding)
    VM->>Remote: API calls
    Remote->>API: HTTP GET/POST
    API->>DB: EF Core
    DB-->>API: Data
    API-->>Remote: JSON
    Remote->>Local: Cache response
    Remote-->>VM: Processed data
    VM-->>UI: PropertyChanged

    Geo->>API: Nearby locations (5s poll)
    Geo-->>AutoPlay: NearbyLocationDetected
    AutoPlay->>Audio: PlayAsync(url, locId, guideId)
    Audio->>CDN: Stream MP3

    HB->>API: Heartbeat (5s)
    API->>DB: Activity log + extend session

    Pages->>Svc: Admin actions
    Svc->>TTS: Synthesize audio
    Svc->>CDN: Upload
    Svc->>DB: Save
```
