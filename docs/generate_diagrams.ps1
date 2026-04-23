$dir = "d:\Project\repos\AudioLife\docs\diagrams"
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

Set-Content -Path "$dir\01_app_startup.mmd" -Value @"
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
        App->>App: NavigateToShellRoot()
        App->>User: Vào AppShell (guest/offline)
    else Online
        alt Có valid local session
            App->>App: NavigateToShellRoot()
            App->>App: StartAutoPlaybackAsync()
        end
        App->>API: CheckDeviceSessionAsync(deviceId)
        API->>Server: GET /api/mobile/session/by-device
        Server->>DB: Query UserAppSessions
        Server-->>API: DeviceSessionCheckResult
        alt Không có session trên server
            App->>Store: ClearSnapshotAsync()
            App->>User: NavigateToIntro()
        else Có session
            App->>API: ValidateSessionAsync(token, deviceId)
            Server->>DB: Check session + subscription
            Server-->>API: SessionValidationResult
            alt Invalid
                App->>Store: ClearSnapshotAsync()
                App->>User: NavigateToIntro()
            else Valid
                App->>Store: SaveSnapshotAsync(newSnapshot)
                App->>App: StartHeartbeatAsync()
                App->>User: AppShell (verified)
            end
        end
    end
"@ -Encoding UTF8

Set-Content -Path "$dir\02_qr_onboarding_payment.mmd" -Value @"
sequenceDiagram
    actor User
    participant Intro as IntroPage
    participant QR as QrScannerPage
    participant App as App.xaml.cs
    participant PaySel as PaymentPlanSelectionPage
    participant PayOut as PaymentCheckoutPage
    participant API as RemoteApiService
    participant Server as Web API
    participant DB as SQL Server
    participant Lang as LanguageSection

    User->>Intro: Nhấn Bắt đầu
    Intro->>App: NavigateToQrScanner()
    App->>QR: Hiển thị QrScannerPage
    User->>QR: Quét mã QR
    QR->>App: CompleteQrOnboardingAsync(payload)
    App->>PaySel: Hiển thị PaymentPlanSelectionPage
    PaySel->>API: GetPaymentPackagesAsync()
    API->>Server: GET /api/mobile/payment/packages
    Server->>DB: Query PaymentPackages
    Server-->>PaySel: packages
    PaySel->>User: Hiển thị danh sách gói
    User->>PaySel: Chọn gói
    PaySel->>PayOut: Navigate to Checkout
    User->>PayOut: Xác nhận thanh toán
    PayOut->>API: CompletePaymentAsync(request)
    API->>Server: POST /api/mobile/payment/complete
    Server->>DB: Create AppUser + Subscription + Session
    Server-->>PayOut: sessionToken + expiresAt
    PayOut->>App: NavigateToLanguageSelection()
    App->>Lang: Hiển thị LanguageSection
    User->>Lang: Chọn ngôn ngữ
    Lang->>App: NavigateToShellRoot()
    App->>App: StartHeartbeat + AutoPlayback
    App->>User: AppShell (MainPage)
"@ -Encoding UTF8

Set-Content -Path "$dir\03_browse_search_poi.mmd" -Value @"
sequenceDiagram
    actor User
    participant VM as MainViewModel
    participant API as RemoteApiService
    participant Cache as LocalDatabaseService
    participant L10n as ContentLocalizationMapper
    participant Fallback as ApiService (local)
    participant Server as Web API
    participant DB as SQL Server

    User->>VM: Mở MainPage
    VM->>API: GetLocationsAsync()
    API->>Server: GET /api/mobile/locations?language=vi
    alt Server OK
        Server->>DB: Locations + Category + AudioGuides
        Server-->>API: locations
        API->>Cache: UpsertCacheAsync()
        API->>L10n: LocalizeLocations()
        API-->>VM: localized locations
    else Server unavailable
        API->>Cache: GetCachedJsonAsync()
        alt Cache hit
            Cache-->>API: cached data
            API->>L10n: LocalizeLocations()
            API-->>VM: cached locations
        else Cache miss
            API->>Fallback: GetLocationsAsync()
            Fallback-->>VM: local seed data
        end
    end
    VM->>User: Hiển thị danh sách
