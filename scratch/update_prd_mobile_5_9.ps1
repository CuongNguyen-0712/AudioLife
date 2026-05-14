$path = 'd:\Develop\AudioLife\PRD_AudioLife_VinhKhanhAudioGuide_v3.0.html'
$content = [System.IO.File]::ReadAllText($path)

# --- Section 5: GPS ---
$old5 = @"
sequenceDiagram
    participant Device as GPS Hardware
    participant GeoService as GeolocationService
    participant API as Mobile API
    
    loop Chu kỳ qu&#233;t (VD: 60s)
        GeoService-&gt;&gt;Device: Y&#234;u cầu tọa độ hiện tại
        Device--&gt;&gt;GeoService: Trả về Lat, Lng
        GeoService-&gt;&gt;API: GET /api/mobile/locations/nearby?lat=...&amp;lng=...&amp;radius=0.1
        API--&gt;&gt;GeoService: Trả về danh s&#225;ch POI nằm trong b&#225;n k&#237;nh (Nearby)
        GeoService-&gt;&gt;GeoService: T&#237;nh to&#225;n khoảng c&#225;ch ch&#237;nh x&#225;c bằng c&#244;ng thức Haversine
    end
"@
$new5 = @"
sequenceDiagram
    participant Device as GPS Hardware
    participant GeoService as GeolocationService
    participant API as RemoteApiService
    participant Backend as Mobile API
    
    loop Chu kỳ quét (Cấu hình: GeolocationPriority.High)
        GeoService->>Device: GetLocationAsync()
        Device-->>GeoService: Trả về Location (Lat, Lng)
        GeoService->>API: GetNearbyLocationsAsync(lat, lng, radius)
        API->>Backend: GET /api/mobile/locations/nearby
        Backend-->>API: List<LocationDto>
        API-->>GeoService: Nearby locations
        GeoService->>GeoService: Fire NearbyLocationDetected event
    end
"@

# --- Section 6: Queue ---
$old6 = @"
sequenceDiagram
    participant Geo as GeolocationService
    participant Queue as QueueManager
    
    Geo-&gt;&gt;Queue: Truyền v&#224;o danh s&#225;ch POI gần nhất vừa lấy được
    Queue-&gt;&gt;Queue: Dọn dẹp c&#225;c POI cũ đ&#227; nằm ngo&#224;i b&#225;n k&#237;nh (Exit geofence)
    Queue-&gt;&gt;Queue: Th&#234;m c&#225;c POI mới v&#224;o h&#224;ng đợi hệ thống
    Queue-&gt;&gt;Queue: Ph&#226;n loại &amp; Cập nhật thứ tự ưu ti&#234;n
    Queue--&gt;&gt;Geo: Phản hồi trạng th&#225;i h&#224;ng đợi đ&#227; sẵn s&#224;ng
"@
$new6 = @"
sequenceDiagram
    participant Geo as GeolocationService
    participant AutoPlay as AutoPlaybackService
    participant Store as Local State
    
    Geo->>AutoPlay: OnNearbyLocationDetected(args)
    AutoPlay->>Store: Kiểm tra danh sách POI hiện tại
    AutoPlay->>AutoPlay: HandleProximityTriggerAsync()
    AutoPlay->>AutoPlay: QueueOrPlayAsync(candidate)
    AutoPlay->>AutoPlay: Sắp xếp theo Scoring Tie-Breaker
    AutoPlay-->>Geo: Cập nhật trạng thái phát
"@

# --- Section 7: Xử lý trùng ---
$old7 = @"
sequenceDiagram
    participant Queue as QueueManager
    participant Cache as PlayedHistoryCache
    
    Queue-&gt;&gt;Queue: Đ&#225;nh gi&#225; một POI trong h&#224;ng đợi
    Queue-&gt;&gt;Cache: Kiểm tra t&#237;nh hợp lệ
    alt Trường hợp 1: Debounce (Chống spam)
        Cache--&gt;&gt;Queue: Đ&#227; qu&#233;t qua POI n&#224;y trong v&#242;ng 30s qua
        Queue-&gt;&gt;Queue: Bỏ qua (Drop) để tr&#225;nh trigger li&#234;n tục
    else Trường hợp 2: Đ&#227; ph&#225;t ho&#224;n to&#224;n (Played)
        Cache--&gt;&gt;Queue: Đ&#227; nghe trọn vẹn POI n&#224;y trong 24h qua
        Queue-&gt;&gt;Queue: X&#243;a khỏi h&#224;ng đợi Auto-play (User vẫn c&#243; thể bấm tay)
    else Trường hợp 3: Hợp lệ
        Cache--&gt;&gt;Queue: POI mới hoặc đ&#227; qua thời gian Cooldown
        Queue-&gt;&gt;Queue: Đưa v&#224;o danh s&#225;ch được ph&#233;p Auto-play
    end
