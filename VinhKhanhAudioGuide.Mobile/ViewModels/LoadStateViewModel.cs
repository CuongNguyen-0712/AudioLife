using CommunityToolkit.Mvvm.ComponentModel;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public abstract partial class LoadStateViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    protected void BeginLoading()
    {
        IsError = false;
        ErrorMessage = string.Empty;
        IsLoading = true;
    }

    protected void CompleteLoading(bool hasData)
    {
        HasData = hasData;
        IsEmpty = !hasData;
        IsError = false;
        ErrorMessage = string.Empty;
        IsLoading = false;
    }

    protected void FailLoading(string errorMessage)
    {
        HasData = false;
        IsEmpty = true;
        IsError = true;
        ErrorMessage = errorMessage;
        IsLoading = false;
    }
}
