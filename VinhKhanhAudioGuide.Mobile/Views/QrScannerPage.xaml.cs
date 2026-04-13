using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using VinhKhanhAudioGuide.Mobile.Services;
using ZXing.Net.Maui;
using System.IO;
using SkiaSharp;
using ZXing.SkiaSharp;
using ZXing.Common;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class QrScannerPage : ContentPage
{
	private bool _isHandlingResult;
	private readonly string _seedQrDeepLink = QrCodePayloadService.BuildAudioDeepLink("loc_001", "ag_001_1");
	private readonly ILocalizationService _localizationService;

	public QrScannerPage()
	{
		InitializeComponent();
		_localizationService = ResolveLocalizationService();
		LoadSeedQrPreview();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
		if (status != PermissionStatus.Granted)
		{
			status = await Permissions.RequestAsync<Permissions.Camera>();
		}

		if (status != PermissionStatus.Granted)
		{
			await DisplayAlert(
				_localizationService.GetString("Common_Notice"),
				_localizationService.GetString("Qr_StatusNoCamera"),
				_localizationService.GetString("Common_Understood"));
			App.NavigateToIntro();
			return;
		}

		try
		{
			ScannerView.Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.TwoDimensional,
				AutoRotate = true,
				Multiple = false
			};

			ScannerView.CameraLocation = CameraLocation.Rear;
			ScannerView.IsDetecting = false;
			await Task.Delay(120);

			_isHandlingResult = false;
			ScannerView.IsDetecting = true;
			StatusLabel.Text = _localizationService.GetString("Qr_StatusPrompt");
		}
		catch
		{
			StatusLabel.Text = _localizationService.GetString("Qr_StatusCameraFailed");
		}
	}

	private void LoadSeedQrPreview()
	{
		try
		{
			var qrBytes = QrCodePayloadService.GenerateQrCodePng(_seedQrDeepLink, 8);
			SeedQrImage.Source = ImageSource.FromStream(() => new MemoryStream(qrBytes));
		}
		catch
		{
			// Keep scanner usable even if preview generation fails.
		}
	}

	private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		if (_isHandlingResult)
		{
			return;
		}

		var firstResult = e.Results?.FirstOrDefault();
		var rawValue = firstResult?.Value;
		if (string.IsNullOrWhiteSpace(rawValue))
		{
			return;
		}

		_isHandlingResult = true;
		ScannerView.IsDetecting = false;

		MainThread.BeginInvokeOnMainThread(async () => await HandleScannedValueAsync(rawValue));
	}

	private async Task HandleScannedValueAsync(string rawValue)
	{
		if (!QrCodePayloadService.TryParseAudioPayload(rawValue, out var payload))
		{
			StatusLabel.Text = _localizationService.GetString("Qr_StatusInvalid");
			_isHandlingResult = false;
			ScannerView.IsDetecting = true;
			return;
		}

		StatusLabel.Text = "Đang chuyển đến màn hình thanh toán...";
		await App.CompleteQrOnboardingAsync(payload);
	}

	private void OnRescanClicked(object sender, EventArgs e)
	{
		_isHandlingResult = false;
		ScannerView.IsDetecting = true;
		StatusLabel.Text = _localizationService.GetString("Qr_StatusPrompt");
	}

	private async void OnPickQrImageClicked(object sender, EventArgs e)
	{
		if (_isHandlingResult)
		{
			return;
		}

		try
		{
			var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
			{
				Title = _localizationService.GetString("Qr_ImagePickerTitle")
			});

			if (file is null)
			{
				return;
			}

			await using var input = await file.OpenReadAsync();
			using var memory = new MemoryStream();
			await input.CopyToAsync(memory);
			var imageBytes = memory.ToArray();

			PickedQrImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
			PickedQrFrame.IsVisible = true;

			var qrText = TryDecodeQrTextFromImage(imageBytes);
			if (string.IsNullOrWhiteSpace(qrText))
			{
				StatusLabel.Text = _localizationService.GetString("Qr_StatusNoQrInImage");
				return;
			}

			_isHandlingResult = true;
			ScannerView.IsDetecting = false;
			await HandleScannedValueAsync(qrText);
		}
		catch
		{
			StatusLabel.Text = _localizationService.GetString("Qr_StatusImageReadFailed");
		}
	}

	private async void OnUseSeedQrClicked(object sender, EventArgs e)
	{
		if (_isHandlingResult)
		{
			return;
		}

		_isHandlingResult = true;
		ScannerView.IsDetecting = false;
		await HandleScannedValueAsync(_seedQrDeepLink);
	}

	private void OnBackToIntroClicked(object sender, EventArgs e)
	{
		ScannerView.IsDetecting = false;
		App.NavigateToIntro();
	}

	private static string? TryDecodeQrTextFromImage(byte[] imageBytes)
	{
		using var bitmap = SKBitmap.Decode(imageBytes);
		if (bitmap is null)
		{
			return null;
		}

		var reader = new BarcodeReader
		{
			AutoRotate = true,
			Options = new DecodingOptions
			{
				TryHarder = true,
				TryInverted = true,
				PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
			}
		};

		var result = reader.Decode(bitmap);
		return result?.Text;
	}

	private static ILocalizationService ResolveLocalizationService()
	{
		return Application.Current?.Handler?.MauiContext?.Services.GetService<ILocalizationService>()
			?? new LocalizationService();
	}
}