"@ -Encoding UTF8

Set-Content -Path "$dir\04_auto_playback_system.mmd" -Value @"
sequenceDiagram
    actor User
    participant Geo as GeolocationService
    participant GPS as Device GPS
    participant API as RemoteApiService
    participant Auto as AutoPlaybackService
    participant Audio as AudioService
    participant CDN as Cloudinary

    Note over Geo: Tracking mỗi 5 giây
    loop Mỗi 5s
        Geo->>GPS: GetLocationAsync()
        GPS-->>Geo: lat, lng, accuracy
        Geo->>API: GetNearbyLocationsAsync()
        API-->>Geo: nearby locations
        Geo-->>Auto: NearbyLocationDetected(candidates)
    end

    Note over Auto: TH1 - User vào bán kính 5m
    Auto->>Auto: Filter candidates trong 5m
    Auto->>Auto: ResolveTieBreaker
    alt POI đã nghe 5 phút
        Auto->>User: Muốn nghe lại không?
    else POI mới
        Auto->>Auto: QueueOrPlayAsync()
    end

    Note over Auto: TH3 - Đang phát A, B xếp hàng
    alt Audio đang playing
        Auto->>Auto: Queue B
    else Audio idle
        Auto->>API: GetLocationByIdAsync()
        Auto->>Audio: PlayAsync(url, locId, guideId)
        Audio->>CDN: Stream MP3
        Audio-->>Auto: StateChanged(Playing)
    end

    Audio-->>Auto: StateChanged(Stopped)
    Auto->>Auto: Dequeue next
    Auto->>Auto: PlayNext()

    Note over Auto: TH4 - User bấm B ngắt A
    User->>Auto: HandleManualPlaybackAsync(B)
    Auto->>Auto: Save A position
    Auto->>Audio: Stop A, Play B
    Audio-->>Auto: B Stopped
    Auto->>User: Nghe tiếp A?
    alt Nghe tiếp
        Auto->>Audio: Play A + Seek
    else Nghe lại từ đầu
        Auto->>Audio: Play A
    else Bỏ qua
        Note over Auto: Done
    end
"@ -Encoding UTF8

Set-Content -Path "$dir\05_manual_audio_playback.mmd" -Value @"
sequenceDiagram
    actor User
    participant Detail as LocationDetailPage
    participant PlayerVM as AudioPlayerViewModel
    participant AudioSvc as AudioService
    participant Plugin as Plugin.Maui.Audio
    participant CDN as Cloudinary CDN

    User->>Detail: Chọn location
    User->>Detail: Nhấn Play
    Detail->>PlayerVM: Navigate to AudioPlayerPage
    PlayerVM->>AudioSvc: PlayAsync(url, locationId, guideId)
    AudioSvc->>AudioSvc: SetState(Loading)
    AudioSvc->>CDN: HTTP GET audio
    CDN-->>AudioSvc: MP3 stream
    AudioSvc->>Plugin: CreatePlayer(stream)
    AudioSvc->>Plugin: player.Play()
    AudioSvc-->>PlayerVM: StateChanged(Playing)
"@ -Encoding UTF8

Set-Content -Path "$dir\06_heartbeat_keepalive.mmd" -Value @"
sequenceDiagram
    participant App as App.xaml.cs
    participant HB as AppHeartbeatService
    participant Store as AppSessionStore
    participant API as RemoteApiService
    participant Server as Web API
    participant DB as SQL Server

    App->>HB: StartAsync(onSessionInvalidated)
    loop Mỗi 5 giây
        HB->>Store: GetSnapshotAsync()
        HB->>API: SendHeartbeatAsync(request)
        API->>Server: POST /api/mobile/heartbeat
        Server->>DB: Find session
        alt Valid
            Server->>DB: Update LastSeen
            Server-->>HB: Success true
        else Invalid
            Server-->>HB: Success false
            HB->>App: onSessionInvalidated()
        end
    end