"@
$new7 = @"
sequenceDiagram
    participant AutoPlay as AutoPlaybackService
    participant History as ListeningHistory
    
    AutoPlay->>AutoPlay: Evaluate candidate POI
    
    alt Trường hợp 1: Cooldown (Default 5 mins)
        AutoPlay->>AutoPlay: Check LastPlayTime
        Note over AutoPlay: Bỏ qua nếu chưa hết Cooldown
    else Trường hợp 2: Đã nghe hoàn toàn
        AutoPlay->>History: Check IsFullyPlayed(LocationId)
        History-->>AutoPlay: True
        Note over AutoPlay: Giảm ưu tiên hoặc Skip Auto-play
    else Trường hợp 3: Trùng lặp tức thời
        AutoPlay->>AutoPlay: Check IsAlreadyInQueue?
        Note over AutoPlay: Bỏ qua để tránh duplicate playback
    end
"@

# --- Section 8: Scoring ---
$old8 = @"
sequenceDiagram
    participant Queue as QueueManager
    
    Queue-&gt;&gt;Queue: Nhận danh s&#225;ch c&#225;c POI hợp lệ trong h&#224;ng đợi
    loop T&#237;nh điểm từng POI
        Queue-&gt;&gt;Queue: BaseScore (Mặc định)
        Queue-&gt;&gt;Queue: + Khoảng c&#225;ch thực (Gần hơn -&gt; Điểm cao hơn)
        Queue-&gt;&gt;Queue: x Multiplier (Nh&#226;n hệ số nếu l&#224; POI Nổi bật/Featured)
    end
    Queue-&gt;&gt;Queue: Sắp xếp danh s&#225;ch giảm dần theo Tổng Điểm (DESC)
    Queue-&gt;&gt;Queue: Chọn POI c&#243; điểm cao nhất để k&#237;ch hoạt ph&#225;t audio
"@
$new8 = @"
sequenceDiagram
    participant AutoPlay as AutoPlaybackService
    
    AutoPlay->>AutoPlay: ResolveTieBreaker(candidates)
    loop Từng Candidate
        AutoPlay->>AutoPlay: score = CalculateDistanceScore()
        AutoPlay->>AutoPlay: score += CalculateApproachScore (Góc tiếp cận)
        AutoPlay->>AutoPlay: score += Priority (POI Featured)
        AutoPlay->>AutoPlay: score -= HistoryPenalty (Đã nghe gần đây)
    end
    AutoPlay->>AutoPlay: OrderByDescending(s => s.TotalScore)
    AutoPlay->>AutoPlay: Select winner (Top 1)
"@

# --- Section 9: Search ---
$old9 = @"
sequenceDiagram
    actor User
    participant View as SearchPage
    participant API as Mobile API
    
    User-&gt;&gt;View: Nhập từ kh&#243;a t&#236;m kiếm
    View-&gt;&gt;View: Timer: Chờ 300ms kh&#244;ng g&#245; ph&#237;m (Debounce)
    View-&gt;&gt;API: GET /api/mobile/locations/search?query={keyword}
    API--&gt;&gt;View: Trả về danh s&#225;ch kết quả
    View--&gt;&gt;User: Hiển thị danh s&#225;ch kết quả (Locations, Tours)
"@
$new9 = @"
sequenceDiagram
    actor User
    participant VM as SearchViewModel
    participant API as RemoteApiService
    participant Backend as Mobile API
    
    User->>VM: Nhập Query string
    VM->>VM: TriggerSearchDebouncedAsync()
    VM->>VM: ExecuteSearchAsync()
    VM->>API: SearchLocationsAsync(query)
    API->>Backend: GET /api/mobile/locations/search
    Backend-->>API: SearchResultDto
    API-->>VM: Results (Locations + Tours)
    VM-->>User: Update UI (ObservableCollection)
"@

$content = $content.Replace($old5, $new5)
$content = $content.Replace($old6, $new6)
$content = $content.Replace($old7, $new7)
$content = $content.Replace($old8, $new8)
$content = $content.Replace($old9, $new9)

[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
Write-Output "Done"
