$path = 'd:\Develop\AudioLife\PRD_AudioLife_VinhKhanhAudioGuide_v3.0.html'
$content = [System.IO.File]::ReadAllText($path)

# --- Section 10: Tour Detail ---
$old10 = @"
sequenceDiagram
    actor User
    participant View as TourDetailPage
    participant API as Mobile API
    
    User-&gt;&gt;View: Bấm v&#224;o xem một Tour
    View-&gt;&gt;API: GET /api/mobile/tours/{id}
    API--&gt;&gt;View: Trả về th&#244;ng tin tổng quan &amp; List&lt;Stops&gt;
    View--&gt;&gt;User: Hiển thị bản đồ lộ tr&#236;nh &amp; thứ tự c&#225;c điểm dừng (Stops)
"@
$new10 = @"
sequenceDiagram
    actor User
    participant VM as TourDetailViewModel
    participant API as RemoteApiService
    participant Backend as Mobile API
    
    User->>VM: Select Tour
    VM->>VM: LoadTourDetailsAsync(id)
    VM->>API: GetTourByIdAsync(id)
    API->>Backend: GET /api/mobile/tours/{id}
    Backend-->>API: TourDto
    API-->>VM: Tour details (Stops, Geofences)
    VM-->>User: Render Map & Stop List
"@