"@ -Encoding UTF8

Set-Content -Path "$dir\07_web_admin_login.mmd" -Value @"
sequenceDiagram
    actor Admin
    participant Login as Account/Login
    participant Auth as AuthUserStore
    participant DB as SQL Server
    participant Cookie as CookieAuth

    Admin->>Login: POST username + password
    Login->>Auth: FindByCredentialsAsync()
    Auth->>DB: Query AuthUserAccounts
    DB-->>Auth: dbUser
    alt Hợp lệ
        Auth-->>Login: AuthenticatedUser
        Login->>Cookie: SignInAsync(claims)
        Login->>Admin: Redirect Dashboard
    else Không hợp lệ
        Login->>Admin: Lỗi đăng nhập
    end
"@ -Encoding UTF8

Set-Content -Path "$dir\08_poi_change_request.mmd" -Value @"
sequenceDiagram
    actor PoiAdmin
    participant Shop as Shop/ChangeRequests
    participant CRS as PoiChangeRequestService
    participant DB as SQL Server
    actor SysAdmin
    participant AdminCR as Admin/ChangeRequests

    PoiAdmin->>Shop: Tạo Change Request
    Shop->>CRS: SubmitAsync(request)
    CRS->>DB: Add PoiChangeRequest
    SysAdmin->>AdminCR: Xem requests
    AdminCR->>CRS: Approve request
    CRS->>CRS: TryApplyChangeSetAsync()
    CRS->>DB: Update Location/Audio
    CRS->>DB: Status = Approved
"@ -Encoding UTF8

Set-Content -Path "$dir\09_tts_generation.mmd" -Value @"
sequenceDiagram
    participant Caller as PoiChangeRequestService
    participant TTS as EdgeTextToSpeechService
    participant Edge as Edge Neural TTS
    participant Storage as CloudinaryStorageService
    participant CDN as Cloudinary

    Caller->>TTS: SynthesizeAsync(text, vi)
    TTS->>Edge: Communicate.Stream(text)
    Edge-->>TTS: audio bytes
    TTS-->>Caller: MP3 bytes
    Caller->>Storage: UploadAudioAsync()
    Storage->>CDN: Upload
    CDN-->>Storage: AudioUrl
    Storage-->>Caller: AudioUrl
"@ -Encoding UTF8

Set-Content -Path "$dir\10_payment_package_management.mmd" -Value @"
sequenceDiagram
    actor SysAdmin
    participant Page as Admin/PaymentPackages
    participant Svc as PaymentPackageService
    participant DB as SQL Server

    SysAdmin->>Page: Mở quản lý gói
    Page->>Svc: GetDashboardStatsAsync()
    Svc->>DB: Query Packages + Subs
    Svc-->>Page: Dashboard Stats
    SysAdmin->>Page: Tạo/Cập nhật gói
    Page->>Svc: Create/UpdateAsync()
    Svc->>DB: SaveChanges
"@ -Encoding UTF8

Set-Content -Path "$dir\11_expired_account_cleanup.mmd" -Value @"
sequenceDiagram
    participant Host as ASP.NET Host
    participant Cleanup as ExpiredAccountCleanupService
    participant DB as SQL Server

    Host->>Cleanup: ExecuteAsync
    loop Mỗi 6 tiếng
        Cleanup->>DB: Query Users + Subs
        DB-->>Cleanup: users
        Cleanup->>Cleanup: Check active subscription
        alt Hết hạn
            Cleanup->>Cleanup: IsDeleted=true
        end
        Cleanup->>DB: SaveChangesAsync()
    end
"@ -Encoding UTF8

Write-Host "MMD files created."

# Now generate PNGs using npx mmdc
$files = Get-ChildItem -Path $dir -Filter "*.mmd"
foreach ($file in $files) {
    $pngPath = "$dir\$($file.BaseName).png"
    Write-Host "Rendering $($file.Name) to $pngPath..."
    npx @mermaid-js/mermaid-cli@latest -i $file.FullName -o $pngPath --backgroundColor white
}

Write-Host "Done rendering."
