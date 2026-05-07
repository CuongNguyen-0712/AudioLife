namespace VinhKhanhAudioGuide.Mobile.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        // Điều hướng tới màn hình theo route Shell.
        // Dùng ở các flow startup, player, map, checkout.
        await Shell.Current.GoToAsync(route);
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        // Điều hướng kèm tham số context (LocationId, PlaybackSource, ...).
        // Thuộc flow chuyển màn hình có trạng thái.
        await Shell.Current.GoToAsync(route, parameters);
    }

    public async Task GoBackAsync()
    {
        // Quay lại màn hình trước trong stack điều hướng.
        // Dùng cho các thao tác back trong flow UI.
        await Shell.Current.GoToAsync("..");
    }
}