# --- Section 11: Tour Progress ---
$old11 = @"
sequenceDiagram
    actor User
    participant View as TourDetailPage
    participant DB as SQLite (Local Progress)
    
    User-&gt;&gt;View: Mở lại một Tour đang đi dở
    View-&gt;&gt;DB: Truy vấn Progress(TourId)
    DB--&gt;&gt;View: Trả về: Đ&#227; ho&#224;n th&#224;nh đến Checkpoint #3
    View--&gt;&gt;User: Hiển thị UI: Highlight điểm đến tiếp theo (Stop #4)
    
    User-&gt;&gt;View: Nhấn ho&#224;n th&#224;nh Stop #4
    View-&gt;&gt;DB: Cập nhật Checkpoint(TourId, StopId=4)
    DB--&gt;&gt;View: Lưu th&#224;nh c&#244;ng
    View--&gt;&gt;User: Cập nhật lại thanh tiến tr&#236;nh (Progress Bar)
"@
$new11 = @"
sequenceDiagram
    actor User
    participant VM as TourDetailViewModel
    participant CPS as TourCheckpointService
    participant DB as SQLite
    
    User->>VM: Resume Tour
    VM->>CPS: GetCheckpointAsync(tourId)
    CPS->>DB: SELECT * FROM TourCheckpoints
    DB-->>CPS: Last stop index
    CPS-->>VM: Checkpoint data
    VM-->>User: Highlight next stop
    
    User->>VM: Arrive at Stop
    VM->>CPS: SaveCheckpointAsync(tourId, stopIndex)
    CPS->>DB: INSERT/UPDATE TourCheckpoints
    VM-->>User: Update Progress Bar
"@

# --- Section 12: QR / Payment ---
$old12 = @"
sequenceDiagram
    actor User
    participant UI as Onboarding
    participant Store as AppStore / CH Play
    participant API as Mobile API (/payment &amp; /session)
    
    alt Luồng 1: Qu&#233;t QR (Đại l&#253;)
        User-&gt;&gt;UI: Chọn Qu&#233;t QR Đại l&#253;
        UI-&gt;&gt;API: POST /api/mobile/session/scan (K&#232;m QRCode)
        API--&gt;&gt;UI: Trả về SessionToken
    else Luồng 2: Thanh to&#225;n Online
        User-&gt;&gt;UI: Chọn Mua G&#243;i In-App
        UI-&gt;&gt;Store: Request Payment
        Store--&gt;&gt;UI: Trả về Payment Receipt
        UI-&gt;&gt;API: POST /api/mobile/payment/complete (Gửi Receipt)
        API--&gt;&gt;UI: Verify Receipt &amp; Trả về SessionToken
    end
    
    UI-&gt;&gt;UI: Lưu SessionToken &amp; DeviceId v&#224;o Local Secure Storage
"@
$new12 = @"
sequenceDiagram
    actor User
    participant App as App.xaml.cs
    participant QR as QrCodePayloadService
    participant API as RemoteApiService
    participant Backend as Mobile API
    
    alt Luồng 1: Quét QR
        User->>App: Scan QR Code
        App->>QR: TryParseAudioPayload(code)
        QR-->>App: AudioPayload (LocationId, PackageId)
        App->>API: CompleteQrOnboardingAsync(payload)
        API->>Backend: POST /api/mobile/session/scan
        Backend-->>App: Session Snapshot
    else Luồng 2: Thanh toán Online
        User->>App: Buy Package
        App->>API: CompletePaymentAsync(receipt)
        API->>Backend: POST /api/mobile/payment/complete
        Backend-->>App: Session Snapshot
    end
    App->>App: SaveSnapshotAsync()
"@

# --- Section 13: Session Management ---
$old13 = @"
sequenceDiagram
    participant App as App Lifecycle
    participant Storage as SecureStorage
    participant API as Mobile API
    
    App-&gt;&gt;App: Mở App (OnStart)
    App-&gt;&gt;Storage: Lấy SessionToken &amp; DeviceId
    alt Token Null hoặc Trống
        Storage--&gt;&gt;App: None
        App-&gt;&gt;App: Điều hướng đến trang Intro / Payment
    else C&#243; Token
        Storage--&gt;&gt;App: TokenData
        App-&gt;&gt;API: GET /api/mobile/session/validate?token={Token}
        API--&gt;&gt;App: Trạng th&#225;i (Hợp lệ / Lỗi / Hết hạn)
        alt Hợp lệ
            App-&gt;&gt;App: Cho ph&#233;p truy cập v&#224;o Trang chủ (Shell)
        else Kh&#244;ng hợp lệ / Hết hạn
            App-&gt;&gt;Storage: X&#243;a Token hiện tại
            App-&gt;&gt;App: Force điều hướng về trang Intro
        end
    end
"@
$new13 = @"
sequenceDiagram
    participant App as App.xaml.cs
    participant Store as AppSessionStore
    participant API as RemoteApiService
    participant Backend as Mobile API
    
    App->>App: InitializeStartupAsync()
    App->>Store: GetSnapshotAsync()
    alt No Snapshot
        App->>App: NavigateToIntro()
    else Has Snapshot
        App->>API: ValidateSessionAsync(token)
        API->>Backend: GET /api/mobile/session/validate
        alt Valid
            App->>App: NavigateToShellRoot()
        else Invalid/Expired
            App->>Store: ClearSnapshotAsync()
            App->>App: NavigateToIntro()
        end
    end
"@

# --- Section 14: Heartbeat ---
$old14 = @"
sequenceDiagram
    participant App as Background Service
    participant API as Session API
    
    loop Mỗi chu kỳ (VD: 60 gi&#226;y)
        App-&gt;&gt;API: Gọi Heartbeat GET /api/mobile/session/heartbeat (DeviceId, Token)
        API-&gt;&gt;API: Cập nhật trường LastActiveTime trong DB
        API--&gt;&gt;App: Trả về HTTP 200 OK (Keep Alive) hoặc HTTP 401 (Hết hạn)
        
        alt Nhận m&#227; 401 Unauthorized
            App-&gt;&gt;App: Bắn Event SessionExpired
            App-&gt;&gt;App: X&#243;a dữ liệu &amp; Buộc người d&#249;ng tho&#225;t về m&#224;n h&#236;nh đăng nhập
        end
    end
"@
$new14 = @"
sequenceDiagram
    participant App as AppHeartbeatService
    participant API as RemoteApiService
    participant Backend as Mobile API
    
    loop Mỗi 60 giây
        App->>API: SendHeartbeatAsync()
        API->>Backend: POST /api/mobile/heartbeat
        alt 200 OK
            Note over App: Session active
        else 401 Unauthorized
            App->>App: Fire SessionExpired
            App->>App: Force NavigateToIntro()
        end
    end
"@

# --- Section 15: Multi-language ---
$old15 = @"
sequenceDiagram
    actor User
    participant UI as Views/Pages
    participant Resx as LocalizationResources
    participant Pref as Language Preferences
    
    User-&gt;&gt;Pref: Thay đổi ng&#244;n ngữ (VD: English)
    Pref-&gt;&gt;Pref: Lưu Setting v&#224;o LocalStorage (&quot;en-US&quot;)
    Pref-&gt;&gt;UI: Ph&#225;t t&#237;n hiệu CultureChanged ra to&#224;n App
    
    loop Mọi th&#224;nh phần UI đang mở
        UI-&gt;&gt;Resx: Y&#234;u cầu lấy văn bản theo Resource Key
        Resx--&gt;&gt;UI: Trả về text bằng ng&#244;n ngữ mới (Tiếng Anh)
        UI-&gt;&gt;UI: Cập nhật ngay lập tức nội dung hiển thị (Ti&#234;u đề, M&#244; tả...)
    end
"@
$new15 = @"
sequenceDiagram
    actor User
    participant VM as SettingsViewModel
    participant Loc as LocalizationService
    participant Mapper as ContentLocalizationMapper
    
    User->>VM: Change Language
    VM->>Loc: SetCulture(newCulture)
    Loc->>Loc: Update CurrentThread Culture
    Loc-->>VM: Notify CultureChanged
    
    VM->>Mapper: LocalizeContentAsync()
    Mapper->>Mapper: Update UI strings from Resx
    VM-->>User: Refresh Page Content
"@

$content = $content.Replace($old10, $new10)
$content = $content.Replace($old11, $new11)
$content = $content.Replace($old12, $new12)
$content = $content.Replace($old13, $new13)
$content = $content.Replace($old14, $new14)
$content = $content.Replace($old15, $new15)

[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
Write-Output "Done"
