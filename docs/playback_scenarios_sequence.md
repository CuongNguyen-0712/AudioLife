# Sequence Diagrams - Các Kịch Bản Auto-Playback

Dưới đây là sơ đồ tuần tự (Sequence Diagram) chi tiết cho 5 trường hợp theo đúng yêu cầu mô tả của dự án.

## TH1 — Tự động phát khi vào vùng POI

Khi bật chế độ auto-nearest, app bắt đầu tracking vị trí và tìm POI gần nhất trong vùng quét động. Nếu tìm thấy POI hợp lệ thì tự động phát audio của POI đó và cập nhật footer trạng thái “đang phát tự động”.

```mermaid
sequenceDiagram
    actor User
    participant App as App (Auto-Nearest)
    participant Geo as GeolocationTracker
    participant Scanner as DynamicScanner
    participant AutoPlay as AutoPlaybackService
    participant Audio as AudioService
    participant UI as Footer UI

    User->>App: Bật chế độ Auto-Nearest
    App->>Geo: StartTracking()
    
    loop Tracking định kỳ
        Geo->>Scanner: Cập nhật vị trí hiện tại
        Scanner->>Scanner: Tìm POI gần nhất trong vùng quét động
        
        alt Tìm thấy POI hợp lệ
            Scanner-->>AutoPlay: Phát hiện POI hợp lệ
            AutoPlay->>Audio: Yêu cầu phát Audio của POI
            Audio->>Audio: Load & Play()
            Audio-->>AutoPlay: Trạng thái (Playing)
            AutoPlay->>UI: Cập nhật: "Đang phát tự động"
            UI-->>User: Hiển thị trạng thái ở Footer
        end
    end
```

---

## TH2 — Đang nghe POI A, phát hiện POI B gần hơn

Hệ thống hiện tại ngắt luồng A khi chuyển POI: clear queue cũ, StopAsync(), rồi tạo queue mới cho B và phát B. Tức là không giữ A chạy đến hết rồi mới phát B.

```mermaid
sequenceDiagram
    participant Geo as GeolocationTracker
    participant Scanner as DynamicScanner
    participant AutoPlay as AutoPlaybackService
    participant Queue as QueueManager
    participant Audio as AudioService

    Note over Audio, AutoPlay: Đang phát audio của POI A
    
    Geo->>Scanner: Cập nhật vị trí hiện tại
    Scanner->>Scanner: Phát hiện POI B gần hơn POI A
    Scanner-->>AutoPlay: Trigger chuyển đổi POI sang B
    
    AutoPlay->>Queue: Clear queue cũ (của POI A)
    AutoPlay->>Audio: StopAsync() (Ngắt ngay luồng A)
    
    AutoPlay->>Queue: Tạo queue mới cho POI B
    AutoPlay->>Audio: Phát luồng mới cho POI B
    Audio->>Audio: Load & Play(POI B)
```

---

## TH3 — Người dùng chọn tay audio khác

Nếu user bấm chọn guide khác, app restart ngay guide được chọn (manual override). Đồng thời lưu “preferred guide/url” để các lần auto sau tại cùng POI ưu tiên đúng lựa chọn user.

```mermaid
sequenceDiagram
    actor User
    participant UI as Audio Selection UI
    participant AutoPlay as AutoPlaybackService
    participant Prefs as PreferenceStore
    participant Audio as AudioService

    User->>UI: Chọn tay guide khác (Manual override)
    UI->>AutoPlay: Yêu cầu phát guide được chọn
    
    AutoPlay->>Audio: StopAsync() (Ngắt guide đang phát)
    AutoPlay->>Audio: PlayAsync(Guide mới)
    
    AutoPlay->>Prefs: Lưu "preferred guide/url" của POI
    
    Note over AutoPlay, Prefs: Ở lần auto-play tiếp theo tại POI này,<br>hệ thống sẽ ưu tiên lấy preferred guide.
```

---

## TH4 - Đứng giữa 2 POI cùng lúc

Phát quán nào gần hơn trước, quán còn lại xếp hàng chờ. Nếu 2 quán cách đều nhau thì ưu tiên POI nào có PoiAdmin sử dụng gói thanh toán cao hơn thì phát trước.

```mermaid
sequenceDiagram
    participant Scanner as DynamicScanner
    participant PkgService as PaymentPackageService
    participant AutoPlay as AutoPlaybackService
    participant Queue as QueueManager
    participant Audio as AudioService

    Scanner->>Scanner: Phát hiện đứng giữa 2 POI cùng lúc
    
    alt Khoảng cách khác nhau
        Scanner->>Scanner: Đánh giá khoảng cách
        Scanner-->>AutoPlay: POI gần hơn (Ưu tiên 1), POI kia (Ưu tiên 2)
    else Khoảng cách bằng nhau (Cách đều)
        Scanner->>PkgService: Kiểm tra gói thanh toán của PoiAdmin cho 2 POI
        PkgService-->>Scanner: Trả về mức gói thanh toán (Package Level)
        Scanner->>Scanner: So sánh, chọn POI có gói cao hơn
        Scanner-->>AutoPlay: POI có gói cao hơn (Ưu tiên 1), POI kia (Ưu tiên 2)
    end
    
    AutoPlay->>Audio: Phát audio cho POI Ưu tiên 1
    AutoPlay->>Queue: Đưa POI Ưu tiên 2 vào hàng đợi (Queue)
```

---

## TH5 — Hàng đợi theo lộ trình Tour

Tour có queue theo thứ tự location IDs. Hết audio điểm hiện tại thì tự động chuyển sang điểm kế tiếp. Có Pause & Save checkpoint (vị trí + audio + thời điểm) và Continue để resume đúng vị trí đã lưu.

```mermaid
sequenceDiagram
    actor User
    participant AutoPlay as AutoPlaybackService
    participant TourQueue as TourQueueManager
    participant Audio as AudioService
    participant Store as CheckpointStore

    Note over TourQueue: Queue được tạo theo thứ tự Location IDs của Tour
    
    AutoPlay->>Audio: Phát audio điểm hiện tại trong Tour
    
    alt Hết audio điểm hiện tại
        Audio-->>AutoPlay: Playback Ended
        AutoPlay->>TourQueue: Lấy thông tin điểm kế tiếp
        TourQueue-->>AutoPlay: Location ID kế tiếp
        AutoPlay->>Audio: Tự động chuyển & phát audio điểm kế tiếp
    else Người dùng tạm dừng (Pause & Save)
        User->>AutoPlay: Pause
        AutoPlay->>Audio: PauseAsync()
        AutoPlay->>Store: Save Checkpoint (Vị trí + Audio + Thời điểm)
    else Người dùng tiếp tục (Continue)
        User->>AutoPlay: Continue
        AutoPlay->>Store: Get Checkpoint
        Store-->>AutoPlay: Trả về dữ liệu checkpoint
        AutoPlay->>Audio: ResumeAsync() từ đúng thời điểm đã lưu
    end
```
