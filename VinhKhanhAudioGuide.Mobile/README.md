# Vinh Khanh Audio Guide - Mobile App

Ứng dụng hướng dẫn du lịch bằng âm thanh, giúp bạn khám phá văn hóa và lịch sử Việt Nam.

## 🏗️ Cấu trúc dự án

```
VinhKhanhAudioGuide.Mobile/
├── App.xaml                    # App resources
├── AppShell.xaml               # Shell navigation
├── MauiProgram.cs              # DI & Config
├── Models/                     # Data models
│   ├── Location.cs
│   ├── Category.cs
│   ├── AudioGuide.cs
│   ├── Tour.cs
│   └── UserProfile.cs
├── ViewModels/                 # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── AudioPlayerViewModel.cs
│   ├── LocationDetailViewModel.cs
│   ├── MapViewModel.cs
│   ├── ToursViewModel.cs
│   ├── ProfileViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── FavoritesViewModel.cs
│   ├── SearchViewModel.cs
│   └── TourDetailViewModel.cs
├── Views/                      # XAML Pages
│   ├── MainPage.xaml
│   ├── AudioPlayerPage.xaml
│   ├── LocationDetailPage.xaml
│   ├── MapPage.xaml
│   ├── ToursPage.xaml
│   ├── ProfilePage.xaml
│   ├── SettingsPage.xaml
│   ├── FavoritesPage.xaml
│   ├── SearchPage.xaml
│   ├── TourDetailPage.xaml
│   ├── DownloadsPage.xaml
│   ├── HistoryPage.xaml
│   ├── HelpPage.xaml
│   ├── AboutPage.xaml
│   └── EditProfilePage.xaml
├── Services/                   # Business logic
│   ├── IAudioService.cs
│   ├── AudioService.cs
│   ├── INavigationService.cs
│   └── NavigationService.cs
├── Converters/                 # Value converters
│   └── Converters.cs
├── Data/                       # Sample data
│   └── SampleData.cs
├── Resources/                  # App resources
│   ├── AppIcon/
│   ├── Fonts/
│   ├── Images/
│   ├── Raw/
│   ├── Splash/
│   └── Styles/
│       ├── Colors.xaml
│       ├── Styles.xaml
│       └── AppStyles.xaml
└── Platforms/                  # Platform-specific code
    ├── Android/
    ├── iOS/
    ├── MacCatalyst/
    └── Windows/
```

## 🚀 Yêu cầu hệ thống

- .NET 8.0 SDK
- Visual Studio 2022 / JetBrains Rider 2023+
- Android SDK (API 21+)
- iOS SDK (nếu build cho iOS)
- Windows 10/11 (nếu build cho Windows)

## 📦 Các NuGet Packages

- `CommunityToolkit.Mvvm` - MVVM toolkit
- `CommunityToolkit.Maui` - MAUI extensions
- `CommunityToolkit.Maui.MediaElement` - Audio player

## ▶️ Chạy ứng dụng

### Trong JetBrains Rider:

1. Mở file `VinhKhanhAudioGuide.slnx`
2. Chọn project `VinhKhanhAudioGuide.Mobile`
3. Chọn target platform (Android, iOS, Windows)
4. Nhấn `Run` (F5)

### Bằng dotnet CLI:

```bash
# Restore packages
cd VinhKhanhAudioGuide.Mobile
dotnet restore

# Build for Android
dotnet build -f net8.0-android

# Run on Android emulator
dotnet build -f net8.0-android -t:Run

# Build for Windows
dotnet build -f net8.0-windows10.0.17763.0

# Run on Windows
dotnet run -f net8.0-windows10.0.17763.0
```

## 📱 Tính năng chính

### 1. Trang chủ
- Danh sách địa điểm nổi bật
- Danh mục theo chủ đề
- Địa điểm gần bạn

### 2. Khám phá (Search)
- Tìm kiếm địa điểm
- Lọc theo danh mục
- Gợi ý phổ biến

### 3. Bản đồ
- Hiển thị các địa điểm trên bản đồ
- Chỉ đường đến địa điểm
- Xem khoảng cách

### 4. Tours
- Lộ trình tham quan theo chủ đề
- Chi tiết từng tour
- Bắt đầu lộ trình

### 5. Tài khoản
- Thông tin cá nhân
- Danh sách yêu thích
- Audio đã tải
- Lịch sử nghe
- Cài đặt ứng dụng

### 6. Audio Player
- Phát audio hướng dẫn
- Điều khiển play/pause/seek
- Hiển thị tiến độ

## 🎨 Styles & Colors

### Primary Colors:
- Primary: `#512BD4` (Purple)
- Secondary: `#DFD8F7`
- Accent: `#2B0B98`

### Status Colors:
- Success: `#4CAF50` (Green)
- Warning: `#FF9800` (Orange)
- Error: `#F44336` (Red)

## 📝 Thêm hình ảnh

Đặt các hình ảnh vào thư mục `Resources/Images/`:

Các icon cần thiết:
- `home_icon.png`
- `search_icon.png`
- `map_icon.png`
- `tour_icon.png`
- `profile_icon.png`
- `play_icon.png`
- `pause_icon.png`
- `favorite_icon.png`
- `favorite_filled_icon.png`
- `download_icon.png`
- `audio_icon.png`
- `chevron_right.png`
- Và các hình ảnh địa điểm

## 🔧 Cấu hình

### Android
- Min SDK: 21
- Target SDK: 34
- Application ID: `com.vinhkhanh.audioguide`

### iOS
- Min iOS: 11.0
- Bundle ID: `com.vinhkhanh.audioguide`

### Windows
- Target: Windows 10.0.17763.0

## 📞 Liên hệ

- Email: support@vinhkhanhaudioguide.com
- Hotline: 1900 1234

## 📄 License

© 2024 Vinh Khanh Audio Guide. All rights reserved.
