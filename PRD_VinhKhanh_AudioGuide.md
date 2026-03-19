# PRD - Vinh Khanh Audio Guide

## 1. Thông tin tài liệu
- Product: Vinh Khanh Audio Guide (Mobile + Web Admin)
- Phiên bản PRD: 1.0
- Ngày cập nhật: 2026-03-19
- Trạng thái: Draft for implementation
- Owner: Product/Engineering Team

## 2. Tóm tắt sản phẩm
Vinh Khanh Audio Guide là hệ sinh thái gồm:
- Ứng dụng mobile MAUI cho người dùng cuối để khám phá địa điểm ẩm thực và nghe audio guide.
- Web Admin ASP.NET Core Razor Pages cho vận hành nội dung, phân quyền và quản trị theo mô hình:
  - Admin Hệ Thống
  - Admin POI (quản lý theo địa điểm được phân quyền)

Mục tiêu là chuẩn hóa trải nghiệm khám phá ẩm thực bằng audio, đồng thời cung cấp nền tảng quản trị nội dung hiệu quả, trực quan và hoàn toàn tiếng Việt.

## 3. Bài toán cần giải quyết
### 3.1 Vấn đề hiện tại
- Nội dung audio/địa điểm cần quản trị tập trung và phân quyền rõ theo khu vực POI.
- Trải nghiệm khám phá ẩm thực cho người dùng cần có lộ trình rõ ràng, dễ tìm kiếm, dễ nghe.
- Giao diện quản trị cần thống nhất ngôn ngữ, đúng định hướng thiết kế, hạn chế mismatch content.

### 3.2 Cơ hội
- Tăng thời gian tương tác qua audio guide theo từng điểm đến.
- Tối ưu vận hành bằng dashboard, báo cáo, kiểm duyệt, và workflow quản trị nội dung.
- Mở rộng dữ liệu địa điểm/tour/audio theo từng giai đoạn mà không phá vỡ cấu trúc hệ thống.

## 4. Mục tiêu sản phẩm
### 4.1 Mục tiêu kinh doanh
- Chuẩn hóa quy trình quản lý nội dung audio theo địa điểm và danh mục.
- Rút ngắn thời gian cập nhật nội dung từ lúc tạo đến lúc hiển thị.
- Tăng mức độ sử dụng tính năng tìm kiếm, tour, và nghe audio trên mobile.

### 4.2 Mục tiêu người dùng
- Tìm địa điểm nhanh, hiểu nội dung nhanh, nghe audio mượt.
- Theo dõi tour dễ, chọn điểm ăn phù hợp sở thích.
- Trải nghiệm tiếng Việt rõ ràng, nhất quán.

### 4.3 KPI đề xuất
- Mobile:
  - Tỷ lệ user phát ít nhất 1 audio/session >= 45%
  - Tỷ lệ hoàn thành nghe audio >= 30%
  - Tỷ lệ dùng Search trước khi mở chi tiết địa điểm >= 40%
- Web Admin:
  - Thời gian cập nhật 1 audio guide < 3 phút
  - Tỷ lệ dữ liệu có đủ metadata (title, description, language, duration, location) >= 95%
  - Tỷ lệ địa điểm có ít nhất 1 audio >= 90%

## 5. Personas
### 5.1 Người dùng cuối (Khách khám phá ẩm thực)
- Nhu cầu: tìm món/địa điểm nhanh, nghe giới thiệu ngắn gọn, xem tour gợi ý.
- Thiết bị chính: mobile.

### 5.2 Admin Hệ Thống
- Nhu cầu: quản trị toàn cục danh mục, địa điểm, tour, audio, user/role, báo cáo, cài đặt.

### 5.3 Admin POI
- Nhu cầu: cập nhật nội dung audio và thông tin địa điểm thuộc phạm vi được phân quyền.

## 6. Phạm vi sản phẩm
## 6.1 In-scope
### Mobile App (MAUI)
- Home: địa điểm nổi bật, danh mục, đề xuất.
- Search: tìm kiếm, lọc danh mục, gợi ý.
- Map: xem địa điểm trên bản đồ.
- Tours: danh sách tour, chi tiết tour.
- Audio Player: phát/tạm dừng/seek, hiển thị tiến độ.
- Profile: yêu thích, lịch sử nghe, downloads, cài đặt, chỉnh sửa thông tin.

### Web Admin (ASP.NET Core Razor Pages)
- Auth: login/logout, điều hướng theo role.
- Admin Hệ Thống:
  - Dashboard tổng quan
  - User & Role Management
  - Categories CRUD
  - Locations CRUD
  - Tours CRUD
  - AudioGuides CRUD
  - Reports
  - Settings
- Admin POI:
  - Dashboard theo location
  - Audio Management theo location
  - Reviews
  - Analytics
  - Cập nhật thông tin location thuộc quyền

### Design/Content
- Đồng bộ UI theo ngôn ngữ thiết kế đã triển khai trong repo (dashboard-hero, card-v2, table-v2).
- Toàn bộ content chính trên web sử dụng tiếng Việt có dấu, nhất quán thuật ngữ.

## 6.2 Out-of-scope (giai đoạn hiện tại)
- Thanh toán, booking, loyalty points.
- CMS headless riêng biệt.
- AI recommendation phức tạp theo hành vi real-time.
- Multi-tenant đa thương hiệu.

