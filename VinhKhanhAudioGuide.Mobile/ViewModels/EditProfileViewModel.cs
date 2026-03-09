using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class EditProfileViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private DateTime _dateOfBirth = new(1990, 1, 1);

    [ObservableProperty]
    private string _gender = "Nam";

    [ObservableProperty]
    private string _avatarUrl = "default_avatar";

    [ObservableProperty]
    private bool _isSaving;

    public List<string> GenderOptions { get; } = new() { "Nam", "Nữ", "Khác" };

    public EditProfileViewModel(IApiService apiService, INavigationService navigationService)
    {
        _apiService = apiService;
        _navigationService = navigationService;
        _ = LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var profile = await _apiService.GetUserProfileAsync();
        if (profile != null)
        {
            Name = profile.Name;
            Email = profile.Email;
            AvatarUrl = profile.AvatarUrl;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Vui lòng nhập họ và tên.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Vui lòng nhập email hợp lệ.", "OK");
            return;
        }

        IsSaving = true;
        try
        {
            var profile = await _apiService.GetUserProfileAsync();
            if (profile != null)
            {
                profile.Name = Name;
                profile.Email = Email;
                profile.AvatarUrl = AvatarUrl;

                await _apiService.UpdateUserProfileAsync(profile);
            }

            await Application.Current!.MainPage!.DisplayAlert(
                "Thành công", "Thông tin đã được cập nhật.", "OK");

            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", $"Không thể lưu: {ex.Message}", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task ChangeAvatarAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Chọn ảnh đại diện"
            });

            if (result != null)
            {
                AvatarUrl = result.FullPath;
            }
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Không thể chọn ảnh.", "OK");
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Đổi mật khẩu",
            "Tính năng đang được phát triển. Vui lòng thử lại sau.",
            "OK");
    }
}
