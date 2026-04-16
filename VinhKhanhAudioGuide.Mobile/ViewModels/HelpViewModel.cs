using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class HelpViewModel : LoadStateViewModel
{
    [ObservableProperty]
    private List<FaqItem> _faqItems;

    public HelpViewModel()
    {
        _faqItems = new List<FaqItem>
        {
            new()
            {
                Question = "Làm sao để tải audio về máy?",
                Answer = "Vào trang chi tiết địa điểm, nhấn vào biểu tượng tải xuống bên cạnh mỗi audio guide. Audio sẽ được lưu vào bộ nhớ thiết bị để bạn nghe khi không có mạng."
            },
            new()
            {
                Question = "Làm sao để sử dụng offline?",
                Answer = "Sau khi tải audio về máy, bạn có thể nghe mà không cần kết nối internet. Vào mục 'Audio đã tải' trong tài khoản để xem danh sách audio đã tải."
            },
            new()
            {
                Question = "Làm sao để thay đổi ngôn ngữ?",
                Answer = "Vào Cài đặt > Ngôn ngữ, chọn ngôn ngữ bạn muốn sử dụng. Hiện tại ứng dụng hỗ trợ Tiếng Việt, Tiếng Anh, Tiếng Trung, Tiếng Nhật, Tiếng Hàn và Tiếng Pháp."
            },
            new()
            {
                Question = "Làm sao để chia sẻ với bạn bè?",
                Answer = "Tại trang chi tiết địa điểm, nhấn nút Chia sẻ ở góc trên bên phải để gửi link cho bạn bè qua tin nhắn, email hoặc mạng xã hội."
            },
            new()
            {
                Question = "Làm sao để báo lỗi?",
                Answer = "Nếu gặp lỗi, vui lòng liên hệ hỗ trợ qua email support@vinhkhanhaudioguide.com hoặc gọi hotline 1900 1234. Chúng tôi sẽ phản hồi trong 24 giờ."
            },
            new()
            {
                Question = "Ứng dụng có miễn phí không?",
                Answer = "Ứng dụng cung cấp nhiều audio guide miễn phí. Một số nội dung premium yêu cầu đăng ký gói trả phí để truy cập."
            },
            new()
            {
                Question = "Dữ liệu có chính xác không?",
                Answer = "Nội dung audio được biên soạn bởi các chuyên gia lịch sử và văn hóa. Chúng tôi thường xuyên cập nhật và kiểm tra tính chính xác của thông tin."
            }
        };

        CompleteLoading(_faqItems.Count > 0);
    }

    [RelayCommand]
    private void ToggleFaqItem(FaqItem? item)
    {
        if (item == null) return;
        item.IsExpanded = !item.IsExpanded;
    }

    [RelayCommand]
    private async Task SendEmailAsync()
    {
        try
        {
            var message = new EmailMessage
            {
                Subject = "Hỗ trợ - Vinh Khanh Audio Guide",
                Body = "",
                To = new List<string> { "support@vinhkhanhaudioguide.com" }
            };
            await Email.Default.ComposeAsync(message);
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Không thể mở ứng dụng email.", "OK");
        }
    }

    [RelayCommand]
    private async Task CallHotlineAsync()
    {
        try
        {
            PhoneDialer.Default.Open("19001234");
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Không thể thực hiện cuộc gọi.", "OK");
        }
    }

    [RelayCommand]
    private async Task OpenUserGuideAsync()
    {
        try
        {
            await Browser.Default.OpenAsync("https://vinhkhanhaudioguide.com/guide", BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Không thể mở trình duyệt.", "OK");
        }
    }
}

public class FaqItem : ObservableObject
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
}