## 7. Yêu cầu chức năng
### 7.1 Authentication & Authorization
- User chưa đăng nhập phải vào màn hình login trước.
- Đăng nhập thành công:
  - Admin -> Admin Dashboard
  - ShopOwner -> POI Dashboard
- Logout trả về login.
- Policy phân quyền theo folder/page.

### 7.2 Quản trị danh mục (Categories)
- Tạo/sửa/xóa/xem danh mục.
- Thuộc tính: Id, Name, Icon, Description.
- Chặn xóa nếu còn dữ liệu liên kết (locations).

### 7.3 Quản trị địa điểm (Locations)
- CRUD địa điểm, map category.
- Thuộc tính: Id, Name, Description, Address, Lat, Long, Duration, ImageUrl, Category.
- Hiển thị số audio theo địa điểm.

### 7.4 Quản trị tour (Tours)
- CRUD tour, cấu hình location trong tour.
- Thuộc tính: Id, Name, Description, Duration, Price, IsFeatured, ImageUrl.

### 7.5 Quản trị audio guide
- CRUD audio guide theo location.
- Thuộc tính: Id, Title, Description, AudioUrl, Duration, Language, TranscriptText.
- Admin POI chỉ thấy và sửa dữ liệu trong location được phân quyền.

### 7.6 User & Role Management
- Danh sách user, role, trạng thái hoạt động.
- Mapping Admin POI với danh sách địa điểm quản lý.

### 7.7 Reports & Analytics
- Chỉ số tổng quan: tổng địa điểm, tổng audio, tổng tour, audio/location.
- Breakdown theo danh mục.
- Top location theo số lượng audio.
- Analytics POI theo location được chọn.

### 7.8 Mobile trải nghiệm người dùng
- Search trả kết quả nhanh, ưu tiên đúng danh mục/món liên quan.
- Audio player ổn định khi chuyển trang.
- Lưu lịch sử nghe và danh sách yêu thích.

## 8. Yêu cầu phi chức năng
### 8.1 Performance
- Web page TTFB mục tiêu < 500ms trong môi trường nội bộ.
- Search mobile phản hồi < 1 giây với dữ liệu mẫu hiện tại.

### 8.2 Reliability
- Không crash khi dữ liệu trống.
- Mọi trang CRUD xử lý trạng thái empty-state rõ ràng.

### 8.3 Security
- Cookie auth + role policy bắt buộc cho web admin.
- Không lộ credential thật trong production config.

### 8.4 Localization
- Giao diện chính web dùng tiếng Việt có dấu.
- Thuật ngữ chuẩn: Admin Hệ Thống, Admin POI, Bảng điều khiển, Thống kê, Quản lý audio.

### 8.5 Maintainability
- Tách ViewModels/Services theo module.
- Tiêu chuẩn UI tái sử dụng class hệ thống (hero/card/table).

## 9. Luồng người dùng chính
### 9.1 Luồng người dùng mobile
1. Mở app -> Home
2. Search hoặc chọn category
3. Xem chi tiết địa điểm
4. Play audio guide
5. Lưu yêu thích hoặc xem tour liên quan

### 9.2 Luồng Admin Hệ Thống
1. Login
2. Vào dashboard tổng
3. Quản lý danh mục/địa điểm/tour/audio
4. Kiểm tra báo cáo và settings

### 9.3 Luồng Admin POI
1. Login
2. Chọn location trong phạm vi
3. Quản lý audio và cập nhật thông tin location
4. Theo dõi review và analytics

## 10. Data model mức cao
- Category (1) - (n) Location
- Location (1) - (n) AudioGuide
- Tour (n) - (n) Location qua TourLocation
- User (role-based) - (n) LocationIds (với ShopOwner)

## 11. Kế hoạch phát hành đề xuất
### Phase 1 - Foundation
- Auth + Role routing
- CRUD cơ bản Categories/Locations/Tours/AudioGuides
- Mobile browse/play cơ bản

### Phase 2 - Operational Quality
- User & Role Management
- Reports + Settings
- Reviews + Analytics cho POI
- Chuẩn hóa UI/content tiếng Việt toàn web

### Phase 3 - Optimization
- Cải tiến search relevance
- Theo dõi KPI, tối ưu conversion nghe audio
- Tinh chỉnh UX mobile và dashboard theo dữ liệu thực tế

## 12. Rủi ro và giảm thiểu
- Rủi ro lệch content giữa module -> chuẩn hóa glossary + review checklist trước release.
- Rủi ro phân quyền sai phạm vi -> test matrix role/location bắt buộc.
- Rủi ro dữ liệu mẫu không đại diện -> thêm seed theo vùng/nhóm món thực tế.
- Rủi ro nợ UI do chỉnh rời rạc -> bắt buộc dùng design classes chuẩn.

## 13. Tiêu chí nghiệm thu (UAT)
- Login-first flow hoạt động đúng với mọi role.
- Tất cả trang quản trị chính hiển thị tiếng Việt đúng dấu.
- Admin POI không truy cập được location ngoài scope.
- CRUD quan trọng chạy ổn định, không lỗi validation cơ bản.
- Build solution thành công, không error.

## 14. Phụ lục
### 14.1 Tech stack
- Mobile: .NET MAUI, MVVM
- Web: ASP.NET Core Razor Pages (.NET 8)
- DB: SQL Server (EF Core)

### 14.2 Tên file PRD trong repo
- PRD_VinhKhanh_AudioGuide.md
